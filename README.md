# DStream .NET SDK

A modern .NET SDK for building **DStream providers** using stdin/stdout communication.
Providers are simple standalone binaries that communicate with the DStream CLI via JSON over stdin/stdout pipes.  
Each provider defines a **Config**, a **Provider class**, and implements either `IInputProvider` or `IOutputProvider`.

## Quick Start

**1. Reference the SDK:**
```xml
<ProjectReference Include="../dstream-dotnet-sdk/sdk/Katasec.DStream.Abstractions/Katasec.DStream.Abstractions.csproj" />
<ProjectReference Include="../dstream-dotnet-sdk/sdk/Katasec.DStream.SDK.Core/Katasec.DStream.SDK.Core.csproj" />
```

**2. Create your provider (top-level statements):**
```csharp
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

// Simple top-level program entry point
await StdioProviderHost.RunInputProviderAsync<MyInputProvider, MyConfig>();
// or
await StdioProviderHost.RunOutputProviderAsync<MyOutputProvider, MyConfig>();
```

**That's it!** The SDK handles all the stdin/stdout plumbing, configuration parsing, JSON serialization, and process lifecycle management.

---

## Provider Basics

1. **Config class**  
   Each provider defines a config model for its settings. Example:

   ```csharp
   public sealed record CounterConfig
   {
       public int Interval { get; init; } = 1000;
       public int MaxCount { get; init; } = 0;
   }
   ```

2. **Provider base**  
   Providers inherit from `ProviderBase<TConfig>`. This gives them access to `Config` (populated by the SDK at runtime).

   ```csharp
   public abstract class ProviderBase<TConfig>
   {
       protected TConfig Config { get; private set; }
       protected IPluginContext Ctx { get; private set; }
       public void Initialize(TConfig config, IPluginContext ctx) { ... }
   }
   ```

3. **Provider interfaces**  
   - `IInputProvider`: produces `Envelope` events via `IAsyncEnumerable<Envelope> ReadAsync()`.  
   - `IOutputProvider`: consumes `Envelope` events via `Task WriteAsync(IEnumerable<Envelope> batch, ...)`.  
   - Each provider implements exactly one interface (input OR output, not both).

---

## Example: Counter Input Provider

This provider generates an incrementing counter every `Interval` milliseconds.

```csharp
using System.Runtime.CompilerServices;
using Katasec.DStream.Abstractions;
using Katasec.DStream.SDK.Core;

// Simple top-level program entry point
await StdioProviderHost.RunInputProviderAsync<CounterInputProvider, CounterConfig>();

public class CounterInputProvider : ProviderBase<CounterConfig>, IInputProvider
{
    public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, [EnumeratorCancellation] CancellationToken ct)
    {
        var count = 0;
        
        while (!ct.IsCancellationRequested)
        {
            count++;
            
            // Stop if max count reached
            if (Config.MaxCount > 0 && count > Config.MaxCount)
                break;

            // Create counter data
            var data = new { value = count, timestamp = DateTimeOffset.UtcNow };
            var metadata = new Dictionary<string, object?>
            {
                ["seq"] = count,
                ["provider"] = "counter-input-provider"
            };
            
            yield return new Envelope(data, metadata);
            
            await Task.Delay(Config.Interval, ct);
        }
    }
}

public sealed record CounterConfig
{
    public int Interval { get; init; } = 1000;
    public int MaxCount { get; init; } = 0;
}
```

---

## Running Providers

Providers are standalone binaries that communicate via stdin/stdout:

```bash
# Test input provider directly:
echo '{"interval": 500, "max_count": 3}' | ./counter-input-provider

# Test output provider directly:
echo '{"outputFormat": "simple"}' | ./console-output-provider

# Test full pipeline manually:
echo '{"interval": 500, "max_count": 3}' | ./counter-input-provider 2>/dev/null | echo '{"outputFormat": "simple"}' | ./console-output-provider
```

---

## Task Configuration (HCL)

Your providers can be orchestrated by DStream CLI using `dstream.hcl`:

```hcl
task "counter-to-console" {
  input {
    provider_path = "./counter-input-provider"
    config = {
      interval = 1000
      max_count = 10
    }
  }

  output {
    provider_path = "./console-output-provider"
    config = {
      outputFormat = "simple"
    }
  }
}
```

---

## SDK Architecture

The DStream .NET SDK uses a simple stdin/stdout architecture:

- **`Katasec.DStream.Abstractions`** - Core interfaces (`IInputProvider`, `IOutputProvider`, `Envelope`)
- **`Katasec.DStream.SDK.Core`** - Base classes (`ProviderBase<TConfig>`) and `StdioProviderHost`

### Provider Development Flow

1. **Reference SDK**: Add project references to `Abstractions` and `SDK.Core`
2. **Define Config**: Record class for your provider settings
3. **Implement Provider**: Inherit from `ProviderBase<TConfig>` and implement `IInputProvider` or `IOutputProvider`
4. **Bootstrap**: Call `StdioProviderHost.RunInputProviderAsync<>()` or `RunOutputProviderAsync<>()`

### Architecture Benefits

✅ **Unix Pipeline Philosophy**:
- **Input Providers** = `IInputProvider` (generate data streams, like counters, CDC, APIs)
- **Output Providers** = `IOutputProvider` (consume data streams, like databases, queues, files)
- **Communication** = JSON over stdin/stdout (universal, testable, debuggable)
- **Process Model** = One binary per provider (independent, scalable, fault-isolated)
- **Configuration** = JSON via stdin (simple, language-agnostic)
- **Developer Experience** = Write business logic, SDK handles everything else

## Getting Started

See the example providers:
- [Counter Input Provider](https://github.com/katasec/dstream-counter-input-provider)
- [Console Output Provider](https://github.com/katasec/dstream-console-output-provider)

For detailed development guidance, see [WARP.md](./WARP.md).
