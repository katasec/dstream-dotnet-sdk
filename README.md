# DStream .NET SDK

A modern .NET SDK for building **DStream plugins**.
Plugins are loaded by the DStream CLI via [HashiCorp go-plugin](https://github.com/hashicorp/go-plugin) over gRPC.  
Each plugin defines a **Config**, a **Provider**, and implements one or more provider interfaces (`IInputProvider`, `IOutputProvider`, etc).

## Quick Start

**1. Reference the SDK:**
```xml
<ProjectReference Include="Katasec.DStream.SDK.PluginHost" />
```

**2. Create your plugin:**
```csharp
using Katasec.DStream.SDK.PluginHost;
using Katasec.DStream.SDK.Core;
using Katasec.DStream.Abstractions;

await PluginHost.Run<MyPlugin, MyConfig>();
```

**That's it!** The SDK handles all the gRPC plumbing, configuration binding, and HashiCorp go-plugin protocol details.

---

## Plugin Basics

1. **Config class**  
   Each plugin defines a config model for its settings. Example:

   ```csharp
   public sealed class GenericCounterConfig
   {
       public int Interval { get; set; } = 5000;
   }
   ```

2. **Provider base**  
   Providers inherit from `ProviderBase<TConfig>`. This gives them access to `Config` (populated by the host at runtime).

   ```csharp
   public abstract class ProviderBase<TConfig>
   {
       protected TConfig Config { get; private set; }
       protected IPluginContext Context { get; private set; }
       public void Initialize(TConfig config, IPluginContext ctx) { ... }
   }
   ```

3. **Provider interfaces**  
   - `IInputProvider`: produces `Envelope` events (`ReadAsync`).  
   - `IOutputProvider`: consumes `Envelope` events (`WriteAsync`).  
   - A provider can implement one, both, or future extensions.

---

## Example: Counter Plugin (Input Provider)

This plugin generates an incrementing counter every `Interval` milliseconds.

```csharp
public sealed class GenericCounterPlugin 
    : ProviderBase<GenericCounterConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(
        IPluginContext ctx,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var log = (HCLog.Net.HCLogger)ctx.Logger;
        log.Info($"counter_start interval={Config.Interval}");

        var seq = 0;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(Config.Interval, ct);
            seq++;

            var meta = new Dictionary<string, object?> 
            { 
                ["seq"] = seq, 
                ["source"] = "counter" 
            };

            yield return new Envelope(seq, meta);
        }

        log.Info("counter_complete");
    }
}
```

---

## Running the Plugin

In your sample app:

```csharp
using Katasec.DStream.SDK.PluginHost;
using Katasec.DStream.SDK.Core;
using Katasec.DStream.Abstractions;

await PluginHost.Run<GenericCounterPlugin, GenericCounterConfig>();
```

This bootstraps the gRPC server, wires in your plugin, and hands control to the DStream CLI.

---

## HCL Configuration

Your plugin can be launched by DStream CLI using `dstream.hcl`:

```hcl
task "dotnet-counter" {
  type        = "plugin"
  plugin_path = "../dstream-dotnet-sdk/samples/dstream-dotnet-test/out/dstream-dotnet-test"

  config {
    interval = 1000
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

---

## SDK Architecture

The DStream .NET SDK follows modern patterns:

- **`Katasec.DStream.SDK.PluginHost`** - Main package for plugin developers (reference this)
- **`Katasec.DStream.SDK.Core`** - Base classes (`ProviderBase<TConfig>`)
- **`Katasec.DStream.Abstractions`** - Core interfaces (`IInputProvider`, `IOutputProvider`)

### Plugin Development Flow

1. **Reference SDK**: `Katasec.DStream.SDK.PluginHost`
2. **Define Config**: POCO class for your plugin settings
3. **Implement Provider**: Inherit from `ProviderBase<TConfig>`
4. **Bootstrap**: Call `PluginHost.Run<TPlugin, TConfig>()`

### Integration

✅ **Modern Architecture**:
- **Sources** = `IInputProvider` (produce events, like the counter)
- **Sinks** = `IOutputProvider` (consume events, like ASB or Console)
- **Config** is automatically bound from HCL → gRPC → `TConfig`
- **Clean builds** with no warnings from generated code
- **Simple developer experience** - reference one package and go!

## Getting Started

See the [sample project](./samples/dstream-dotnet-test/) for a complete working example.

For detailed development guidance, see [WARP.md](./WARP.md).
