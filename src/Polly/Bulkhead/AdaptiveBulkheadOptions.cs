#nullable enable

namespace Polly.Bulkhead;

/// <summary>
/// Configuration options for adaptive bulkhead policy that automatically adjusts maxParallelization 
/// based on latency and error rate using AIMD (Additive Increase/Multiplicative Decrease) algorithm.
/// </summary>
public class AdaptiveBulkheadOptions
{
    /// <summary>
    /// Gets or sets the initial maximum number of concurrent actions that may be executing through the policy.
    /// </summary>
    public int InitialMaxParallelization { get; set; } = 10;

    /// <summary>
    /// Gets or sets the minimum allowed maximum parallelization value.
    /// </summary>
    public int MinMaxParallelization { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum allowed maximum parallelization value.
    /// </summary>
    public int MaxMaxParallelization { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of actions that can be queued waiting for execution.
    /// </summary>
    public int MaxQueueingActions { get; set; } = 0;

    /// <summary>
    /// Gets or sets the target latency threshold. When average latency exceeds this, parallelization is decreased.
    /// </summary>
    public TimeSpan LatencyThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the error rate threshold (0.0 to 1.0). When error rate exceeds this, parallelization is decreased.
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the additive increase amount when conditions are good.
    /// </summary>
    public int AdditiveIncrease { get; set; } = 1;

    /// <summary>
    /// Gets or sets the multiplicative decrease factor when conditions are poor (0.0 to 1.0).
    /// </summary>
    public double MultiplicativeDecrease { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the window size for calculating moving averages of latency and error rate.
    /// </summary>
    public int SamplingWindowSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval for evaluating and adjusting the parallelization level.
    /// </summary>
    public TimeSpan AdjustmentInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the minimum number of samples required before making adjustments.
    /// </summary>
    public int MinSamplesForAdjustment { get; set; } = 10;
}