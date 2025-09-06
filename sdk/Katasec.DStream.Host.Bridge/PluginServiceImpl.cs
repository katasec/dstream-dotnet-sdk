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

    public override Task<Empty> Start(Proto.StartRequest request, ServerCallContext context)
    {
        var raw = JsonFormatter.Default.Format(request);
        _log.Info($"[RPC] Start payload={raw}");

        // Bind StartRequest -> TConfig (tries request.config first, else whole object)
        _config = BindConfig(raw) ?? new TConfig();
        _log.Info($"[CONFIG] bound={JsonSerializer.Serialize(_config)}");


        // log interval if present on TConfig
        var prop = typeof(TConfig).GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "Interval", StringComparison.OrdinalIgnoreCase));
        if (prop is not null)
        {
            var val = prop.GetValue(_config);
            _log.Info($"[CONFIG] interval(from TConfig)={val}");
        }


        // Initialize provider (once)
        if (_provider is null)
        {
            _provider = new TProvider();
            var initCtx = MakeCtx();
            _provider.Initialize(_config, initCtx);
        }

        // Kick the input loop in background (since Start is unary)
        _runCts?.Cancel();
        _runCts = new CancellationTokenSource();
        _ = RunInputLoop(_runCts.Token);

        return Task.FromResult(new Empty());
    }

    // ---------- helpers ----------

    private async Task RunInputLoop(CancellationToken ct)
    {
        if (_provider is not IInputProvider input) { _log.Warn("provider is not IInputProvider"); return; }

        var ctx = MakeCtx();

        try
        {
            await foreach (var env in input.ReadAsync(ctx, ct))
            {
                // Emit is bridged to HCLogger for now; you can swap to gRPC later if you add a stream RPC.
                await ctx.Emit(env, ct);
            }
        }
        catch (OperationCanceledException) { /* normal */ }
        catch (Exception ex) { _log.Error("run_input_loop_failed", ex); }
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
