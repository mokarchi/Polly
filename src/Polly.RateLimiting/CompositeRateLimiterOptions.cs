namespace Polly.RateLimiting;

/// <summary>
/// Options for the <see cref="CompositeRateLimiter"/>.
/// </summary>
public sealed class CompositeRateLimiterOptions
{
    /// <summary>
    /// Gets or sets the time provider used for timing operations.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Gets or sets the initial token limit for the token bucket.
    /// </summary>
    public int InitialTokenLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum token limit for adaptive adjustments.
    /// </summary>
    public int MinTokenLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum token limit for adaptive adjustments.
    /// </summary>
    public int MaxTokenLimit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the initial number of tokens per replenishment period.
    /// </summary>
    public int InitialTokensPerPeriod { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum tokens per period for adaptive adjustments.
    /// </summary>
    public int MaxTokensPerPeriod { get; set; } = 100;

    /// <summary>
    /// Gets or sets the token replenishment period.
    /// </summary>
    public TimeSpan TokenReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets whether tokens are automatically replenished.
    /// </summary>
    public bool AutoReplenishment { get; set; } = true;

    /// <summary>
    /// Gets or sets the queue limit for the token bucket.
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Gets or sets the initial permit limit for the sliding window (leap second window).
    /// </summary>
    public int InitialPermitLimit { get; set; } = 50;

    /// <summary>
    /// Gets or sets the minimum permit limit for adaptive adjustments.
    /// </summary>
    public int MinPermitLimit { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum permit limit for adaptive adjustments.
    /// </summary>
    public int MaxPermitLimit { get; set; } = 500;

    /// <summary>
    /// Gets or sets the leap second window duration.
    /// </summary>
    public TimeSpan LeapSecondWindow { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the number of segments per window for the sliding window.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 10;

    /// <summary>
    /// Gets or sets the metrics collection window for calculating moving averages.
    /// </summary>
    public TimeSpan MetricsWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the success rate threshold above which capacity is increased.
    /// </summary>
    public double HighSuccessThreshold { get; set; } = 0.9;

    /// <summary>
    /// Gets or sets the success rate threshold below which capacity is decreased.
    /// </summary>
    public double LowSuccessThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the multiplier for increasing capacity during high success periods.
    /// </summary>
    public double IncreaseMultiplier { get; set; } = 1.2;

    /// <summary>
    /// Gets or sets the multiplier for decreasing capacity during low success periods.
    /// </summary>
    public double DecreaseMultiplier { get; set; } = 0.8;
}