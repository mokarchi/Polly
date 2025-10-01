using System.Threading.RateLimiting;
using Polly.RateLimiting;

namespace Polly.RateLimiting.Tests;

public class CompositeRateLimiterTests : IDisposable
{
    private CompositeRateLimiter? _rateLimiter;

    public void Dispose()
    {
        _rateLimiter?.Dispose();
    }

    [Fact]
    public void Constructor_ValidOptions_ShouldCreateInstance()
    {
        // Arrange
        var options = new CompositeRateLimiterOptions
        {
            InitialTokenLimit = 10,
            InitialPermitLimit = 5
        };

        // Act
        _rateLimiter = new CompositeRateLimiter(options);

        // Assert
        Assert.NotNull(_rateLimiter);
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CompositeRateLimiter(null!));
    }

    [Fact]
    public async Task AcquireAsync_WithinLimits_ShouldSucceed()
    {
        // Arrange
        var options = new CompositeRateLimiterOptions
        {
            InitialTokenLimit = 10,
            InitialPermitLimit = 5,
            TokenReplenishmentPeriod = TimeSpan.FromSeconds(1),
            LeapSecondWindow = TimeSpan.FromSeconds(1)
        };
        _rateLimiter = new CompositeRateLimiter(options);

        // Act
        var lease = await _rateLimiter.AcquireAsync(1);

        // Assert
        Assert.NotNull(lease);
        Assert.True(lease.IsAcquired);
        lease.Dispose();
    }

    [Fact]
    public void AttemptAcquire_WithinLimits_ShouldSucceed()
    {
        // Arrange
        var options = new CompositeRateLimiterOptions
        {
            InitialTokenLimit = 10,
            InitialPermitLimit = 5
        };
        _rateLimiter = new CompositeRateLimiter(options);

        // Act
        var lease = _rateLimiter.AttemptAcquire(1);

        // Assert
        Assert.NotNull(lease);
        Assert.True(lease.IsAcquired);
        lease.Dispose();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var options = new CompositeRateLimiterOptions
        {
            InitialTokenLimit = 10,
            InitialPermitLimit = 5
        };
        _rateLimiter = new CompositeRateLimiter(options);

        // Act & Assert - Should not throw
        _rateLimiter.Dispose();
        _rateLimiter.Dispose(); // Double dispose should be safe
        
        // Assert successful disposal
        Assert.True(true); // Disposal completed without exceptions
    }
}

