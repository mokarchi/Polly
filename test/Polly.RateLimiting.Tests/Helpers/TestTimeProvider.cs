namespace Polly.RateLimiting.Tests.Helpers;

/// <summary>
/// Fake time provider for testing.
/// </summary>
internal class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan timeSpan)
    {
        _now += timeSpan;
    }

    public void SetTime(DateTimeOffset time)
    {
        _now = time;
    }
}