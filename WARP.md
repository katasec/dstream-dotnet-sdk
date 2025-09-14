# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Development Commands

### Environment Configuration

**PowerShell on macOS:**
- .NET path: `/usr/local/share/dotnet/dotnet`
- Use full path when running dotnet commands in PowerShell

### Building the Solution
```bash
# Build the entire solution
/usr/local/share/dotnet/dotnet build dstream-dotnet-sdk.sln

# Build a specific project
/usr/local/share/dotnet/dotnet build sdk/Katasec.DStream.SDK.Core/Katasec.DStream.SDK.Core.csproj

# Build in release mode
/usr/local/share/dotnet/dotnet build dstream-dotnet-sdk.sln -c Release
```

### Running Tests
```bash
# Run all tests
/usr/local/share/dotnet/dotnet test dstream-dotnet-sdk.sln

# Run tests for a specific project
/usr/local/share/dotnet/dotnet test tests/Providers.AsbQueue.Tests/Providers.AsbQueue.Tests.csproj

# Run tests with verbose output
/usr/local/share/dotnet/dotnet test dstream-dotnet-sdk.sln -v normal
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
/usr/local/share/dotnet/dotnet publish dstream-dotnet-test.csproj -c Release -r win-x64 -o out
/usr/local/share/dotnet/dotnet publish dstream-dotnet-test.csproj -c Release -r linux-x64 -o out
/usr/local/share/dotnet/dotnet publish dstream-dotnet-test.csproj -c Release -r osx-x64 -o out
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
/usr/local/share/dotnet/dotnet restore dstream-dotnet-sdk.sln

# Clean all build outputs
/usr/local/share/dotnet/dotnet clean dstream-dotnet-sdk.sln
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

## Architectural Decisions

### Provider I/O Model: Stdin/Stdout Streaming over gRPC
er/Writer Model

**Key Insight: One Task = One Command = Three Processes**

DStream uses a **task-centric execution model** where each streaming job runs as an independent task with clean isolation:

```bash
# Each command runs one isolated streaming task
dstream run dotnet-counter    # Demo: Counter → Console
dstream run mssql-cdc         # Production: SQL Server → Azure Service Bus  
dstream run api-monitor       # API polling → Multiple outputs
```

**Task Execution Model:**
```
┌─────────────────────────────────────────────────────────────┐
│  Task: "dotnet-counter"                                     │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────────┐   │
│  │ DStream CLI │   │ Counter     │   │ Console Output  │   │
│  │ (Process 1) │◄─►│ Input       │◄─►│ Provider        │   │
│  │             │   │ (Process 2) │   │ (Process 3)     │   │
│  └─────────────┘   └─────────────┘   └─────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**Data Flow: Simple stdin/stdout Piping**
```
┌──────────────┐  stdout   ┌─────────────────┐  stdin   ┌─────────────────┐
│ Input        │  JSON     │ DStream CLI     │  JSON    │ Output          │
│ Provider     │──────────→│ (Orchestrator)  │─────────→│ Provider        │
│              │ envelopes │ • Launch tasks  │envelopes │                 │
│              │           │ • Pipe data     │          │                 │
│              │           │ • Manage procs  │          │                 │
└──────────────┘           └─────────────────┘          └─────────────────┘
```

**Benefits of Task-Based Isolation:**
- **Process Isolation**: Each task runs independently with no shared state
- **Simple I/O Model**: Providers only need to handle stdin/stdout (like a single process with input/output)
- **Easy Testing**: Each provider can be tested independently with shell commands
- **Clean Shutdown**: Killing CLI process cleanly terminates all 3 processes
- **Resource Boundaries**: Clear CPU/memory limits per streaming task
- **Operational Simplicity**: One command = one streaming job (easy monitoring)

### Configuration: HCL-Based Task Definitions

**Decision:** DStream uses a **Reader/Writer abstraction** where input providers are essentially streaming readers and output providers are streaming writers, communicating over gRPC via HashiCorp go-plugin protocol.

**The Key Insight: Native Streaming APIs over gRPC**

This is fundamentally **"Native streaming patterns for structured data over gRPC"** - each language uses its idiomatic streaming abstractions:

**Language-Specific API Mappings:**

