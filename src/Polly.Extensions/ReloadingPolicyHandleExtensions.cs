using Microsoft.Extensions.Options;
using Polly.Retry;
using Polly.Timeout;
using Polly.Utils;

namespace Polly.Extensions;

/// <summary>
/// Extensions for integrating ReloadingPolicyHandle with IOptionsSnapshot and IOptionsMonitor.
/// </summary>
public static class ReloadingPolicyHandleExtensions
{
    /// <summary>
    /// Creates a retry reloading policy handle that automatically updates when options change.
    /// </summary>
    /// <typeparam name="TResult">The type of result the retry handles.</typeparam>
    /// <param name="optionsMonitor">The options monitor to watch for changes.</param>
    /// <param name="name">The named options instance to monitor.</param>
    /// <returns>A new retry reloading policy handle that updates when options change.</returns>
    public static RetryReloadingPolicyHandle<TResult> CreateReloadingHandle<TResult>(
        this IOptionsMonitor<RetryStrategyOptions<TResult>> optionsMonitor,
        string? name = null)
    {
        Guard.NotNull(optionsMonitor);

        var initialOptions = optionsMonitor.Get(name ?? Options.DefaultName);
        var handle = new RetryReloadingPolicyHandle<TResult>(initialOptions);

        // Subscribe to option changes
        var registration = optionsMonitor.OnChange((options, optionName) =>
        {
            if (string.Equals(name ?? Options.DefaultName, optionName, StringComparison.Ordinal))
            {
                handle.OnConfigurationChanged(options);
            }
        });

        // Store the registration in a weak way to avoid memory leaks
        // In a real implementation, you might want to return an IDisposable wrapper
        return handle;
    }

    /// <summary>
    /// Creates a retry reloading policy handle that automatically updates when options change.
    /// </summary>
    /// <param name="optionsMonitor">The options monitor to watch for changes.</param>
    /// <param name="name">The named options instance to monitor.</param>
    /// <returns>A new retry reloading policy handle that updates when options change.</returns>
    public static RetryReloadingPolicyHandle CreateReloadingHandle(
        this IOptionsMonitor<RetryStrategyOptions> optionsMonitor,
        string? name = null)
    {
        Guard.NotNull(optionsMonitor);

        var initialOptions = optionsMonitor.Get(name ?? Options.DefaultName);
        var handle = new RetryReloadingPolicyHandle(initialOptions);

        // Subscribe to option changes
        var registration = optionsMonitor.OnChange((options, optionName) =>
        {
            if (string.Equals(name ?? Options.DefaultName, optionName, StringComparison.Ordinal))
            {
                handle.OnConfigurationChanged(options);
            }
        });

        return handle;
    }

    /// <summary>
    /// Creates a timeout reloading policy handle that automatically updates when options change.
    /// </summary>
    /// <param name="optionsMonitor">The options monitor to watch for changes.</param>
    /// <param name="name">The named options instance to monitor.</param>
    /// <returns>A new timeout reloading policy handle that updates when options change.</returns>
    public static TimeoutReloadingPolicyHandle CreateReloadingHandle(
        this IOptionsMonitor<TimeoutStrategyOptions> optionsMonitor,
        string? name = null)
    {
        Guard.NotNull(optionsMonitor);

        var initialOptions = optionsMonitor.Get(name ?? Options.DefaultName);
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);

        // Subscribe to option changes
        var registration = optionsMonitor.OnChange((options, optionName) =>
        {
            if (string.Equals(name ?? Options.DefaultName, optionName, StringComparison.Ordinal))
            {
                handle.OnConfigurationChanged(options);
            }
        });

        return handle;
    }

    /// <summary>
    /// Creates a disposable wrapper for a reloading policy handle that manages the options monitor subscription.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to monitor.</typeparam>
    /// <typeparam name="THandle">The type of reloading policy handle.</typeparam>
    /// <param name="optionsMonitor">The options monitor to watch for changes.</param>
    /// <param name="handleFactory">Factory function to create the handle from initial options.</param>
    /// <param name="name">The named options instance to monitor.</param>
    /// <returns>A disposable wrapper that manages the subscription lifecycle.</returns>
    public static DisposableReloadingHandle<THandle> CreateDisposableReloadingHandle<TOptions, THandle>(
        this IOptionsMonitor<TOptions> optionsMonitor,
        Func<TOptions, THandle> handleFactory,
        string? name = null)
        where THandle : class, IReloadingPolicyHandle
    {
        Guard.NotNull(optionsMonitor);
        Guard.NotNull(handleFactory);

        var initialOptions = optionsMonitor.Get(name ?? Options.DefaultName);
        var handle = handleFactory(initialOptions);

        // Subscribe to option changes
        var registration = optionsMonitor.OnChange((options, optionName) =>
        {
            if (string.Equals(name ?? Options.DefaultName, optionName, StringComparison.Ordinal))
            {
                handle.OnConfigurationChanged(options);
            }
        });

        return new DisposableReloadingHandle<THandle>(handle, registration);
    }
}

/// <summary>
/// A disposable wrapper for reloading policy handles that manages the lifecycle of options monitor subscriptions.
/// </summary>
/// <typeparam name="THandle">The type of reloading policy handle.</typeparam>
public sealed class DisposableReloadingHandle<THandle> : IDisposable
    where THandle : class, IReloadingPolicyHandle
{
    private readonly THandle _handle;
    private readonly IDisposable _registration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableReloadingHandle{THandle}"/> class.
    /// </summary>
    /// <param name="handle">The reloading policy handle.</param>
    /// <param name="registration">The options monitor registration to dispose.</param>
    internal DisposableReloadingHandle(THandle handle, IDisposable registration)
    {
        _handle = handle;
        _registration = registration;
    }

    /// <summary>
    /// Gets the underlying reloading policy handle.
    /// </summary>
    public THandle Handle => _handle;

    /// <summary>
    /// Disposes the options monitor subscription.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _registration.Dispose();
            _disposed = true;
        }
    }
}