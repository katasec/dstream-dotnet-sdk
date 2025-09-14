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
}