**Go (CLI Orchestration):**
```go
// Input Provider = io.Reader pattern
type InputProvider interface {
    Read(ctx context.Context) (<-chan Envelope, error)
}

// Output Provider = io.Writer pattern  
type OutputProvider interface {
    Write(ctx context.Context, envelopes <-chan Envelope) error
}

// Go CLI = Data Pump (like Unix pipe)
func PumpData(reader InputProvider, writer OutputProvider) {
    envelopes, _ := reader.Read(ctx)
    writer.Write(ctx, envelopes)
}
```

**.NET (Provider Implementation):**
```csharp
// Input Provider = IAsyncEnumerable<T> (streaming read)
public interface IInputProvider : IProvider
{
    IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct);
    // ↑ Like Stream.ReadAsync() but for structured data
}

// Output Provider = async batch writer
public interface IOutputProvider : IProvider  
{
    Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct);
    // ↑ Like Stream.WriteAsync() but for structured data
}
```

**Runtime Architecture:**
```
┌─────────────────┐    gRPC     ┌─────────────────┐    gRPC     ┌─────────────────┐
│  Input Provider │────────────▶│     Go CLI      │────────────▶│ Output Provider │
│   (.NET Stream) │             │ (io.Reader/     │             │   (.NET Stream) │  
│                 │             │  io.Writer)     │             │                 │
└─────────────────┘             └─────────────────┘             └─────────────────┘
```

**Cross-Language Composability:**
```bash
# Any language can implement providers using native patterns
dotnet-mssql-provider → go-cli → rust-kafka-provider
python-api-provider → go-cli → java-elasticsearch-provider
go-counter-provider → go-cli → dotnet-console-provider
```

**Why this works:**
- **Native patterns:** Each language uses its idiomatic streaming APIs
- **Familiar abstractions:** Go = `io.Reader`/`io.Writer`, .NET = `IAsyncEnumerable`/`WriteAsync`
- **Composable:** Any reader can connect to any writer, regardless of language
- **gRPC abstraction:** Network transport is hidden behind native APIs
- **Battle-tested:** Built on proven streaming patterns from each ecosystem

### Provider Distribution: Independent Binaries

**Decision:** Each input/output provider is an **independent executable binary** distributed as OCI images.

**Options Considered:**

**❌ Option A: Library Loading (NuGet packages)**
- Providers as NuGet packages loaded into single plugin binary
- Complex dependency management
- Coordination required between provider authors
- Difficult ecosystem growth

**✅ Option B: Independent Binaries (Chosen)**
- Each provider is its own executable
- Distributed via OCI container registries
- Zero coordination between provider authors
- Natural ecosystem growth

**Task Configuration (HCL):**
```hcl
# dotnet-counter.hcl - Simple demo task
task "dotnet-counter" {
  input {
    provider_path = "./counter-input-provider"     # Development: local binary
    # provider_href = "ghcr.io/org/counter:v1.0.0"  # Production: OCI image
    config = {
      interval = 1000
      max_count = 100
    }
  }
  
  output {
    provider_path = "./console-output-provider"     # Development: local binary
    # provider_href = "ghcr.io/org/console:v1.0.0"   # Production: OCI image
    config = {
      outputFormat = "structured"
    }
  }
}

# mssql-cdc.hcl - Production CDC task
task "mssql-cdc" {
  input {
    provider_href = "ghcr.io/katasec/mssql-cdc:v1.0.0"  # Production OCI
    config = {
      connection_string = "Server=...;Database=..."
      tables = ["orders", "customers"]
    }
  }
  output {
    provider_href = "ghcr.io/katasec/azure-servicebus:v2.1.0"
    config = {
      connection_string = "Endpoint=sb://..."
      topic_name = "cdc-events"
    }
  }
}
```

**Provider Discovery Evolution:**
- **Phase 1 (Now)**: `provider_path` for local binaries during development
- **Phase 2 (Future)**: `provider_href` for OCI container images in production
- **Configuration**: HCL (not JSON/YAML) - Terraform-style infrastructure-as-code

**Benefits:**
- **Publishing:** Anyone can publish a provider instantly
- **Versioning:** Granular versioning per provider
- **Security:** Audit each provider independently
- **Ecosystem:** No gatekeepers, natural marketplace emerges

**Trade-offs:**
- Higher runtime overhead (2+ processes vs 1)
- More complex CLI orchestration
- Inter-process communication complexity

### Transform Strategy: Queue Chaining

**Decision:** Transforms happen via **queue chaining** rather than separate transform processes.

**Options Considered:**

**❌ Option A: Separate Transform Process**
```
Go CLI → Input Provider → Transform Provider → Output Provider
```
- Too complex (3-process orchestration)
- Complex IPC between all components

