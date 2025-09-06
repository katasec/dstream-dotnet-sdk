using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HCLog.Net;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK;
using Proto = Katasec.DStream.Proto;

namespace Katasec.DStream.Host.Bridge;

public sealed class PluginServiceImpl<TProvider, TConfig> : Proto.Plugin.PluginBase
    where TProvider : ProviderBase<TConfig>, IProvider, new()
    where TConfig : class, new()
{
    private readonly HCLogger _log;
    private readonly IServiceProvider _services = new ServiceProviderStub();

    private TProvider? _provider;
    private TConfig _config = new();
    private CancellationTokenSource? _runCts;

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
        var ctx = MakeCtx();

        try
        {
            await foreach (var env in input.ReadAsync(ctx, ct))
                await ctx.Emit(env, ct);
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex) { _log.Error("run_input_loop_failed", ex); }

        return new Empty();
    }


    private IPluginContext MakeCtx() =>
        new LocalContext(_log, _services, async (env, _) =>
        {
            // Minimal emit bridge: log payload + meta
            var meta = env.Meta is null ? "" : JsonSerializer.Serialize(env.Meta);
            _log.Info($"emit payload={env.Payload} meta={meta}");
            await Task.CompletedTask;
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
}
