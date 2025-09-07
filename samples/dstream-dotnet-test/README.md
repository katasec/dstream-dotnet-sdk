# DStream .NET Sample Plugin

This sample demonstrates how to build a DStream plugin using the .NET SDK. It implements a simple counter plugin that generates incrementing numbers at configurable intervals.

## What This Sample Shows

- **Plugin Structure** - How to organize a DStream plugin project
- **Configuration** - Defining and using strongly-typed config
- **Provider Implementation** - Building an `IInputProvider`
- **SDK Usage** - Using `PluginHost.Run<>()` to bootstrap
- **Build Process** - Publishing cross-platform plugin executables

## Code Overview

### Configuration Class
```csharp
public sealed record GenericCounterConfig
{
    public int Interval { get; init; } = 5000; // milliseconds
}
```

### Plugin Implementation
```csharp
public sealed class GenericCounterPlugin : ProviderBase<GenericCounterConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        var hc = (HCLogger)ctx.Logger;
        hc.Info($"counter_start interval={Config.Interval}");

        for (int seq = 0; !ct.IsCancellationRequested; seq++)
        {
            await Task.Delay(Config.Interval, ct);
            
            var meta = new Dictionary<string, object?>
            {
                ["seq"] = seq,
                ["source"] = "counter"
            };

            yield return new Envelope(seq, meta);
        }
    }
}
```

### Bootstrap
```csharp
await PluginHost.Run<GenericCounterPlugin, GenericCounterConfig>();
```

## Building and Running

### Build the Plugin
```bash
# Using the provided build script
.\build.ps1 publish

# Or manually
dotnet publish -c Release -r win-x64 -o out
```

### Run via DStream CLI
```bash
# From the Go CLI project
cd C:\Users\ameer.deen\progs\dstream
go run . run dotnet-counter
```

### Configuration (dstream.hcl)
```hcl
task "dotnet-counter" {
  type = "plugin"
  plugin_path = "../dstream-dotnet-sdk/samples/dstream-dotnet-test/out/dstream-dotnet-test"
  
  config {
    interval = 1000  # 1 second intervals
  }
  
  input {
    provider = "null"
    config {}
  }
  
  output {
    provider = "console"
    config { format = "json" }
  }
}
```

## Project Structure

- **Program.cs** - Entry point and plugin implementation
- **build.ps1** - Cross-platform build script
- **dstream-dotnet-test.csproj** - Project configuration
- **out/** - Published plugin executables (created by build)

## Key Concepts Demonstrated

1. **Self-contained deployment** - Plugin runs as standalone executable
2. **gRPC integration** - Communicates with Go CLI via HashiCorp go-plugin
3. **Configuration binding** - HCL config automatically mapped to .NET objects
4. **Logging integration** - Uses HCLog.Net for HashiCorp-compatible logging
5. **Async streaming** - Generates events using `IAsyncEnumerable<Envelope>`

## Using as Template

This sample serves as a template for building your own DStream plugins. Simply:

1. Copy this project structure
2. Replace `GenericCounterPlugin` with your implementation
3. Update the config class for your needs
4. Implement `IInputProvider`, `IOutputProvider`, or both
5. Build and test with the DStream CLI
