using Polly.Utils;

namespace Polly.Timeout;

/// <summary>
/// State class for timeout strategy configuration that can be atomically updated.
/// </summary>
public sealed class TimeoutState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutState"/> class.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    public TimeoutState(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the timeout value.
    /// </summary>
    public TimeSpan Timeout { get; }
}

/// <summary>
/// A reloading policy handle for timeout strategies that allows atomic updates of timeout configuration
/// without rebuilding the entire pipeline.
/// </summary>
public sealed class TimeoutReloadingPolicyHandle : ReloadingPolicyHandle<TimeoutState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutReloadingPolicyHandle"/> class.
    /// </summary>
    /// <param name="options">The initial timeout strategy options.</param>
    public TimeoutReloadingPolicyHandle(TimeoutStrategyOptions options)
        : base(new TimeoutState(Guard.NotNull(options).Timeout))
    {
    }

    /// <summary>
    /// Called when the timeout strategy configuration changes.
    /// </summary>
    /// <param name="newOptions">The new timeout strategy options.</param>
    public override void OnConfigurationChanged(object newOptions)
    {
        if (newOptions is TimeoutStrategyOptions timeoutOptions)
        {
            var newState = new TimeoutState(timeoutOptions.Timeout);
            UpdateState(newState);
        }
    }

    /// <summary>
    /// Gets the current timeout value.
    /// </summary>
    /// <returns>The current timeout value.</returns>
    public TimeSpan GetTimeout() => GetCurrentState().Timeout;
}