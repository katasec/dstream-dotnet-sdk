# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Development Commands

### Building the Solution
```bash
# Build the entire solution
dotnet build dstream-dotnet-sdk.sln

# Build a specific project
dotnet build sdk/Katasec.DStream.SDK/Katasec.DStream.SDK.csproj

# Build in release mode
dotnet build dstream-dotnet-sdk.sln -c Release
```

### Running Tests
```bash
# Run all tests
dotnet test dstream-dotnet-sdk.sln

# Run tests for a specific project
dotnet test tests/Providers.AsbQueue.Tests/Providers.AsbQueue.Tests.csproj

# Run tests with verbose output
dotnet test dstream-dotnet-sdk.sln -v normal
```

### Sample Plugin Development
```bash
# Navigate to the sample project
cd samples/dstream-dotnet-test

# Build and publish the sample plugin
./build.ps1 publish

# Clean build outputs
./build.ps1 clean

# Manual publish (cross-platform)
dotnet publish dstream-dotnet-test.csproj -c Release -r win-x64 -o out
dotnet publish dstream-dotnet-test.csproj -c Release -r linux-x64 -o out
dotnet publish dstream-dotnet-test.csproj -c Release -r osx-x64 -o out
```

### Package Management
```bash
# Restore NuGet packages for all projects
dotnet restore dstream-dotnet-sdk.sln

# Clean all build outputs
dotnet clean dstream-dotnet-sdk.sln
```

## Architecture Overview

### Core Components

**SDK Architecture (Current - Recommended)**
- `Katasec.DStream.Abstractions`: Core interfaces (`IInputProvider`, `IOutputProvider`, `IPluginContext`)
- `Katasec.DStream.SDK`: Base classes (`ProviderBase<TConfig>`) and utilities
- `Katasec.DStream.Host.Bridge`: gRPC bridge for HashiCorp go-plugin integration

**Legacy Architecture (Being Phased Out)**
- `Katasec.DStream.Plugin`: Legacy plugin interfaces and registry
- `Katasec.DStream.Providers`: Legacy provider implementations

### Plugin Development Pattern

Plugins in this SDK follow a specific pattern:

1. **Config Class**: Defines plugin configuration
```csharp
public sealed record PluginConfig
{
    public int Interval { get; init; } = 5000;
}
```

2. **Provider Implementation**: Inherits from `ProviderBase<TConfig>` and implements provider interfaces
```csharp
public sealed class MyPlugin : ProviderBase<PluginConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct) { }
}
```

3. **Host Entry Point**: Uses `PluginHost.Run<>()` to bootstrap the plugin
```csharp
await PluginHost.Run<MyPlugin, PluginConfig>();
```

### Provider Types

**Input Providers** (`IInputProvider`)
- Read data from external sources
- Implement `ReadAsync()` returning `IAsyncEnumerable<Envelope>`
- Examples: Counter generators, database CDC, message queues

**Output Providers** (`IOutputProvider`)
- Write data to external destinations  
- Implement `WriteAsync()` accepting `IEnumerable<Envelope>`
- Examples: Console output, Azure Service Bus, databases

### Key Data Types

- `Envelope`: Core data structure with `Payload` (object) and `Meta` (metadata dictionary)
- `IPluginContext`: Runtime context providing logger and services
- `ProviderBase<TConfig>`: Base class handling configuration and context injection

### Project Structure

- `sdk/`: Current SDK implementation (use this)
- `providers/`: Sample provider implementations
- `samples/`: Example plugins and usage patterns
- `tests/`: Unit tests and test utilities
- `legacy/`: Deprecated components (avoid for new development)

### Integration with DStream CLI

Plugins are deployed as self-contained executables that communicate with the DStream CLI via gRPC using HashiCorp's go-plugin protocol. The plugin outputs a handshake message on startup and then serves gRPC requests for configuration binding and data processing.

### Development Notes

- Plugins must target .NET 9.0 or later
- Use `PublishSingleFile=true` for deployment to create standalone executables
- HCLogger (from HCLog.Net) is used for logging integration with HashiCorp tools
- Configuration is automatically bound from HCL to .NET config objects via JSON serialization
- The `[EnumeratorCancellation]` attribute is required on cancellation tokens in async enumerables

## Common Provider Patterns

### Input Provider Template
```csharp
public sealed class MyInputProvider : ProviderBase<MyConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        var log = (HCLogger)ctx.Logger;
        
        while (!ct.IsCancellationRequested)
        {
            // Read data from source
            var data = await ReadFromSource(ct);
            
            var meta = new Dictionary<string, object?> { ["source"] = "mysource" };
            yield return new Envelope(data, meta);
        }
    }
}
```

### Output Provider Template
```csharp
public sealed class MyOutputProvider : ProviderBase<MyConfig>, IOutputProvider
{
    public Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct)
    {
        var log = (HCLogger)ctx.Logger;
        
        foreach (var envelope in batch)
        {
            if (ct.IsCancellationRequested) break;
            // Write envelope.Payload to destination
            WriteToDestination(envelope.Payload, envelope.Meta, ct);
        }
        
        return Task.CompletedTask;
    }
}
```
