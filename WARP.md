# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Development Commands

### Building the Solution
```bash
# Build the entire solution
dotnet build dstream-dotnet-sdk.sln

# Build a specific project
dotnet build sdk/Katasec.DStream.SDK.Core/Katasec.DStream.SDK.Core.csproj

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

### Running Plugins via DStream CLI
```bash
# Navigate to the Go CLI project
cd C:\Users\ameer.deen\progs\dstream

# Run a specific task defined in dstream.hcl
go run . run dotnet-counter

# Run with debug logging
go run . run dotnet-counter --log-level debug

# Example dstream.hcl task configuration:
# task "dotnet-counter" {
#   type = "plugin"
#   plugin_path = "../dstream-dotnet-sdk/samples/dstream-dotnet-test/out/dstream-dotnet-test"
#   config { interval = 500 }
#   input { provider = "null"; config {} }
#   output { provider = "console"; config { format = "json" } }
# }
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
- `Katasec.DStream.SDK.Core`: Base classes (`ProviderBase<TConfig>`) and utilities
- `Katasec.DStream.SDK.PluginHost`: Main SDK package for plugin developers (gRPC bridge for HashiCorp go-plugin integration)

**Legacy Architecture (Removed)**
- Legacy components have been removed after successful migration to new SDK

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
  - `Katasec.DStream.Abstractions/`: Core interfaces
  - `Katasec.DStream.SDK.Core/`: Base classes and utilities
  - `Katasec.DStream.SDK.PluginHost/`: Main SDK package (reference this for plugin development)
- `providers/`: Sample provider implementations
- `samples/`: Example plugins and usage patterns
- `tests/`: Unit tests and test utilities
- Legacy components have been removed

### Developer Experience

**Plugin developers only need to reference one package:**
```xml
<ProjectReference Include="Katasec.DStream.SDK.PluginHost" />
```

This follows AWS SDK patterns where developers reference the main SDK package (like `AWS.SDK.S3`) rather than internal implementation details.

### Integration with DStream CLI

The DStream CLI is a Go application located at `C:\Users\ameer.deen\progs\dstream` that serves as the host for .NET plugins. The integration works as follows:

**Plugin Lifecycle:**
1. Go CLI parses `dstream.hcl` configuration file
2. CLI launches .NET plugin executable as subprocess using `exec.Command()`
3. .NET plugin starts gRPC server and outputs handshake: `1|1|tcp|127.0.0.1:{port}|grpc`
4. Go CLI connects to plugin's gRPC server
5. CLI sends `StartRequest` with config, input, and output provider settings
6. Plugin runs until cancelled by CLI

**gRPC Interface (defined in `proto/plugin.proto`):**
```protobuf
service Plugin {
  rpc GetSchema (google.protobuf.Empty) returns (GetSchemaResponse);
  rpc Start (StartRequest) returns (google.protobuf.Empty);
}
```

**Configuration Flow:**
- HCL config → Go CLI → JSON → gRPC `StartRequest` → .NET deserialization → Plugin config
- Configuration includes global plugin settings, input provider config, and output provider config

### Development Notes

**Technical Requirements:**
- Plugins must target .NET 9.0 or later
- Use `PublishSingleFile=true` for deployment to create standalone executables
- All plugins must implement gRPC server using ASP.NET Core + Kestrel (HTTP/2)
- Plugins communicate exclusively via gRPC (HashiCorp go-plugin protocol)

**Logging Integration:**
- HCLogger (from HCLog.Net) is used for logging integration with HashiCorp tools
- Logs are written to stderr (stdout is reserved for handshake protocol)
- Log format is compatible with go-hclog JSON structure

**Configuration System:**
- Configuration is automatically bound from HCL → JSON → .NET config objects
- Uses `google.protobuf.Struct` for config transport over gRPC
- Plugin receives global config, input config, and output config separately
- The `[EnumeratorCancellation]` attribute is required on cancellation tokens in async enumerables

**Plugin Detection:**
- CLI detects plugins via environment variables: `PLUGIN_PROTOCOL_VERSIONS`, `PLUGIN_MIN_PORT`
- Direct execution shows HashiCorp-style warning message

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
