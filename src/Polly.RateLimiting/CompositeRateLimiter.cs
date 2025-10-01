using System.Threading.RateLimiting;

namespace Polly.RateLimiting;

/// <summary>
/// A composite rate limiter that combines Token Bucket and Leap Second Window algorithms
/// with adaptive parameter updates based on moving average of success/timeout rates.
/// </summary>
public sealed class CompositeRateLimiter : IDisposable
{
    private const int MinDataPointsForUpdate = 10;
    private const double SignificantChangeThreshold = 0.1;
    private const double DefaultSuccessRate = 0.5;

    private volatile TokenBucketRateLimiter _tokenBucket;
    private volatile SlidingWindowRateLimiter _slidingWindow;
    private readonly AdaptiveMetrics _metrics;
    private readonly CompositeRateLimiterOptions _options;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeRateLimiter"/> class.
    /// </summary>
    /// <param name="options">The composite rate limiter options.</param>
    public CompositeRateLimiter(CompositeRateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _metrics = new AdaptiveMetrics(options.MetricsWindow, options.TimeProvider);

        // Initialize Token Bucket
        _tokenBucket = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = options.InitialTokenLimit,
            ReplenishmentPeriod = options.TokenReplenishmentPeriod,
            TokensPerPeriod = options.InitialTokensPerPeriod,
            AutoReplenishment = options.AutoReplenishment,
            QueueLimit = options.QueueLimit
        });

        // Initialize Sliding Window (Leap Second Window)
        _slidingWindow = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.InitialPermitLimit,
            Window = options.LeapSecondWindow,
            SegmentsPerWindow = options.SegmentsPerWindow
        });
    }

    /// <summary>
    /// Acquires a rate limit lease asynchronously.
    /// </summary>
    /// <param name="permitCount">Number of permits to acquire.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A rate limit lease.</returns>
    public ValueTask<RateLimitLease> AcquireAsync(int permitCount = 1, CancellationToken cancellationToken = default)
    {
        return PerformAcquisition(permitCount, cancellationToken);
    }

    /// <summary>
    /// Attempts to acquire a rate limit lease immediately.
    /// </summary>
    /// <param name="permitCount">Number of permits to acquire.</param>
    /// <returns>A rate limit lease.</returns>
    public RateLimitLease AttemptAcquire(int permitCount = 1)
    {
        // For synchronous acquisition, use AttemptAcquire on both limiters
        var tokenBucketLease = _tokenBucket.AttemptAcquire(permitCount);
        if (!tokenBucketLease.IsAcquired)
        {
            _metrics.RecordFailure();
            UpdateParameters();
            return tokenBucketLease;
        }

        var slidingWindowLease = _slidingWindow.AttemptAcquire(permitCount);
        if (!slidingWindowLease.IsAcquired)
        {
            tokenBucketLease.Dispose(); // Release the token bucket lease
            _metrics.RecordFailure();
            UpdateParameters();
            return slidingWindowLease;
        }

        // Both succeeded
        _metrics.RecordSuccess();
        UpdateParameters();
        return new CompositeRateLimitLease(tokenBucketLease, slidingWindowLease);
    }

    private async ValueTask<RateLimitLease> PerformAcquisition(int permitCount, CancellationToken cancellationToken)
    {
        // Try Token Bucket first (burst capacity)
        var tokenBucketLease = await _tokenBucket.AcquireAsync(permitCount, cancellationToken).ConfigureAwait(false);
        if (!tokenBucketLease.IsAcquired)
        {
            _metrics.RecordFailure();
            UpdateParameters();
            return tokenBucketLease;
        }

        // Try Sliding Window (rate limiting over time)
        var slidingWindowLease = await _slidingWindow.AcquireAsync(permitCount, cancellationToken).ConfigureAwait(false);
        if (!slidingWindowLease.IsAcquired)
        {
            tokenBucketLease.Dispose(); // Release the token bucket lease
            _metrics.RecordFailure();
            UpdateParameters();
            return slidingWindowLease;
        }

        // Both succeeded
        _metrics.RecordSuccess();
        UpdateParameters();
        return new CompositeRateLimitLease(tokenBucketLease, slidingWindowLease);
    }

    private void UpdateParameters()
    {
        lock (_lock)
        {
            var metrics = _metrics.GetMetrics();
            var successRate = metrics.TotalAttempts > 0 ? (double)metrics.SuccessCount / metrics.TotalAttempts : 0.5;
            var movingAverage = _metrics.GetMovingAverage();

            // Skip updates if insufficient data
            if (metrics.TotalAttempts < MinDataPointsForUpdate)
            {
                return;
            }

            // Update Token Bucket parameters based on success rate
            if (movingAverage.SuccessRate > _options.HighSuccessThreshold && successRate > _options.HighSuccessThreshold)
            {
                // High success rate - increase capacity
                var currentTokenLimit = _options.InitialTokenLimit;
                var newTokenLimit = Math.Min(
                    (int)(currentTokenLimit * _options.IncreaseMultiplier),
                    _options.MaxTokenLimit);
                
                var newTokensPerPeriod = Math.Min(
                    (int)(_options.InitialTokensPerPeriod * _options.IncreaseMultiplier),
                    _options.MaxTokensPerPeriod);

                // Recreate token bucket with new parameters if significantly different
                if (Math.Abs(newTokenLimit - currentTokenLimit) > currentTokenLimit * SignificantChangeThreshold)
                {
                    var oldTokenBucket = _tokenBucket;
                    _tokenBucket = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = newTokenLimit,
                        ReplenishmentPeriod = _options.TokenReplenishmentPeriod,
                        TokensPerPeriod = newTokensPerPeriod,
                        AutoReplenishment = _options.AutoReplenishment,
                        QueueLimit = _options.QueueLimit
                    });
                    oldTokenBucket.Dispose();
                }

                // Update sliding window
                var currentPermitLimit = _options.InitialPermitLimit;
                var newPermitLimit = Math.Min(
                    (int)(currentPermitLimit * _options.IncreaseMultiplier),
                    _options.MaxPermitLimit);

                if (Math.Abs(newPermitLimit - currentPermitLimit) > currentPermitLimit * SignificantChangeThreshold)
                {
                    var oldSlidingWindow = _slidingWindow;
                    _slidingWindow = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = newPermitLimit,
                        Window = _options.LeapSecondWindow,
                        SegmentsPerWindow = _options.SegmentsPerWindow
                    });
                    oldSlidingWindow.Dispose();
                }
            }
            else if (movingAverage.SuccessRate < _options.LowSuccessThreshold && successRate < _options.LowSuccessThreshold)
            {
                // Low success rate - decrease capacity
                var currentTokenLimit = _options.InitialTokenLimit;
                var newTokenLimit = Math.Max(
                    (int)(currentTokenLimit * _options.DecreaseMultiplier),
                    _options.MinTokenLimit);
                
                var currentPermitLimit = _options.InitialPermitLimit;
                var newPermitLimit = Math.Max(
                    (int)(currentPermitLimit * _options.DecreaseMultiplier),
                    _options.MinPermitLimit);

                // Update token bucket
                if (Math.Abs(newTokenLimit - currentTokenLimit) > currentTokenLimit * SignificantChangeThreshold)
                {
                    var oldTokenBucket = _tokenBucket;
                    _tokenBucket = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = newTokenLimit,
                        ReplenishmentPeriod = _options.TokenReplenishmentPeriod,
                        TokensPerPeriod = Math.Max(1, (int)(_options.InitialTokensPerPeriod * _options.DecreaseMultiplier)),
                        AutoReplenishment = _options.AutoReplenishment,
                        QueueLimit = _options.QueueLimit
                    });
                    oldTokenBucket.Dispose();
                }

                // Update sliding window
                if (Math.Abs(newPermitLimit - currentPermitLimit) > currentPermitLimit * SignificantChangeThreshold)
                {
                    var oldSlidingWindow = _slidingWindow;
                    _slidingWindow = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = newPermitLimit,
                        Window = _options.LeapSecondWindow,
                        SegmentsPerWindow = _options.SegmentsPerWindow
                    });
                    oldSlidingWindow.Dispose();
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _tokenBucket?.Dispose();
        _slidingWindow?.Dispose();
    }
}