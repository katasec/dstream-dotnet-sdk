# Katasec.DStream.SDK.PluginHost

**Main SDK package for DStream plugin developers**

This is the primary package you should reference when building DStream plugins. It provides everything you need to create plugins that integrate with the DStream CLI via HashiCorp go-plugin protocol.

## Usage

### 1. Reference this package
```xml
<ProjectReference Include="Katasec.DStream.SDK.PluginHost" />
```

### 2. Create your plugin
```csharp
using Katasec.DStream.SDK.PluginHost;
using Katasec.DStream.SDK.Core;
using Katasec.DStream.Abstractions;

// Define your config
public sealed record MyConfig
{
    public int Interval { get; init; } = 1000;
}

// Implement your provider
public sealed class MyPlugin : ProviderBase<MyConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(
        IPluginContext ctx, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Your plugin logic here
        yield return new Envelope("Hello", new Dictionary<string, object?>());
    }
}

// Bootstrap your plugin
await PluginHost.Run<MyPlugin, MyConfig>();
```

## What's Included

- **PluginHost.Run<>()** - Entry point for plugin execution
- **gRPC server implementation** - Handles HashiCorp go-plugin protocol
- **Configuration binding** - Automatic HCL → .NET config conversion
- **All dependencies** - Transitively includes SDK.Core and Abstractions

## Architecture

This package sits at the top of the SDK stack:
- **SDK.PluginHost** ← You reference this
- **SDK.Core** ← Base classes (ProviderBase, utilities)
- **Abstractions** ← Interfaces (IInputProvider, IOutputProvider)

The design follows modern SDK patterns where developers reference the main package and get everything they need.
