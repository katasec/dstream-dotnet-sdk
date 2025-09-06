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

        // Emit 1..N indefinitely? Keep a simple cap for sample UX; adjust as needed.
        var seq = 0;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Config.Interval, ct);

            seq++;
            hc.Debug($"tick seq={seq}");

            var meta = new Dictionary<string, object?> { ["seq"] = seq, ["source"] = "counter" };
            yield return new Envelope(seq, meta);
        }

        hc.Info("counter_complete");
    }

    protected override void OnInitialized()
    {
        var hc = (HCLogger)Ctx.Logger;
        hc.Info("initialized_counter_plugin");
    }
}
