using System.Text.Json;
using Katasec.DStream.Abstractions;

namespace Katasec.DStream.SDK.Core;

/// <summary>
/// Stdin/Stdout host that bridges existing SDK abstractions to simple stdin/stdout execution
/// Handles all the plumbing so provider authors only focus on business logic
/// </summary>
public static class StdioProviderHost
{
    /// <summary>
    /// Run an output provider in stdin/stdout mode
    /// </summary>
    public static async Task RunOutputProviderAsync<TProvider, TConfig>(CancellationToken cancellationToken = default)
        where TProvider : ProviderBase<TConfig>, IOutputProvider, new()
        where TConfig : class, new()
    {
        try
        {
            // Setup graceful shutdown if no external token provided
            var cts = cancellationToken == default ? new CancellationTokenSource() : null;
            var effectiveToken = cancellationToken == default ? cts!.Token : cancellationToken;
            
            if (cts != null)
            {
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true;
                    cts.Cancel();
                };
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Starting service...");

            // Read configuration from stdin
            var configJson = await Console.In.ReadLineAsync();
            if (string.IsNullOrEmpty(configJson))
            {
                await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] No configuration received");
                Environment.Exit(1);
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Received config: {configJson}");

            // Parse configuration  
            var config = JsonSerializer.Deserialize<TConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }) ?? new TConfig();

            // Create provider instance
            var provider = new TProvider();
            var context = new StdioPluginContext(); // Mock context for stdin/stdout mode
            provider.Initialize(config, context);

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Starting data processing...");

            // Process incoming data stream
            var messageCount = 0;
            string? line;
            var envelopes = new List<Envelope>();

            while ((line = await Console.In.ReadLineAsync()) != null && !effectiveToken.IsCancellationRequested)
            {
                try
                {
                    messageCount++;

                    // Parse the incoming JSON as Envelope
                    var envelopeData = JsonSerializer.Deserialize<EnvelopeDto>(line, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                    if (envelopeData != null)
                    {
                        // Convert DTO to SDK Envelope
                        var envelope = new Envelope(
                            envelopeData.Data ?? new object(),
                            envelopeData.Metadata ?? new Dictionary<string, object?>()
                        );

                        envelopes.Add(envelope);

                        // Call provider's WriteAsync method
                        await provider.WriteAsync([envelope], context, effectiveToken);
                    }
                }
                catch (JsonException)
                {
                    await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Failed to parse JSON: {line}");
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Error processing message: {ex.Message}");
                }
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Processed {messageCount} messages. Stream ended.");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Run an input provider in stdin/stdout mode
    /// </summary>
    public static async Task RunInputProviderAsync<TProvider, TConfig>(CancellationToken cancellationToken = default)
        where TProvider : ProviderBase<TConfig>, IInputProvider, new()
        where TConfig : class, new()
    {
        try
        {
            // Setup graceful shutdown if no external token provided
            var cts = cancellationToken == default ? new CancellationTokenSource() : null;
            var effectiveToken = cancellationToken == default ? cts!.Token : cancellationToken;
            
            if (cts != null)
            {
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true;
                    cts.Cancel();
                };
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Starting service...");

            // Read configuration from stdin
            var configJson = await Console.In.ReadLineAsync();
            if (string.IsNullOrEmpty(configJson))
            {
                await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] No configuration received");
                Environment.Exit(1);
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Received config: {configJson}");

            // Parse configuration  
            var config = JsonSerializer.Deserialize<TConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }) ?? new TConfig();

            // Create provider instance
            var provider = new TProvider();
            var context = new StdioPluginContext(); // Mock context for stdin/stdout mode
            provider.Initialize(config, context);

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Starting data generation...");

            // Read from input provider and emit to stdout
            await foreach (var envelope in provider.ReadAsync(context, effectiveToken))
            {
                if (effectiveToken.IsCancellationRequested) break;

                // Convert envelope to JSON and write to stdout
                var envelopeDto = new EnvelopeDto
                {
                    Data = envelope.Payload,
                    Metadata = envelope.Meta?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>()
                };

                var json = JsonSerializer.Serialize(envelopeDto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await Console.Out.WriteLineAsync(json);
                await Console.Out.FlushAsync();
            }

            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Input provider completed.");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{typeof(TProvider).Name}] Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

/// <summary>
/// DTO for serializing/deserializing JSON envelopes in stdin/stdout communication
/// </summary>
internal class EnvelopeDto
{
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>
/// Simple plugin context for stdin/stdout mode
/// </summary>
internal class StdioPluginContext : IPluginContext
{
    public object Logger => new StdioLogger();
}

/// <summary>
/// Simple logger that writes to stderr for stdin/stdout mode
/// </summary>
internal class StdioLogger
{
    public void Info(string message) => Console.Error.WriteLine($"[INFO] {message}");
    public void Error(string message) => Console.Error.WriteLine($"[ERROR] {message}");
    public void Debug(string message) => Console.Error.WriteLine($"[DEBUG] {message}");
}