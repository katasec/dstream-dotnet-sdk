using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

// Entry point for the modern stdin/stdout provider.
// This provider reads configuration from stdin and outputs data to stdout.
// No more gRPC plugin host - pure Unix pipeline approach.

await StdioProviderHost.RunInputProviderAsync<GenericCounterPlugin, GenericCounterConfig>();


/// <summary>
/// Config for the GenericCounterPlugin.
/// </summary>
public sealed record GenericCounterConfig
{
    // Name matches HCL key "interval" from dstream.hcl
    // Units: milliseconds
    public int Interval { get; init; } = 5000;
}

/// <summary>
///  Code for the plugin:
///  A plugin implements ProviderBase. ReadAsync reads from the source and 
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
        // Log to stderr (stdout is for data output)
        await Console.Error.WriteLineAsync($"[GenericCounterPlugin] Starting counter with interval={Config.Interval}ms");

        for (int seq = 1; !ct.IsCancellationRequested; seq++)
        {
            await Task.Delay(Config.Interval, ct);

            var data = new { value = seq, timestamp = DateTimeOffset.UtcNow };
            var meta = new Dictionary<string, object?>
            {
                ["seq"] = seq,
                ["source"] = "generic-counter-plugin",
                ["interval_ms"] = Config.Interval
            };

            await Console.Error.WriteLineAsync($"[GenericCounterPlugin] Emitting counter value: {seq}");
            yield return new Envelope(data, meta);
        }

        await Console.Error.WriteLineAsync("[GenericCounterPlugin] Counter stopped");
    }
}


