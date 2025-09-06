namespace Katasec.DStream.Provider.ConsoleOut;

public sealed class ConsoleOutputConfig
{
    /// <summary>"json" or "text"</summary>
    public string Format { get; set; } = "json";

    /// <summary>Include metadata when Format="text"</summary>
    public bool IncludeMeta { get; set; } = true;
}
