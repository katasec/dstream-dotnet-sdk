namespace DStreamDotNetTest;

/// <summary>
/// Mirrors HCL:
/// task { type="plugin" ... config { interval = 5000 } }
/// Only the plugin's own config block is represented here.
/// </summary>
public sealed record GenericCounterConfig
{
    // Name matches HCL key exactly: "interval"
    // Units: milliseconds
    public int Interval { get; init; } = 5000;
}
