# Katasec.DStream.SDK.Core

**Base classes and utilities for DStream plugin development**

This package provides the core implementation classes that plugin developers use, similar to how shared SDK cores provide common functionality across services.

## Key Components

### ProviderBase<TConfig>

The base class that all DStream providers inherit from:

```csharp
public abstract class ProviderBase<TConfig>
{
    protected TConfig Config { get; private set; }
    protected IPluginContext Ctx { get; private set; }
    
    public void Initialize(TConfig config, IPluginContext ctx) { ... }
    protected virtual void OnInitialized() { }
    protected Task EmitAsync(object payload, CancellationToken ct, 
        IDictionary<string, object?>? meta = null) { ... }
}
```

### Features

- **Configuration Binding** - Automatic config injection
- **Context Access** - Logger, services, emit functionality  
- **Lifecycle Hooks** - OnInitialized() for setup logic
- **Emit Helper** - Simplified downstream event emission

## Usage

Most plugin developers don't reference this directly - it comes transitively through `SDK.PluginHost`. However, if you're building infrastructure or need just the base classes:

```xml
<ProjectReference Include="Katasec.DStream.SDK.Core" />
```

## Architecture Position

- **SDK.PluginHost** - Main developer package
- **SDK.Core** ← This package - shared implementation
- **Abstractions** - Pure interfaces

This separation allows for clean architecture where interfaces are separate from implementation, following .NET design patterns.
