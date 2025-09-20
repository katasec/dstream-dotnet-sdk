using System.Text.Json;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

namespace ConsoleOutputProvider;

// Configuration class for the console output provider
public class ConsoleConfig
{
    /// <summary>
    /// Output format: "simple" (default) or "json" or "structured"
    /// </summary>
    public string OutputFormat { get; set; } = "simple";
    
    /// <summary>
    /// Demo infrastructure resource count for lifecycle testing
    /// </summary>
    public int ResourceCount { get; set; } = 3;
}

// Console output provider that demonstrates output + infrastructure lifecycle
public class ConsoleOutputProvider : InfrastructureProviderBase<ConsoleConfig>, IOutputProvider
{
    // Infrastructure lifecycle methods - override the OnXxx methods from base class
    protected override async Task<string[]> OnInitializeInfrastructureAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"🚀 Running 'init' - Creating demo infrastructure for console output provider...");
        
        // Simulate quick infrastructure creation
        await Task.Delay(100, ct);
        
        var resources = new string[]
        {
            "console_log_target:stdout",
            "console_error_target:stderr",
            $"demo_resource_count:{Config.ResourceCount}"
        };
        
        await Console.Error.WriteLineAsync($"✅ Infrastructure initialized! Created {Config.ResourceCount} demo resources.");
        return resources;
    }
    
    protected override async Task<string[]> OnDestroyInfrastructureAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"🔥 Running 'destroy' - Tearing down demo infrastructure for console output provider...");
        
        // Simulate quick teardown
        await Task.Delay(100, ct);
        
        var resources = new string[]
        {
            "console_log_target:stdout",
            "console_error_target:stderr",
            $"demo_resource_count:{Config.ResourceCount}"
        };
        
        await Console.Error.WriteLineAsync($"🗑️ All {Config.ResourceCount} demo infrastructure resources destroyed.");
        return resources;
    }
    
    protected override async Task<(string[] resources, Dictionary<string, object?>? metadata)> OnGetInfrastructureStatusAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"📊 Running 'status' - Checking console output provider infrastructure...");
        
        await Task.Delay(50, ct);
        
        var resources = new string[]
        {
            "console_log_target:HEALTHY",
            "console_error_target:HEALTHY",
            $"demo_resource_count:{Config.ResourceCount}"
        };
        
        var metadata = new Dictionary<string, object?>
        {
            ["last_checked"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["output_format"] = Config.OutputFormat,
            ["console_available"] = "stdout+stderr ready"
        };
        
        await Console.Error.WriteLineAsync($"✅ Status: {Config.ResourceCount} demo resources are healthy and running.");
        return (resources, metadata);
    }
    
    protected override async Task<(string[] resources, Dictionary<string, object?>? changes)> OnPlanInfrastructureChangesAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"📋 Running 'plan' - Planning infrastructure changes for console output provider...");
        
        await Task.Delay(50, ct);
        
        var resources = new string[]
        {
            "console_log_target:WILL_CREATE",
            "console_error_target:WILL_CREATE",
            $"demo_resource_count:{Config.ResourceCount}"
        };
        
        var changes = new Dictionary<string, object?>
        {
            ["resources_to_create"] = Config.ResourceCount,
            ["resources_to_change"] = 0,
            ["resources_to_destroy"] = 0,
            ["output_format"] = Config.OutputFormat,
            ["estimated_cost"] = "$0.00/month (console is free!)"
        };
        
        await Console.Error.WriteLineAsync($"📈 Plan: Will create {Config.ResourceCount} demo resources (+{Config.ResourceCount} to add, 0 to change, 0 to destroy)");
        return (resources, changes);
    }
    
    // Output provider implementation (for run command)
    private static int _messageCount = 0;
    
    public async Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[ConsoleOutputProvider] Processing batch of {batch.Count()} envelopes with format '{Config.OutputFormat}'");
        
        foreach (var envelope in batch)
        {
            if (ct.IsCancellationRequested) break;
            
            _messageCount++;
            await OutputFormattedEnvelopeAsync(envelope, _messageCount);
        }
    }
    
    private async Task OutputFormattedEnvelopeAsync(Envelope envelope, int messageCount)
    {
        var format = Config.OutputFormat?.ToLower() ?? "simple";
        
        switch (format)
        {
            case "json":
                var json = JsonSerializer.Serialize(new { envelope.Payload, envelope.Meta });
                await Console.Out.WriteLineAsync(json);
                break;
                
            case "structured":
                var structured = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
                await Console.Out.WriteLineAsync($"--- Message #{messageCount} ---");
                await Console.Out.WriteLineAsync(structured);
                break;
                
            default: // "simple"
                await Console.Out.WriteLineAsync($"Message #{messageCount}: {JsonSerializer.Serialize(envelope.Payload)}");
                break;
        }
    }
}

// Main entry point
public class Program
{
    public static async Task Main(string[] args)
    {
        await StdioProviderHost.RunProviderWithCommandAsync<ConsoleOutputProvider, ConsoleConfig>();
    }
}
