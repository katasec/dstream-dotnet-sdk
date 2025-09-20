# DStream Console Output Provider

A sample .NET output provider demonstrating clean architecture separation between **data processing logic** and **infrastructure lifecycle management** in the DStream ecosystem.

## 📁 File Structure

```
console-output-provider/
├── Program.cs        ← Top-level program entry point (5 lines)
├── Config.cs         ← Configuration class
├── Writer.cs         ← Core data processing logic (implements WriteAsync)
└── Infrastructure.cs ← Infrastructure lifecycle management (init/plan/status/destroy)
```

## 🎯 Architecture: Clean Separation of Concerns

This provider demonstrates **partial classes** to cleanly separate concerns:

### 🔧 Core Data Processing (`Writer.cs`)

**Purpose**: Handle incoming streaming data and format output
**Interface**: `IOutputProvider` from `Katasec.DStream.Abstractions`

```csharp
public interface IOutputProvider : IProvider
{
    Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct);
}
```

**Required Implementation**:
- **`WriteAsync` method**: Process batches of `Envelope` objects
- **`Envelope` structure**: `record struct Envelope(object Payload, IReadOnlyDictionary<string, object?> Meta)`
- **Error handling**: Respect `CancellationToken` for graceful shutdown
- **Batch processing**: Handle multiple envelopes efficiently

**Key Responsibilities**:
- ✅ Transform and format incoming data
- ✅ Write output to destination (console, files, APIs, databases, queues)
- ✅ Handle envelope metadata for routing/filtering
- ✅ Implement business logic for data processing

### 🏗️ Infrastructure Management (`Infrastructure.cs`)

**Purpose**: Handle infrastructure provisioning and lifecycle
**Interface**: `IInfrastructureProvider` from `Katasec.DStream.Abstractions`

```csharp
public interface IInfrastructureProvider : IProvider
{
    Task<InfrastructureResult> InitializeAsync(CancellationToken ct);
    Task<InfrastructureResult> DestroyAsync(CancellationToken ct);
    Task<InfrastructureResult> GetStatusAsync(CancellationToken ct);
    Task<InfrastructureResult> PlanAsync(CancellationToken ct);
}
```

**Base Class**: `InfrastructureProviderBase<TConfig>` provides:

```csharp
public abstract class InfrastructureProviderBase<TConfig> : ProviderBase<TConfig>, IInfrastructureProvider
{
    protected abstract Task<string[]> OnInitializeInfrastructureAsync(CancellationToken ct);
    protected abstract Task<string[]> OnDestroyInfrastructureAsync(CancellationToken ct);
    protected abstract Task<(string[] resources, Dictionary<string, object?>? metadata)> OnGetInfrastructureStatusAsync(CancellationToken ct);
    protected abstract Task<(string[] resources, Dictionary<string, object?>? changes)> OnPlanInfrastructureChangesAsync(CancellationToken ct);
}
```

**Key Responsibilities**:
- ✅ **Initialize**: Create required infrastructure (queues, topics, databases, storage accounts)
- ✅ **Plan**: Show what infrastructure changes would be made (like `terraform plan`)
- ✅ **Status**: Report current state of infrastructure resources
- ✅ **Destroy**: Clean up all created infrastructure resources

**Real-World Examples**:
- **Azure Service Bus Provider**: Create/destroy queues and topics
- **SQL Database Provider**: Create/destroy tables, indexes, procedures
- **AWS S3 Provider**: Create/destroy buckets and configure policies
- **Kubernetes Provider**: Deploy/remove pods, services, configmaps

## 🔧 Configuration (`Config.cs`)

Simple configuration class for provider settings:

```csharp
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
```

## 🚀 Entry Point (`Program.cs`)

Modern C# top-level statements:

```csharp
using Katasec.DStream.SDK.Core;
using ConsoleOutputProvider;

// Top-level program entry point
await StdioProviderHost.RunProviderWithCommandAsync<ConsoleOutputProvider.ConsoleOutputProvider, ConsoleOutputProvider.ConsoleConfig>();
```

