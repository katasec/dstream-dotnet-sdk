using System.Text.Json;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

namespace TestInfraProvider;

// Configuration class for the test provider
public class TestConfig
{
    public string? TestValue { get; set; } = "hello-world";
    public int ResourceCount { get; set; } = 3;
}

// Test infrastructure provider that demonstrates the command routing
public class TestInfraProvider : InfrastructureProviderBase<TestConfig>, IOutputProvider
{
    // Infrastructure lifecycle methods - override the OnXxx methods from base class
    protected override async Task<string[]> OnInitializeInfrastructureAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"🚀 Running 'init' - Creating test infrastructure for '{Config.TestValue}'...");
        
        // Simulate quick infrastructure creation
        await Task.Delay(100, ct);
        
        var resources = new string[]
        {
            "azure_servicebus_namespace:test-dstream-ns",
            "azure_servicebus_queue:test-events-queue",
            $"resource_count:{Config.ResourceCount}"
        };
        
        await Console.Error.WriteLineAsync($"✅ Infrastructure initialized! Created {Config.ResourceCount} test resources.");
        return resources;
    }
    
    protected override async Task<string[]> OnDestroyInfrastructureAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"🔥 Running 'destroy' - Tearing down test infrastructure for '{Config.TestValue}'...");
        
        // Simulate quick teardown
        await Task.Delay(100, ct);
        
        var resources = new string[]
        {
            "azure_servicebus_namespace:test-dstream-ns",
            "azure_servicebus_queue:test-events-queue",
            $"resource_count:{Config.ResourceCount}"
        };
        
        await Console.Error.WriteLineAsync($"🗑️ All {Config.ResourceCount} test infrastructure resources destroyed.");
        return resources;
    }
    
    protected override async Task<(string[] resources, Dictionary<string, object?>? metadata)> OnGetInfrastructureStatusAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"📊 Running 'status' - Checking infrastructure for '{Config.TestValue}'...");
        
        await Task.Delay(50, ct);
        
        var resources = new string[]
        {
            "azure_servicebus_namespace:HEALTHY",
            "azure_servicebus_queue:HEALTHY",
            $"resource_count:{Config.ResourceCount}"
        };
        
        var metadata = new Dictionary<string, object?>
        {
            ["last_checked"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            ["health_status"] = "All systems operational"
        };
        
        await Console.Error.WriteLineAsync($"✅ Status: {Config.ResourceCount} resources are healthy and running.");
        return (resources, metadata);
    }
    
    protected override async Task<(string[] resources, Dictionary<string, object?>? changes)> OnPlanInfrastructureChangesAsync(CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"📋 Running 'plan' - Planning infrastructure changes for '{Config.TestValue}'...");
        
        await Task.Delay(50, ct);
        
        var resources = new string[]
        {
            "azure_servicebus_namespace:WILL_CREATE",
            "azure_servicebus_queue:WILL_CREATE",
            $"resource_count:{Config.ResourceCount}"
        };
        
        var changes = new Dictionary<string, object?>
        {
            ["resources_to_create"] = Config.ResourceCount,
            ["resources_to_change"] = 0,
            ["resources_to_destroy"] = 0,
            ["estimated_cost"] = "$5.00/month"
        };
        
        await Console.Error.WriteLineAsync($"📈 Plan: Will create {Config.ResourceCount} resources (+{Config.ResourceCount} to add, 0 to change, 0 to destroy)");
        return (resources, changes);
    }
    
    // Output provider implementation (for run command)
    public async Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct)
    {
        await Console.Error.WriteLineAsync($"[{nameof(TestInfraProvider)}] Processing batch of {batch.Count()} envelopes");
        
        foreach (var envelope in batch)
        {
            await Console.Error.WriteLineAsync($"[{nameof(TestInfraProvider)}] Processed envelope with payload: {JsonSerializer.Serialize(envelope.Payload)}");
        }
    }
}

// Main entry point
public class Program
{
    public static async Task Main(string[] args)
    {
        await StdioProviderHost.RunProviderWithCommandAsync<TestInfraProvider, TestConfig>();
    }
}