using Katasec.DStream.Abstractions;

namespace Katasec.DStream.SDK.Core;

/// <summary>
/// Base class for providers that need config binding + access to plugin context.
/// </summary>
public abstract class ProviderBase<TConfig>
{
    protected TConfig Config { get; private set; } = default!;
    protected IPluginContext Ctx { get; private set; } = default!;

    public void Initialize(TConfig config, IPluginContext ctx)
    {
        Config = config;
        Ctx = ctx;
        OnInitialized();
    }

    /// <summary>
    /// Optional hook after Initialize is called.
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// Helper to emit downstream envelopes.
    /// </summary>
    protected Task EmitAsync(object payload, CancellationToken ct, IDictionary<string, object?>? meta = null)
        => Ctx.Emit(new Envelope(payload, (meta ?? new Dictionary<string, object?>()).AsReadOnly()), ct);
}



internal static class DictExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this IDictionary<TKey, TValue> d) where TKey : notnull
        => d is IReadOnlyDictionary<TKey, TValue> ro ? ro : new Dictionary<TKey, TValue>(d);
}
