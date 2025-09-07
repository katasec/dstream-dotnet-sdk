# Katasec.DStream.Abstractions

**Core interfaces and contracts for the DStream SDK**

This package contains the fundamental interfaces and data types that define how DStream plugins work. It has no dependencies and provides the pure contracts.

## Key Interfaces

### Provider Interfaces

```csharp
// For plugins that generate data
public interface IInputProvider : IProvider
{
    IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct);
}

// For plugins that consume data  
public interface IOutputProvider : IProvider
{
    Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct);
}
```

### Runtime Context

```csharp
public interface IPluginContext
{
    object Logger { get; }           // HCLogger instance
    IServiceProvider Services { get; } // DI container
    Emit Emit { get; }              // Downstream emission
}
```

### Data Types

```csharp
// Core event container
public readonly record struct Envelope(
    object Payload, 
    IReadOnlyDictionary<string, object?> Meta
);

// Emission delegate
public delegate Task Emit(Envelope evt, CancellationToken ct);
```

## Design Philosophy

- **Pure interfaces** - No implementation details
- **Minimal dependencies** - Only standard .NET types
- **Event-driven** - Built around `Envelope` event model
- **Async-first** - All operations are async by default

## Usage

Most developers don't reference this directly - it comes transitively. However, if you're building framework-level components:

```xml
<ProjectReference Include="Katasec.DStream.Abstractions" />
```

## Architecture Position

- **SDK.PluginHost** - Main developer package
- **SDK.Core** - Base implementations  
- **Abstractions** ← This package - pure contracts

This follows the common .NET pattern of separating interfaces from implementations for clean architecture and testability.
