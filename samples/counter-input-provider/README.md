# Counter Input Provider Sample

A sample **DStream input provider** that demonstrates clean architecture patterns for building input providers. Generates sequential counter data with timestamps via stdin/stdout communication - perfect for testing output providers, validating data pipelines, and learning the DStream .NET SDK.

## 📁 File Structure

```
counter-input-provider/
├── Program.cs    ← Top-level statement entry point (5 lines)
├── Config.cs     ← Configuration class (CounterConfig)
└── Reader.cs     ← Core data reading logic (ReadAsync implementation)
```

## 🎯 Clean Architecture Pattern

This sample demonstrates **separation of concerns** - each file has a specific purpose:

### 🚀 Program.cs - Entry Point
**Purpose**: Bootstrap the provider with minimal ceremony
```csharp
using Katasec.DStream.SDK.Core;
using CounterInputProvider;

// Top-level program entry point
await StdioProviderHost.RunInputProviderAsync<CounterInputProvider.CounterInputProvider, CounterInputProvider.CounterConfig>();
```

### ⚙️ Config.cs - Configuration
**Purpose**: Define provider settings with JSON binding
```csharp
public sealed record CounterConfig
{
    /// <summary>Interval in milliseconds between counter increments</summary>
    public int Interval { get; init; } = 1000;
    
    /// <summary>Maximum number of items to generate (0 = infinite)</summary>
    public int MaxCount { get; init; } = 0;
}
```

### 🔧 Reader.cs - Core Business Logic
**Purpose**: Implement data generation logic  
**Interface**: `IInputProvider` from `Katasec.DStream.Abstractions`

```csharp
public interface IInputProvider : IProvider
{
    IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct);
}
```

**Why this interface?**
- ✅ **Streaming data generation**: Returns `IAsyncEnumerable<Envelope>` for continuous data flow
- ✅ **Envelope structure**: Wraps data + metadata for downstream processing
- ✅ **Cancellation support**: Respects `CancellationToken` for graceful shutdown
- ✅ **SDK integration**: Framework calls this method to get your data

**Key method - `ReadAsync`**:
```csharp
public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, [EnumeratorCancellation] CancellationToken ct)
{
    // Your data generation logic here:
    // - Generate data (counter, API calls, file reading, etc.)
    // - Create Envelope with payload + metadata  
    // - Use 'yield return' for streaming
    // - Handle cancellation gracefully
}
```

## 📊 What This Provider Generates

- **Sequential numbers** (1, 2, 3, ...) with timestamps
- **Configurable intervals** between increments
- **Optional max count** for finite sequences
- **Rich metadata** (sequence numbers, provider info)
- **JSON-structured envelopes** for downstream consumption

## Configuration

Accepts JSON configuration via stdin:

```json
{
  "interval": 1000,
  "maxCount": 10
}
```

**Options:**
- `interval` - Milliseconds between counter increments (default: 1000)
- `maxCount` - Maximum items to generate, 0 = infinite (default: 0)

## Usage

### Standalone Testing

```bash
# Generate 3 counter items with 1 second intervals
echo '{"interval": 1000, "maxCount": 3}' | dotnet run
```

### Example Output

```json
{"source":"","type":"","data":{"value":1,"timestamp":"2025-09-20T14:17:11.800128+00:00"},"metadata":{"seq":1,"source":"counter-input-provider","interval_ms":1000}}
{"source":"","type":"","data":{"value":2,"timestamp":"2025-09-20T14:17:12.840258+00:00"},"metadata":{"seq":2,"source":"counter-input-provider","interval_ms":1000}}
{"source":"","type":"","data":{"value":3,"timestamp":"2025-09-20T14:17:13.841851+00:00"},"metadata":{"seq":3,"source":"counter-input-provider","interval_ms":1000}}
```

## Building

```bash
# Build debug version
dotnet build

# Build release version  
dotnet build -c Release

# Publish self-contained binary
dotnet publish -c Release -r osx-arm64 --self-contained
```

## DStream Integration

### Task Configuration (HCL)

```hcl
task "counter-demo" {
  input {
    provider_path = "./counter-input-provider"
    config = {
      interval = 1000
      maxCount = 10
    }
  }
  
  output {
    provider_path = "./some-output-provider"
    config = {
      destination = "demo-output"
    }
  }
}
```

## 📋 Development Checklist

### For Input Providers (Data Generation):

1. **✅ Create Config.cs** - Define your provider's configuration needs
2. **✅ Inherit from `ProviderBase<TConfig>`** in Reader.cs
3. **✅ Implement `IInputProvider`** interface
4. **✅ Implement `ReadAsync` method** with proper `[EnumeratorCancellation]` attribute
5. **✅ Use `yield return`** for streaming data generation
6. **✅ Handle `CancellationToken`** for graceful shutdown
7. **✅ Create rich metadata** for downstream processing
8. **✅ Bootstrap with top-level statements** in Program.cs

## 🎯 Architecture Benefits

- **Type**: Input Provider (generates data)
- **Protocol**: stdin/stdout JSON communication  
- **Framework**: .NET 9.0 with DStream .NET SDK
- **Runtime**: Self-contained executable (~68MB)
- **Pattern**: Clean separation of concerns (config/business logic/entry point)

### Key Benefits:
1. **🧩 Clear Separation**: Configuration, business logic, and entry point are isolated
2. **🔧 Maintainable**: Easy to modify data generation logic in Reader.cs
3. **🧪 Testable**: Each component can be tested independently
4. **📦 Reusable**: Pattern works for any input provider (APIs, databases, files)
5. **⚡ Modern**: Uses latest C# patterns (top-level statements, records)

This demonstrates how simple it is to create a DStream input provider with clean architecture - each file has a focused purpose and the SDK handles all the plumbing!