**What `StdioProviderHost` handles for you**:
- ✅ JSON configuration parsing from stdin
- ✅ Command routing (`run`, `init`, `destroy`, `plan`, `status`)
- ✅ Envelope deserialization from stdin
- ✅ Process lifecycle and graceful shutdown
- ✅ Error handling and logging to stderr

## 📋 Provider Development Checklist

### For Output Providers (Data Processing):

1. **✅ Inherit from `ProviderBase<TConfig>`**
2. **✅ Implement `IOutputProvider`**
3. **✅ Implement `WriteAsync` method**:
   ```csharp
   public async Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct)
   {
       foreach (var envelope in batch)
       {
           if (ct.IsCancellationRequested) break;
           
           // Your business logic here:
           // - Transform envelope.Payload 
           // - Use envelope.Meta for routing
           // - Write to your destination
       }
   }
   ```

### For Infrastructure Providers (Optional):

4. **✅ Also inherit from `InfrastructureProviderBase<TConfig>`** 
5. **✅ Implement required abstract methods**:
   ```csharp
   protected override async Task<string[]> OnInitializeInfrastructureAsync(CancellationToken ct)
   {
       // Create your infrastructure (queues, databases, etc.)
       // Return list of created resource identifiers
   }
   
   protected override async Task<string[]> OnDestroyInfrastructureAsync(CancellationToken ct)
   {
       // Clean up your infrastructure
       // Return list of destroyed resource identifiers  
   }
   ```

## 🧪 Testing

### Test Individual Provider
```bash
# Test data processing
echo '{"outputFormat": "simple"}' | bin/Release/net9.0/osx-x64/console-output-provider

# Test infrastructure commands  
echo '{"command": "plan", "config": {"outputFormat": "simple", "resourceCount": 5}}' | bin/Release/net9.0/osx-x64/console-output-provider
echo '{"command": "init", "config": {"outputFormat": "simple", "resourceCount": 3}}' | bin/Release/net9.0/osx-x64/console-output-provider
echo '{"command": "status", "config": {"outputFormat": "simple"}}' | bin/Release/net9.0/osx-x64/console-output-provider
```

### Test Full Pipeline
```bash
# Via DStream CLI
cd ~/progs/dstream/dstream
go run . run counter-to-console          # Data processing
go run . plan counter-to-console         # Infrastructure planning (future)
go run . init counter-to-console         # Infrastructure provisioning (future)
```

## 🏗️ Building

```bash
# Clean build
make clean && make build

# Manual build  
/usr/local/share/dotnet/dotnet publish -c Release -r osx-x64 --self-contained
```

## 🎯 Key Benefits of This Architecture

1. **🧩 Clear Separation**: Data processing vs infrastructure concerns
2. **🔧 Maintainable**: Developers focus on business logic, DevOps on infrastructure
3. **🧪 Testable**: Each concern can be tested independently
4. **📦 Reusable**: Patterns work for any output provider (databases, queues, APIs)
5. **🔄 Scalable**: Infrastructure and data processing scale independently

## 📖 Related Documentation

- [DStream .NET SDK](../../README.md) - Main SDK documentation
- [DStream Architecture](../../../../../WARP.md) - Overall system design
- [Provider Development Guide](https://github.com/katasec/dstream-dotnet-sdk) - Step-by-step provider creation

## 🎪 Real-World Provider Examples

**This console provider demonstrates patterns used in production providers**:

- **Azure Service Bus Provider**: WriteAsync → Send to ASB, Infrastructure → Create/destroy queues
- **SQL Database Provider**: WriteAsync → Insert records, Infrastructure → Create tables/indexes  
- **AWS S3 Provider**: WriteAsync → Upload files, Infrastructure → Create/configure buckets
- **Elasticsearch Provider**: WriteAsync → Index documents, Infrastructure → Create indices/mappings

The same separation of concerns applies regardless of the destination technology.