using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HCLog.Net;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK;

namespace Katasec.DStream.Provider.ConsoleOut;

public sealed class ConsoleOutputProvider
    : ProviderBase<ConsoleOutputConfig>, IOutputProvider
{
    public Task WriteAsync(
        IEnumerable<Envelope> stream,
        IPluginContext ctx,
        CancellationToken ct)
    {
        var log = (HCLogger)ctx.Logger;
        log.Info($"console_output_start format={Config.Format}");

        foreach (var env in stream)
        {
            if (ct.IsCancellationRequested) break;

            switch (Config.Format.ToLowerInvariant())
            {
                case "text":
                    if (Config.IncludeMeta && env.Meta is not null)
                        log.Info($"payload={env.Payload} meta={JsonSerializer.Serialize(env.Meta)}");
                    else
                        log.Info($"payload={env.Payload}");
                    break;

                default: // "json"
                    var obj = new { payload = env.Payload, meta = env.Meta };
                    log.Info(JsonSerializer.Serialize(obj));
                    break;
            }
        }

        log.Info("console_output_complete");
        return Task.CompletedTask;
    }
}