**✅ Option B: Embedded Transforms (Chosen)**
- Simple transforms embedded in input/output providers
- Complex transforms via queue chaining

**✅ Option C: Queue Chaining (Chosen)**
```
Input → ASB Queue → Transform Process → ASB Queue → Output
```

**Implementation Patterns:**

**Simple Inline Transforms:**
```csharp
public async IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct)
{
    await foreach (var rawEvent in ReadFromSource(ct))
    {
        // Transform before emitting
        var transformed = CleanAndNormalize(rawEvent);
        yield return new Envelope(transformed, metadata);
    }
}
```

**Queue Chaining:**
```hcl
# Stage 1: Raw ingestion
task "ingest" {
  input  { provider_ref = "mssql-cdc" }
  output { provider_ref = "azure-servicebus"; queue = "raw-events" }
}

# Stage 2: Transform
task "transform" {
  input  { provider_ref = "azure-servicebus"; queue = "raw-events" }
  output { provider_ref = "azure-servicebus"; queue = "enriched-events" }
}

# Stage 3: Final destination  
task "sink" {
  input  { provider_ref = "azure-servicebus"; queue = "enriched-events" }
  output { provider_ref = "snowflake-sink" }
}
```

**Benefits:**
- **Fault tolerance:** Queue durability between stages
- **Scalability:** Independent scaling per stage
- **Operations:** Clear monitoring boundaries
- **Testing:** Easy replay and debugging

### Provider Interface Design

**Input Provider Interface:**
```csharp
public interface IInputProvider : IProvider
{
    IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct);
}
```

**Output Provider Interface:**
```csharp
public interface IOutputProvider : IProvider
{
    Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct);
}
```

**Core Data Structure:**
```csharp
public readonly record struct Envelope(object Payload, IReadOnlyDictionary<string, object?> Meta);
```

### Development Workflow

**Provider Development:**
1. Implement `IInputProvider` or `IOutputProvider`
2. Build as self-contained executable
3. Package as OCI image
4. Publish to container registry
5. Users reference via `provider_ref` in HCL

**Runtime Workflow:**
1. `dstream init` - Downloads required provider images
2. `dstream run` - Launches input/output providers as separate processes
3. Go CLI orchestrates data flow between providers
4. Providers communicate via gRPC using HashiCorp go-plugin protocol

**Target Ecosystem:**
- **Input Providers:** SQL Server CDC, PostgreSQL CDC, Kafka, REST APIs, File watchers
- **Output Providers:** Azure Service Bus, Amazon SQS, Snowflake, Elasticsearch, webhooks
- **Transform Providers:** Data enrichment, validation, aggregation, ML inference

This architecture enables a "Terraform for data streaming" ecosystem where providers are composable, independently versioned, and community-contributed.

## Architecture Decision: Unix stdin/stdout vs HashiCorp go-plugin (gRPC)

### Decision Context

DStream was initially designed using HashiCorp's go-plugin framework with gRPC communication, chosen for three key factors:
1. **Battle-tested**: Proven in Terraform, Vault, Consul, Packer
2. **Language support**: gRPC works across multiple languages
3. **Performance**: Binary protocol with efficient serialization

However, during development of the independent provider orchestration model, we evaluated **Unix stdin/stdout pipes** as an alternative IPC mechanism.

### Comparison Analysis

| **Factor** | **HashiCorp go-plugin (gRPC)** | **Unix stdin/stdout** | **Winner** |
|------------|--------------------------------|------------------------|------------|
| **🛡️ Battle Tested** | ✅ **Excellent** | ✅ **Legendary** | **stdin/stdout** |
| | • Used by Terraform, Vault, Consul, Packer | • Unix foundation since 1970s | |
| | • Handles process lifecycle, crashes, recovery | • Every shell, container, CI/CD system | |
| | • Plugin discovery and versioning | • Docker, Kubernetes, systemd native | |
| | • 5+ years production at HashiCorp scale | • **50+ years** in production everywhere | |
| **🌐 Language Support** | ✅ **Very Good** | ✅ **Universal** | **stdin/stdout** |
| | • Go (native), .NET, Python, Java, Rust | • **Every programming language** | |
| | • Requires gRPC libraries and proto files | • Built into language standard libraries | |
| | • Need to implement plugin interface | • Just read/write text (JSON/CSV/etc.) | |
| | • Proto compatibility across versions | • No dependencies, just IO streams | |
| **⚡ Performance** | ✅ **Excellent** | 🤔 **Good** | **Depends on use case** |
| | • Binary protocol, efficient serialization | • Text-based JSON parsing overhead | |
| | • Streaming, bidirectional communication | • Sequential pipe processing | |
| | • Type-safe, schema validation | • String parsing/validation needed | |
| | • HTTP/2 multiplexing, compression | • Simple pipe buffering | |

