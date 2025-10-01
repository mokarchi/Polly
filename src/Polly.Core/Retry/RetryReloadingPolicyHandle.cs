using Polly.Utils;

namespace Polly.Retry;

/// <summary>
/// State class for retry strategy configuration that can be atomically updated.
/// </summary>
public sealed class RetryState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryState"/> class.
    /// </summary>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
    /// <param name="baseDelay">The base delay between retry attempts.</param>
    /// <param name="maxDelay">The maximum delay between retry attempts.</param>
    /// <param name="backoffType">The backoff type for delay calculation.</param>
    /// <param name="useJitter">Whether to use jitter in delay calculation.</param>
    public RetryState(int maxRetryAttempts, TimeSpan baseDelay, TimeSpan? maxDelay, DelayBackoffType backoffType, bool useJitter)
    {
        MaxRetryAttempts = maxRetryAttempts;
        BaseDelay = baseDelay;
        MaxDelay = maxDelay;
        BackoffType = backoffType;
        UseJitter = useJitter;
    }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; }

    /// <summary>
    /// Gets the base delay between retry attempts.
    /// </summary>
    public TimeSpan BaseDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retry attempts.
    /// </summary>
    public TimeSpan? MaxDelay { get; }

    /// <summary>
    /// Gets the backoff type for delay calculation.
    /// </summary>
    public DelayBackoffType BackoffType { get; }

    /// <summary>
    /// Gets a value indicating whether to use jitter in delay calculation.
    /// </summary>
    public bool UseJitter { get; }
}

/// <summary>
/// A reloading policy handle for retry strategies that allows atomic updates of retry configuration
/// without rebuilding the entire pipeline.
/// </summary>
/// <typeparam name="T">The type of result the retry strategy handles.</typeparam>
public sealed class RetryReloadingPolicyHandle<T> : ReloadingPolicyHandle<RetryState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryReloadingPolicyHandle{T}"/> class.
    /// </summary>
    /// <param name="options">The initial retry strategy options.</param>
    public RetryReloadingPolicyHandle(RetryStrategyOptions<T> options)
        : base(new RetryState(
            Guard.NotNull(options).MaxRetryAttempts,
            options.Delay,
            options.MaxDelay,
            options.BackoffType,
            options.UseJitter))
    {
    }

    /// <summary>
    /// Called when the retry strategy configuration changes.
    /// </summary>
    /// <param name="newOptions">The new retry strategy options.</param>
    public override void OnConfigurationChanged(object newOptions)
    {
        if (newOptions is RetryStrategyOptions<T> retryOptions)
        {
            var newState = new RetryState(
                retryOptions.MaxRetryAttempts,
                retryOptions.Delay,
                retryOptions.MaxDelay,
                retryOptions.BackoffType,
                retryOptions.UseJitter);

            UpdateState(newState);
        }
    }

    /// <summary>
    /// Gets the current maximum retry attempts.
    /// </summary>
    /// <returns>The current maximum retry attempts.</returns>
    public int GetMaxRetryAttempts() => GetCurrentState().MaxRetryAttempts;

    /// <summary>
    /// Gets the current base delay.
    /// </summary>
    /// <returns>The current base delay.</returns>
    public TimeSpan GetBaseDelay() => GetCurrentState().BaseDelay;

    /// <summary>
    /// Gets the current maximum delay.
    /// </summary>
    /// <returns>The current maximum delay.</returns>
    public TimeSpan? GetMaxDelay() => GetCurrentState().MaxDelay;

    /// <summary>
    /// Gets the current backoff type.
    /// </summary>
    /// <returns>The current backoff type.</returns>
    public DelayBackoffType GetBackoffType() => GetCurrentState().BackoffType;

    /// <summary>
    /// Gets the current jitter usage setting.
    /// </summary>
    /// <returns>The current jitter usage setting.</returns>
    public bool GetUseJitter() => GetCurrentState().UseJitter;
}

/// <summary>
/// A non-generic reloading policy handle for retry strategies.
/// </summary>
public sealed class RetryReloadingPolicyHandle : ReloadingPolicyHandle<RetryState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryReloadingPolicyHandle"/> class.
    /// </summary>
    /// <param name="options">The initial retry strategy options.</param>
    public RetryReloadingPolicyHandle(RetryStrategyOptions options)
        : base(new RetryState(
            Guard.NotNull(options).MaxRetryAttempts,
            options.Delay,
            options.MaxDelay,
            options.BackoffType,
            options.UseJitter))
    {
    }

    /// <summary>
    /// Called when the retry strategy configuration changes.
    /// </summary>
    /// <param name="newOptions">The new retry strategy options.</param>
    public override void OnConfigurationChanged(object newOptions)
    {
        if (newOptions is RetryStrategyOptions retryOptions)
        {
            var newState = new RetryState(
                retryOptions.MaxRetryAttempts,
                retryOptions.Delay,
                retryOptions.MaxDelay,
                retryOptions.BackoffType,
                retryOptions.UseJitter);

            UpdateState(newState);
        }
    }

    /// <summary>
    /// Gets the current maximum retry attempts.
    /// </summary>
    /// <returns>The current maximum retry attempts.</returns>
    public int GetMaxRetryAttempts() => GetCurrentState().MaxRetryAttempts;

    /// <summary>
    /// Gets the current base delay.
    /// </summary>
    /// <returns>The current base delay.</returns>
    public TimeSpan GetBaseDelay() => GetCurrentState().BaseDelay;

    /// <summary>
    /// Gets the current maximum delay.
    /// </summary>
    /// <returns>The current maximum delay.</returns>
    public TimeSpan? GetMaxDelay() => GetCurrentState().MaxDelay;

    /// <summary>
    /// Gets the current backoff type.
    /// </summary>
    /// <returns>The current backoff type.</returns>
    public DelayBackoffType GetBackoffType() => GetCurrentState().BackoffType;

    /// <summary>
    /// Gets the current jitter usage setting.
    /// </summary>
    /// <returns>The current jitter usage setting.</returns>
    public bool GetUseJitter() => GetCurrentState().UseJitter;
}