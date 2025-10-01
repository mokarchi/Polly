#nullable enable
using Polly.Bulkhead;

namespace Polly;

public partial class Policy
{
    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    public static AdaptiveBulkheadPolicy<TResult> AdaptiveBulkhead<TResult>(AdaptiveBulkheadOptions options)
        => AdaptiveBulkhead<TResult>(options, EmptyAction);

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <param name="onBulkheadRejected">An action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AdaptiveBulkheadPolicy<TResult> AdaptiveBulkhead<TResult>(AdaptiveBulkheadOptions options, Action<Context> onBulkheadRejected)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AdaptiveBulkheadPolicy<TResult>(options, onBulkheadRejected);
    }

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    public static AdaptiveBulkheadPolicy<TResult> AdaptiveBulkhead<TResult>(Action<AdaptiveBulkheadOptions> configureOptions)
        => AdaptiveBulkhead<TResult>(configureOptions, EmptyAction);

    /// <summary>
    /// <para>Builds an adaptive bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <param name="onBulkheadRejected">An action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AdaptiveBulkheadPolicy<TResult> AdaptiveBulkhead<TResult>(Action<AdaptiveBulkheadOptions> configureOptions, Action<Context> onBulkheadRejected)
    {
        if (configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        var options = new AdaptiveBulkheadOptions();
        configureOptions(options);

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AdaptiveBulkheadPolicy<TResult>(options, onBulkheadRejected);
    }
}