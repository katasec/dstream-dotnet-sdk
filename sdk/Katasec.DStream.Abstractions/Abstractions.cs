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

// ---------- Minimal, logging-neutral contract ----------
public interface IDStreamLogger
{
    void Trace(string message, IReadOnlyDictionary<string, object?>? fields = null);
    void Debug(string message, IReadOnlyDictionary<string, object?>? fields = null);
    void Info(string message, IReadOnlyDictionary<string, object?>? fields = null);
    void Warn(string message, IReadOnlyDictionary<string, object?>? fields = null);
    void Error(string message, Exception? ex = null, IReadOnlyDictionary<string, object?>? fields = null);

    // Create a child logger with additional default fields (like hclog/zerolog scopes)
    IDStreamLogger WithFields(IReadOnlyDictionary<string, object?> fields);
}

// ---------- Runtime context & event model ----------
public interface IPluginContext
{
    // Expose raw logger object (currently HCLogger from HCLog.Net)
    object Logger { get; }
    IServiceProvider Services { get; }
    Emit Emit { get; }
}

public readonly record struct Envelope(object Payload, IReadOnlyDictionary<string, object?> Meta);

public delegate Task Emit(Envelope evt, CancellationToken ct);
