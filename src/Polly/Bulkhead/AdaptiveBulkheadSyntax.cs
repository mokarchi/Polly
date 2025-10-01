#nullable enable
using Polly.Bulkhead;

namespace Polly;

public partial class Policy
{
    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    public static AdaptiveBulkheadPolicy AdaptiveBulkhead(AdaptiveBulkheadOptions options)
        => AdaptiveBulkhead(options, EmptyAction);

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <param name="onBulkheadRejected">An action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AdaptiveBulkheadPolicy AdaptiveBulkhead(AdaptiveBulkheadOptions options, Action<Context> onBulkheadRejected)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AdaptiveBulkheadPolicy(options, onBulkheadRejected);
    }

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    public static AdaptiveBulkheadPolicy AdaptiveBulkhead(Action<AdaptiveBulkheadOptions> configureOptions)
        => AdaptiveBulkhead(configureOptions, EmptyAction);

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <param name="onBulkheadRejected">An action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AdaptiveBulkheadPolicy AdaptiveBulkhead(Action<AdaptiveBulkheadOptions> configureOptions, Action<Context> onBulkheadRejected)
    {
        if (configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        var options = new AdaptiveBulkheadOptions();
        configureOptions(options);
        
        ValidateAdaptiveBulkheadOptions(options);
        
        return new AdaptiveBulkheadPolicy(options, onBulkheadRejected);
    }

    private static void ValidateAdaptiveBulkheadOptions(AdaptiveBulkheadOptions options)
    {
        if (options.InitialMaxParallelization <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "InitialMaxParallelization must be greater than zero.");
        
        if (options.MinMaxParallelization <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MinMaxParallelization must be greater than zero.");
        
        if (options.MaxMaxParallelization < options.MinMaxParallelization)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxMaxParallelization must be greater than or equal to MinMaxParallelization.");
        
        if (options.InitialMaxParallelization < options.MinMaxParallelization || options.InitialMaxParallelization > options.MaxMaxParallelization)
            throw new ArgumentOutOfRangeException(nameof(options), "InitialMaxParallelization must be between MinMaxParallelization and MaxMaxParallelization.");
        
        if (options.MaxQueueingActions < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxQueueingActions must be greater than or equal to zero.");
        
        if (options.ErrorRateThreshold < 0.0 || options.ErrorRateThreshold > 1.0)
            throw new ArgumentOutOfRangeException(nameof(options), "ErrorRateThreshold must be between 0.0 and 1.0.");
        
        if (options.MultiplicativeDecrease <= 0.0 || options.MultiplicativeDecrease >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(options), "MultiplicativeDecrease must be between 0.0 and 1.0 (exclusive).");
        
        if (options.AdditiveIncrease <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "AdditiveIncrease must be greater than zero.");
        
        if (options.SamplingWindowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "SamplingWindowSize must be greater than zero.");
        
        if (options.MinSamplesForAdjustment <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MinSamplesForAdjustment must be greater than zero.");
        
        if (options.AdjustmentInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "AdjustmentInterval must be greater than zero.");
        
        if (options.LatencyThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "LatencyThreshold must be greater than zero.");
    }
}