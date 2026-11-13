namespace dr_photodrop.tests;

/// <summary>Orologio fisso per test deterministici.</summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
