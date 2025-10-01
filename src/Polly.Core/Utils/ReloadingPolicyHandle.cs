using System.Threading;

namespace Polly.Utils;

/// <summary>
/// Provides a mechanism for atomically updating internal strategy state when configuration changes,
/// without rebuilding the entire resilience pipeline. Uses interlocked operations for thread-safe updates.
/// </summary>
/// <typeparam name="TState">The type of the state class to be managed.</typeparam>
public abstract class ReloadingPolicyHandle<TState> where TState : class
{
    private TState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReloadingPolicyHandle{TState}"/> class.
    /// </summary>
    /// <param name="initialState">The initial state value.</param>
    protected ReloadingPolicyHandle(TState initialState)
    {
        _state = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    /// <summary>
    /// Gets the current state atomically.
    /// </summary>
    /// <returns>The current state value.</returns>
    public TState GetCurrentState()
    {
        return _state;
    }

    /// <summary>
    /// Atomically updates the state with a new value.
    /// </summary>
    /// <param name="newState">The new state value.</param>
    /// <returns>The previous state value.</returns>
    public TState UpdateState(TState newState)
    {
        Guard.NotNull(newState);
        return Interlocked.Exchange(ref _state, newState);
    }

    /// <summary>
    /// Atomically compares and updates the state if the current value matches the expected value.
    /// </summary>
    /// <param name="newState">The new state value.</param>
    /// <param name="expectedState">The expected current state value.</param>
    /// <returns>The original state value before any change.</returns>
    public TState CompareAndUpdateState(TState newState, TState expectedState)
    {
        Guard.NotNull(newState);
        return Interlocked.CompareExchange(ref _state, newState, expectedState);
    }

    /// <summary>
    /// Called when the configuration changes. Derived classes should implement this to update their state.
    /// </summary>
    /// <param name="newOptions">The new configuration options.</param>
    public abstract void OnConfigurationChanged(object newOptions);
}

