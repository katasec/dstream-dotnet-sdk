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

// ---------- Runtime context & event model ----------
public interface IPluginContext
{
    // Simple logger for stdin/stdout mode
    object Logger { get; }
}

public readonly record struct Envelope(object Payload, IReadOnlyDictionary<string, object?> Meta);
