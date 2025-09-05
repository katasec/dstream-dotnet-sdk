// GenericCounterConfig.cs
// Sample plugin implementation that uses the generic interface with strongly-typed configuration

namespace DStreamDotnetTest;

/// <summary>
/// Strongly typed configuration for the GenericCounterPlugin
/// </summary>
public class GenericCounterConfig
{
    /// <summary>
    /// Interval between counter increments in milliseconds
    /// </summary>
    public int Interval { get; set; } = 5000;
    
    // We're keeping only the interval property to match the existing dotnet-counter task
    // Other properties can be added later as the configuration evolves
}
