# Counter Input Provider

A sample **DStream input provider** that generates sequential counter data with timestamps. Perfect for testing output providers, validating data pipelines, and demonstrating the DStream .NET SDK.

## Overview

This provider generates:
- **Sequential numbers** (1, 2, 3, ...) with timestamps
- **Configurable intervals** between increments
- **Optional max count** for finite sequences
- **Rich metadata** (sequence numbers, provider info)
- **JSON-structured envelopes** for downstream consumption

## Configuration

Accepts JSON configuration via stdin:

```json
{
  "interval": 1000,
  "maxCount": 10
}
```

**Options:**
- `interval` - Milliseconds between counter increments (default: 1000)
- `maxCount` - Maximum items to generate, 0 = infinite (default: 0)

## Usage

### Standalone Testing

```bash
# Generate 3 counter items with 1 second intervals
echo '{"interval": 1000, "maxCount": 3}' | dotnet run
```

### Example Output

```json
{"source":"","type":"","data":{"value":1,"timestamp":"2025-09-20T14:17:11.800128+00:00"},"metadata":{"seq":1,"source":"counter-input-provider","interval_ms":1000}}
{"source":"","type":"","data":{"value":2,"timestamp":"2025-09-20T14:17:12.840258+00:00"},"metadata":{"seq":2,"source":"counter-input-provider","interval_ms":1000}}
{"source":"","type":"","data":{"value":3,"timestamp":"2025-09-20T14:17:13.841851+00:00"},"metadata":{"seq":3,"source":"counter-input-provider","interval_ms":1000}}
```

## Building

```bash
# Build debug version
dotnet build

# Build release version  
dotnet build -c Release

# Publish self-contained binary
dotnet publish -c Release -r osx-arm64 --self-contained
```

## DStream Integration

### Task Configuration (HCL)

```hcl
task "counter-demo" {
  input {
    provider_path = "./counter-input-provider"
    config = {
      interval = 1000
      maxCount = 10
    }
  }
  
  output {
    provider_path = "./some-output-provider"
    config = {
      destination = "demo-output"
    }
  }
}
```

## Architecture

- **Type**: Input Provider (generates data)
- **Protocol**: stdin/stdout JSON communication
- **Framework**: .NET 9.0 with DStream .NET SDK
- **Runtime**: Self-contained executable (~68MB)

This demonstrates how simple it is to create a DStream input provider - the complete implementation is ~50 lines of code thanks to the SDK infrastructure!