### Critical Performance Factor: Interprocess Communication Latency

| **Metric** | **gRPC (TCP/HTTP2)** | **Unix Pipes (stdin/stdout)** | **Winner** |
|------------|----------------------|-------------------------------|------------|
| **Base Latency** | ~50-200μs | ~1-10μs | **Pipes** |
| **Connection Setup** | TCP handshake + HTTP2 setup | Instant (kernel pipe) | **Pipes** |
| **Memory Copies** | Multiple (network stack) | Single (kernel buffer) | **Pipes** |
| **Context Switches** | User → Kernel → Network → Kernel → User | User → Kernel → User | **Pipes** |
| **Protocol Overhead** | HTTP2 headers + protobuf framing | Raw bytes | **Pipes** |
| **Throughput** | ~100K-500K msgs/sec | ~1M+ msgs/sec | **Pipes** |

**Key Insight**: Unix pipes provide **10-50x faster interprocess communication** than gRPC for local process orchestration.

### Real-World DStream Scenarios

#### Scenario 1: Azure Activity Logs → Azure Service Bus
```hcl
task "activity-logs-to-servicebus" {
  type = "providers"
  
  input {
    provider_path = "./azure-activity-logs-provider"
    config = {
      subscription_id = "12345678-1234-1234-1234-123456789012"
      resource_groups = ["production", "staging"]  
      polling_interval = "30s"
    }
  }
  
  output {
    provider_path = "./azure-servicebus-provider"
    config = {
      connection_string = "Endpoint=sb://..."
      queue_name = "activity-logs"
      batch_size = 100
    }
  }
}
```

**Data Flow**:
```
Azure Activity Logs API → Input Provider → stdout → CLI → stdin → Output Provider → Service Bus Queue
```

**Testing Capability**:
```bash
# Test input provider independently:
echo '{"subscription_id":"...","polling_interval":"30s"}' | ./azure-activity-logs-provider

# Test output provider independently:  
echo '{"connection_string":"...","queue_name":"test"}' | ./azure-servicebus-provider

# Test full pipeline manually:
echo '{"subscription_id":"..."}' | ./azure-activity-logs-provider | ./azure-servicebus-provider
```

#### Scenario 2: MS SQL CDC → Azure Data Factory
```hcl
task "mssql-cdc-to-adf" {
  type = "providers"
  
  input {
    provider_path = "./mssql-cdc-provider"
    config = {
      connection_string = "Server=sql.company.com;Database=Orders;..."
      tables = ["Orders", "Customers", "OrderItems"]
      cdc_start_lsn = "auto"
      polling_interval = "5s"
      batch_size = 1000
    }
  }
  
  output {
    provider_path = "./azure-datafactory-provider"  
    config = {
      subscription_id = "12345678-1234-1234-1234-123456789012"
      resource_group = "data-engineering"
      factory_name = "company-adf"
      pipeline_name = "ingest-cdc-changes"
    }
  }
}
```

**High-Frequency CDC Example**:
```bash
# SQL Server CDC processing 10,000 changes/second:

# With gRPC:
10,000 msgs × 100μs latency = 1 second just in IPC overhead
+ Processing time = significant bottleneck

# With Pipes:  
10,000 msgs × 5μs latency = 50ms in IPC overhead
+ Processing time = IPC negligible
```

### Final Architecture Decision: Unix stdin/stdout

**Decision**: DStream adopts **Unix stdin/stdout pipes** for independent provider orchestration.

**Rationale**:
1. **Superior IPC Performance**: 10-50x faster interprocess communication
2. **Universal Language Support**: Every programming language supports stdin/stdout natively
3. **Legendary Battle Testing**: 50+ years of production use in Unix systems
4. **Perfect Alignment**: Matches DStream's vision as "Unix pipeline for data"
5. **Developer Experience**: Trivial testing with shell commands
6. **Operational Simplicity**: Standard Unix tooling (pipes, redirects, etc.)

**Trade-offs Accepted**:
- Text-based JSON parsing overhead vs binary protobuf (negligible for I/O-bound workloads)
- Manual schema validation vs automatic protobuf validation
- Simple sequential processing vs complex bidirectional communication

