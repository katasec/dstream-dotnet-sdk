using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HCLog.Net;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;
using Proto = Katasec.DStream.Proto;

namespace Katasec.DStream.SDK.PluginHost;

public sealed class PluginServiceImpl<TProvider, TConfig> : Proto.Plugin.PluginBase
    where TProvider : ProviderBase<TConfig>, IProvider, new()
    where TConfig : class, new()
{
    private readonly HCLogger _log;
    private readonly IServiceProvider _services = new ServiceProviderStub();

    private TProvider? _provider;
    private TConfig _config = new();

    public PluginServiceImpl(HCLogger log) => _log = log;

    public override Task<Proto.GetSchemaResponse> GetSchema(Empty request, ServerCallContext context)
    {
        _log.Info("[RPC] GetSchema()");
        // TODO: fill with real schema later
        return Task.FromResult(new Proto.GetSchemaResponse());
    }

    public override async Task<Empty> Start(Proto.StartRequest request, ServerCallContext context)
    {
        var raw = JsonFormatter.Default.Format(request);
        _log.Info($"[RPC] Start payload={raw}");

        _config = BindConfig(raw) ?? new TConfig();
        _log.Info($"[CONFIG] bound={JsonSerializer.Serialize(_config)}");
        
        // Parse output provider configuration  
        var outputProvider = request.Output?.Provider ?? "";
        var outputConfigJson = request.Output?.Config?.ToString() ?? "{}";
        _log.Info($"[OUTPUT] provider={outputProvider} config={outputConfigJson}");
        
        // Simple validation: create console output provider directly
        IOutputProvider? outputProviderInstance = null;
        if (outputProvider == "console")
        {
            // TODO: Replace with proper provider loading in separate binaries phase
            outputProviderInstance = CreateConsoleProviderDirect(outputConfigJson);
            _log.Info($"[OUTPUT] Console provider created for validation");
        }

        // init provider once
        if (_provider is null)
        {
            _provider = new TProvider();
            _provider.Initialize(_config, MakeCtx());
        }

        // BLOCK here and drive the input loop until host cancels the RPC
        if (_provider is not IInputProvider input)
        {
            _log.Warn("provider is not IInputProvider; nothing to run");
            return new Empty();
        }

        var ct = context.CancellationToken; // will cancel when host stops the task
        var ctx = MakeCtx(outputProviderInstance);

        try
        {
            await foreach (var env in input.ReadAsync(ctx, ct))
                await ctx.Emit(env, ct);
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex) { _log.Error("run_input_loop_failed", ex); }

        return new Empty();
    }


    private IPluginContext MakeCtx(IOutputProvider? outputProvider = null) =>
        new LocalContext(_log, _services, async (env, ct) =>
        {
            if (outputProvider != null)
            {
                // Route to actual output provider  
                var outputCtx = new LocalContext(_log, _services, async (_, _) => { /* no nested routing */ });
                await outputProvider.WriteAsync(new[] { env }, outputCtx, ct);
                _log.Info($"routed payload={env.Payload} to output provider");
            }
            else
            {
                // Fallback: log payload + meta (original behavior)
                var meta = env.Meta is null ? "" : JsonSerializer.Serialize(env.Meta);
                _log.Info($"emit payload={env.Payload} meta={meta}");
            }
        });

    private static TConfig? BindConfig(string rawJson)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            // Try common shapes: { "config": {...} } or { "body": {...} }
            var cfgNode = node?["config"] ?? node?["body"];
            var json = (cfgNode ?? node)?.ToJsonString();
            return json is null ? null
                                : JsonSerializer.Deserialize<TConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    private sealed class LocalContext(object logger, IServiceProvider services, Emit emit) : IPluginContext
    {
        public object Logger { get; } = logger; // HCLogger (raw)
        public IServiceProvider Services { get; } = services;
        public Emit Emit { get; } = emit;
    }

    private sealed class ServiceProviderStub : IServiceProvider
    {
        public object? GetService(System.Type serviceType) => null;
    }
    
    private IOutputProvider? CreateConsoleProviderDirect(string configJson)
    {
        try
        {
            // Direct instantiation for validation - will be replaced in separate binaries phase
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToArray();
            _log.Info($"Loaded assemblies: {string.Join(", ", loadedAssemblies)}");
            
            var consoleAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Katasec.DStream.Provider.Console");
                
            if (consoleAssembly == null)
            {
                _log.Warn("Console provider assembly not loaded");
                return null;
            }
            
            var providerType = consoleAssembly.GetType("Katasec.DStream.Provider.ConsoleOut.ConsoleOutputProvider");
            var configType = consoleAssembly.GetType("Katasec.DStream.Provider.ConsoleOut.ConsoleOutputConfig");
            
            if (providerType == null || configType == null)
            {
                _log.Warn("Console provider types not found");
                return null;
            }
            
            var provider = Activator.CreateInstance(providerType) as IOutputProvider;
            var config = JsonSerializer.Deserialize(configJson, configType);
            
            // Initialize the provider
            var initMethod = providerType.GetMethod("Initialize");
            initMethod?.Invoke(provider, new[] { config, MakeCtx() });
            
            return provider;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to create console provider: {ex.Message}");
            return null;
        }
    }
}
