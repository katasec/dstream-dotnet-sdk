namespace ConsoleOutputProvider;

// Configuration class for the console output provider
public class ConsoleConfig
{
    /// <summary>
    /// Output format: "simple" (default) or "json" or "structured"
    /// </summary>
    public string OutputFormat { get; set; } = "simple";
    
    /// <summary>
    /// Demo infrastructure resource count for lifecycle testing
    /// </summary>
    public int ResourceCount { get; set; } = 3;
}