**Implementation Model**:
```bash
# Each provider is a standalone binary:
echo 'CONFIG_JSON' | ./input-provider | ./output-provider

# CLI orchestrates the pipeline:
dstream run task-name
# → CLI launches input-provider and output-provider
# → CLI pipes: input.stdout → output.stdin
# → CLI handles process lifecycle and graceful shutdown
```

**This architectural decision enables DStream to fulfill its vision as a "Unix pipeline for data" with maximum simplicity, performance, and universal language support.**

## Current Architecture Status & Evolution Plan

### Background

DStream started with SQL Server CDC embedded in the Go CLI, then evolved to support Go plugins (like [dstream-ingester-mssql](https://github.com/katasec/dstream-ingester-mssql)). The .NET plugin support was added to enable .NET developer teams to contribute to the ecosystem.

### Current State (Working)

**✅ Go CLI ↔ .NET Plugin Communication**
- gRPC communication via HashiCorp go-plugin protocol works
- Configuration passing from HCL → Go CLI → .NET plugin works
- Basic .NET counter plugin runs successfully

**❌ .NET Output Provider Routing (Broken)**
- Current `PluginServiceImpl.cs` only handles input providers
- Output configuration is received but ignored
- `ctx.Emit()` just logs instead of routing to output providers

### Architecture Evolution Plan

**Phase 1: Fix .NET Plugin Architecture (Current)**
```
Go CLI → .NET Plugin Process
             ↓
         (Input + Output routing)
```

**Immediate Goals:**
1. Fix .NET `PluginServiceImpl` to parse output provider config
2. Implement provider registry/factory pattern
3. Route data: Input Provider → Output Provider (within same process)
4. Get console output working with counter input

**Phase 2: Separate .NET Provider Binaries (Future)**
```
Go CLI → Input Provider Binary (.NET)
       ↓
       → Output Provider Binary (.NET)
```

**Future Goals:**
1. Evolve Go CLI to orchestrate separate input/output processes
2. Create provider templates and OCI distribution
3. Build ecosystem of independent provider binaries

### Current Implementation Priority

**Step 1: Fix Output Provider Routing**
- Parse `StartRequest.Output` configuration
- Instantiate appropriate output provider (ConsoleOutputProvider)
- Route `ctx.Emit()` to output provider instead of logging

**Step 2: Validate Input Provider Pattern**
- Ensure input providers work correctly in new architecture
- Test with counter and future SQL Server CDC provider

**Step 3: Build Provider Ecosystem**
- Create MSSQL CDC input provider
- Create Azure Service Bus output provider
- Document provider development patterns

### Development Practices

**Critical: Incremental Changes with Validation**

Every change must be validated to ensure we don't break the working communication:

1. **Make localized changes** - small, focused modifications
2. **Compile and test** after each change:
   ```bash
   # Build the plugin
   cd samples/dstream-dotnet-test
   pwsh -c "./build.ps1 clean && ./build.ps1 publish"
   
   # Test end-to-end communication
   cd ~/progs/dstream
   go run . run dotnet-counter
   
   # Verify: Should see counter data flowing from .NET → Go CLI
   ```
3. **Validate data flow** - ensure counter data still flows correctly
4. **Only proceed** if the basic communication still works

**Current Working Baseline:**
- ✅ Go CLI launches .NET plugin via gRPC
- ✅ .NET counter generates data every 500ms  
- ✅ Data flows back to Go CLI and is logged
- ✅ Graceful shutdown on Ctrl+C

This baseline must never be broken during development.

### Integration with DStream CLI

The DStream CLI is a Go application located at `C:\Users\ameer.deen\progs\dstream` that serves as the host for .NET plugins. The integration works as follows:

### Existing Go-based Providers

**MSSQL CDC Provider**: There's already a working Go-based MSSQL CDC provider at `~/progs/dstream-ingester-mssql` ([GitHub](https://github.com/katasec/dstream-ingester-mssql)) that:
- Uses HashiCorp go-plugin protocol over gRPC
- Handles SQL Server Change Data Capture
- Works with the current DStream CLI
- Will continue to work alongside .NET providers (language-agnostic ecosystem)

This means the initial .NET provider focus should be on:
- **Testing providers** (counter input, console output)
- **Complementary providers** (Azure Service Bus output)
- **Validating cross-language compatibility** (Go input → .NET output)

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
