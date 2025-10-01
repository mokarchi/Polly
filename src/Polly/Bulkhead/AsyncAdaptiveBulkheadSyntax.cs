#nullable enable
using Polly.Bulkhead;

namespace Polly;

public partial class Policy
{
    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    public static AsyncAdaptiveBulkheadPolicy AdaptiveBulkheadAsync(AdaptiveBulkheadOptions options)
        => AdaptiveBulkheadAsync(options, EmptyActionAsync);

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <param name="onBulkheadRejected">An async action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AsyncAdaptiveBulkheadPolicy AdaptiveBulkheadAsync(AdaptiveBulkheadOptions options, Func<Context, Task> onBulkheadRejected)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AsyncAdaptiveBulkheadPolicy(options, onBulkheadRejected);
    }

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    public static AsyncAdaptiveBulkheadPolicy AdaptiveBulkheadAsync(Action<AdaptiveBulkheadOptions> configureOptions)
        => AdaptiveBulkheadAsync(configureOptions, EmptyActionAsync);

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <param name="onBulkheadRejected">An async action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AsyncAdaptiveBulkheadPolicy AdaptiveBulkheadAsync(Action<AdaptiveBulkheadOptions> configureOptions, Func<Context, Task> onBulkheadRejected)
    {
        if (configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        var options = new AdaptiveBulkheadOptions();
        configureOptions(options);

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AsyncAdaptiveBulkheadPolicy(options, onBulkheadRejected);
    }

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    public static AsyncAdaptiveBulkheadPolicy<TResult> AdaptiveBulkheadAsync<TResult>(AdaptiveBulkheadOptions options)
        => AdaptiveBulkheadAsync<TResult>(options, EmptyActionAsync);

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="options">Configuration options for the adaptive bulkhead behavior.</param>
    /// <param name="onBulkheadRejected">An async action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">options</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AsyncAdaptiveBulkheadPolicy<TResult> AdaptiveBulkheadAsync<TResult>(AdaptiveBulkheadOptions options, Func<Context, Task> onBulkheadRejected)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AsyncAdaptiveBulkheadPolicy<TResult>(options, onBulkheadRejected);
    }

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    public static AsyncAdaptiveBulkheadPolicy<TResult> AdaptiveBulkheadAsync<TResult>(Action<AdaptiveBulkheadOptions> configureOptions)
        => AdaptiveBulkheadAsync<TResult>(configureOptions, EmptyActionAsync);

    /// <summary>
    /// <para>Builds an adaptive async bulkhead isolation <see cref="Policy{TResult}"/>, which automatically adjusts the maximum concurrency of actions executed through the policy based on latency and error rate using AIMD algorithm.</para>
    /// <para>This provides better performance under varying load conditions compared to static bulkhead policies.</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="configureOptions">Action to configure the adaptive bulkhead options.</param>
    /// <param name="onBulkheadRejected">An async action to call, if the bulkhead rejects execution due to oversubscription.</param>
    /// <returns>The policy instance.</returns>
    /// <exception cref="ArgumentNullException">configureOptions</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onBulkheadRejected"/> is <see langword="null"/>.</exception>
    public static AsyncAdaptiveBulkheadPolicy<TResult> AdaptiveBulkheadAsync<TResult>(Action<AdaptiveBulkheadOptions> configureOptions, Func<Context, Task> onBulkheadRejected)
    {
        if (configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));
        if (onBulkheadRejected is null) throw new ArgumentNullException(nameof(onBulkheadRejected));

        var options = new AdaptiveBulkheadOptions();
        configureOptions(options);

        ValidateAdaptiveBulkheadOptions(options);
        
        return new AsyncAdaptiveBulkheadPolicy<TResult>(options, onBulkheadRejected);
    }

    private static readonly Func<Context, Task> EmptyActionAsync = _ => Task.CompletedTask;
}