using Katasec.DStream.Host.Bridge;
using Katasec.DStream.Abstractions;
using HCLog.Net;
using Katasec.DStream.SDK;

await PluginHost.Run<GenericCounterPlugin, GenericCounterConfig>();

/// <summary>
/// Default config for GenericCounterPlugin.
/// </summary>
public sealed record GenericCounterConfig
{
    // Name matches HCL key "interval" from dstream.hcl
    // Units: milliseconds
    public int Interval { get; init; } = 5000;
}

/// <summary>
///  A plugin implements ProviderBase<TConfig>. ReadAsync reads from the source and 
///  yields Envelopes downstream.
///  An envelope is a payload + optional metadata dictionary.
/// </summary>
public sealed class GenericCounterPlugin : ProviderBase<GenericCounterConfig>, IInputProvider
{
    /// <summary>
    /// ReadAsync generates an incremental counter every Config.Interval milliseconds 
    /// and yields it downstream as an Envelope.
    /// </summary>
    /// <param name="ctx">A PluginContext is provided by the host at runtime, containing a logger and other services. </param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var hc = (HCLogger)ctx.Logger; // HCLogger from the host
        hc.Info($"counter_start interval={Config.Interval}");

        for (int seq = 0; !ct.IsCancellationRequested; seq++)
        {
            await Task.Delay(Config.Interval, ct);
            seq++;

            var meta = new Dictionary<string, object?>
            {
                ["seq"] = seq,
                ["source"] = "counter"
            };

            yield return new Envelope(seq, meta);
        }
    }
}


