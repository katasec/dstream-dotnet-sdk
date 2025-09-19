namespace Katasec.DStream.Abstractions;

// ---------- Provider shape ----------
public interface IProvider { }

public interface IInputProvider : IProvider
{
    IAsyncEnumerable<Envelope> ReadAsync(IPluginContext ctx, CancellationToken ct);
}

public interface IOutputProvider : IProvider
{
    Task WriteAsync(IEnumerable<Envelope> batch, IPluginContext ctx, CancellationToken ct);
}

// ---------- Infrastructure lifecycle management ----------
public interface IInfrastructureProvider : IProvider
{
    Task<InfrastructureResult> InitializeAsync(CancellationToken ct);
    Task<InfrastructureResult> DestroyAsync(CancellationToken ct);
    Task<InfrastructureResult> GetStatusAsync(CancellationToken ct);
    Task<InfrastructureResult> PlanAsync(CancellationToken ct);
}

// ---------- Runtime context & event model ----------
public interface IPluginContext
{
    // Simple logger for stdin/stdout mode
    object Logger { get; }
}

public readonly record struct Envelope(object Payload, IReadOnlyDictionary<string, object?> Meta);

// ---------- Infrastructure lifecycle results ----------
public class InfrastructureResult
{
    public string Status { get; set; } = "Unknown";
    public string[]? Resources { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

// ---------- Command envelope for lifecycle operations ----------
public class CommandEnvelope<TConfig> where TConfig : class
{
    public string Command { get; set; } = "run";
    public TConfig? Config { get; set; }
}
