using HCLog.Net;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK;

namespace DStreamDotNetTest;

/// <summary>
/// Counter input provider that generates incremental integers and emits them downstream.
/// </summary>
public sealed class GenericCounterPlugin : ProviderBase<GenericCounterConfig>, IInputProvider
{
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
