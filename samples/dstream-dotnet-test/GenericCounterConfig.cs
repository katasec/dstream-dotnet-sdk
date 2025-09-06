namespace DStreamDotNetTest;


public sealed record GenericCounterConfig
{
    // Name matches HCL key "interval" from dstream.hcl
    // Units: milliseconds
    public int Interval { get; init; } = 5000;
}
