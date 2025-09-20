namespace CounterInputProvider;

/// <summary>
/// Configuration for the counter input provider.
/// </summary>
public sealed record CounterConfig
{
    /// <summary>
    /// Interval between counter increments in milliseconds.
    /// </summary>
    public int Interval { get; init; } = 1000;

    /// <summary>
    /// Maximum number of items to generate (0 = infinite).
    /// </summary>
    public int MaxCount { get; init; } = 0;
}