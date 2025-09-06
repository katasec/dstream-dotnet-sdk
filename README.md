# DStream .NET SDK

This SDK makes it easy to build **DStream plugins in .NET**.  
Plugins are loaded by the DStream CLI via [HashiCorp go-plugin](https://github.com/hashicorp/go-plugin) over gRPC.  
Each plugin defines a **Config**, a **Provider**, and implements one or more provider interfaces (`IInputProvider`, `IOutputProvider`, etc).

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
using Katasec.DStream.Host.Bridge;
using DStreamDotNetTest;

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

✅ With this model:
- **Sources** = `IInputProvider` (produce events, like the counter).  
- **Sinks**   = `IOutputProvider` (consume events, like ASB or Console).  
- **Config** is automatically bound from HCL → gRPC → `TConfig`.  
