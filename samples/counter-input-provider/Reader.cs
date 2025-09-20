using System.Runtime.CompilerServices;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

namespace CounterInputProvider;

/// <summary>
/// Counter input provider - Core data reading logic
/// Generates sequential numbers with timestamps
/// Demonstrates minimal DStream input provider implementation
/// </summary>
public sealed class CounterInputProvider : ProviderBase<CounterConfig>, IInputProvider
{
    /// <summary>
    /// ReadAsync generates an incremental counter every Config.Interval milliseconds 
    /// and yields it downstream as an Envelope.
    /// </summary>
    /// <param name="ctx">A PluginContext is provided by the host at runtime, containing a logger and other services.</param>
    /// <param name="ct">Cancellation token for graceful shutdown</param>
    /// <returns>Async enumerable of counter envelopes</returns>
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, [EnumeratorCancellation] CancellationToken ct)
    {
        // Log to stderr (stdout is for data output)
        var maxInfo = Config.MaxCount > 0 ? $", max_count={Config.MaxCount}" : ", infinite";
        await Console.Error.WriteLineAsync($"[CounterInputProvider] Starting counter with interval={Config.Interval}ms{maxInfo}");

        for (int count = 1; !ct.IsCancellationRequested; count++)
        {
            // Stop if max count reached
            if (Config.MaxCount > 0 && count > Config.MaxCount)
            {
                await Console.Error.WriteLineAsync($"[CounterInputProvider] Reached max count {Config.MaxCount}, stopping");
                break;
            }

            var data = new { value = count, timestamp = DateTimeOffset.UtcNow };
            var meta = new Dictionary<string, object?>
            {
                ["seq"] = count,
                ["source"] = "counter-input-provider",
                ["interval_ms"] = Config.Interval
            };

            await Console.Error.WriteLineAsync($"[CounterInputProvider] Emitting counter value: {count}");
            yield return new Envelope(data, meta);

            await Task.Delay(Config.Interval, ct);
        }

        await Console.Error.WriteLineAsync("[CounterInputProvider] Counter stopped");
    }
}