using Microsoft.Extensions.Options;
using Polly.Extensions;
using Polly.Retry;
using Polly.Timeout;
using Xunit;

namespace Polly.Extensions.Tests;

public class ReloadingPolicyHandleExtensionsTests
{
    [Fact]
    public void CreateReloadingHandle_ForRetry_ShouldInitializeCorrectly()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<RetryStrategyOptions<string>>();
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        });

        // Act
        var handle = optionsMonitor.CreateReloadingHandle<string>();

        // Assert
        Assert.Equal(3, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(1), handle.GetBaseDelay());
    }

    [Fact]
    public void CreateReloadingHandle_ForRetryNonGeneric_ShouldInitializeCorrectly()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<RetryStrategyOptions>();
        optionsMonitor.SetOptions(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        });

        // Act
        var handle = optionsMonitor.CreateReloadingHandle();

        // Assert
        Assert.Equal(5, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), handle.GetBaseDelay());
    }

    [Fact]
    public void CreateReloadingHandle_ForTimeout_ShouldInitializeCorrectly()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<TimeoutStrategyOptions>();
        optionsMonitor.SetOptions(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var handle = optionsMonitor.CreateReloadingHandle();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), handle.GetTimeout());
    }

    [Fact]
    public void OptionsMonitor_WhenChanged_ShouldUpdateHandle()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<RetryStrategyOptions<string>>();
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        });

        var handle = optionsMonitor.CreateReloadingHandle<string>();

        // Act
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        });
        optionsMonitor.NotifyChange();

        // Assert
        Assert.Equal(5, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), handle.GetBaseDelay());
    }

    [Fact]
    public void OptionsMonitor_WithNamedOptions_ShouldOnlyUpdateMatchingName()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<RetryStrategyOptions<string>>();
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        }, "test");

        var handle = optionsMonitor.CreateReloadingHandle<string>("test");

        // Act - Update different named options
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromSeconds(10)
        }, "other");
        optionsMonitor.NotifyChange("other");

        // Assert - Should not change
        Assert.Equal(3, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(1), handle.GetBaseDelay());

        // Act - Update matching named options
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        }, "test");
        optionsMonitor.NotifyChange("test");

        // Assert - Should change
        Assert.Equal(5, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), handle.GetBaseDelay());
    }

    [Fact]
    public void CreateDisposableReloadingHandle_ShouldManageSubscription()
    {
        // Arrange
        var optionsMonitor = new TestOptionsMonitor<RetryStrategyOptions<string>>();
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        });

        // Act
        using var disposableHandle = optionsMonitor.CreateDisposableReloadingHandle(
            options => new RetryReloadingPolicyHandle<string>(options));

        // Assert
        Assert.Equal(3, disposableHandle.Handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(1), disposableHandle.Handle.GetBaseDelay());

        // Act - Change options
        optionsMonitor.SetOptions(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        });
        optionsMonitor.NotifyChange();

        // Assert - Should update
        Assert.Equal(5, disposableHandle.Handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), disposableHandle.Handle.GetBaseDelay());
    }

    [Fact]
    public void CreateReloadingHandle_WithNullMonitor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ReloadingPolicyHandleExtensions.CreateReloadingHandle<string>(null!));
    }
}

/// <summary>
/// Test implementation of IOptionsMonitor for testing purposes.
/// </summary>
/// <typeparam name="TOptions">The type of options.</typeparam>
internal sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
{
    private readonly Dictionary<string, TOptions> _options = new();
    private readonly List<Action<TOptions, string?>> _changeCallbacks = new();

    public TOptions CurrentValue => Get(Options.DefaultName);

    public TOptions Get(string? name)
    {
        name ??= Options.DefaultName;
        return _options.TryGetValue(name, out var options) ? options : default!;
    }

    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        _changeCallbacks.Add(listener);
        return new CallbackDisposable(() => _changeCallbacks.Remove(listener));
    }

    public void SetOptions(TOptions options, string? name = null)
    {
        name ??= Options.DefaultName;
        _options[name] = options;
    }

    public void NotifyChange(string? name = null)
    {
        name ??= Options.DefaultName;
        if (_options.TryGetValue(name, out var options))
        {
            foreach (var callback in _changeCallbacks)
            {
                callback(options, name);
            }
        }
    }

    private sealed class CallbackDisposable : IDisposable
    {
        private readonly Action _disposeAction;
        private bool _disposed;

        public CallbackDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposeAction();
                _disposed = true;
            }
        }
    }
}