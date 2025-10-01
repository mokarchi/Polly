using Polly.Retry;
using Xunit;

namespace Polly.Core.Tests.Retry;

public class RetryReloadingPolicyHandleTests
{
    [Fact]
    public void Constructor_WithValidOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var options = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(10),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        };

        // Act
        var handle = new RetryReloadingPolicyHandle<string>(options);

        // Assert
        Assert.Equal(3, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(1), handle.GetBaseDelay());
        Assert.Equal(TimeSpan.FromSeconds(10), handle.GetMaxDelay());
        Assert.Equal(DelayBackoffType.Exponential, handle.GetBackoffType());
        Assert.True(handle.GetUseJitter());
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RetryReloadingPolicyHandle<string>(null!));
    }

    [Fact]
    public void OnConfigurationChanged_WithValidOptions_ShouldUpdateState()
    {
        // Arrange
        var initialOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false
        };
        var handle = new RetryReloadingPolicyHandle<string>(initialOptions);

        var newOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        };

        // Act
        handle.OnConfigurationChanged(newOptions);

        // Assert
        Assert.Equal(5, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), handle.GetBaseDelay());
        Assert.Equal(TimeSpan.FromSeconds(30), handle.GetMaxDelay());
        Assert.Equal(DelayBackoffType.Exponential, handle.GetBackoffType());
        Assert.True(handle.GetUseJitter());
    }

    [Fact]
    public void OnConfigurationChanged_WithInvalidType_ShouldNotUpdateState()
    {
        // Arrange
        var initialOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false
        };
        var handle = new RetryReloadingPolicyHandle<string>(initialOptions);

        // Act
        handle.OnConfigurationChanged("invalid options");

        // Assert - state should remain unchanged
        Assert.Equal(3, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(1), handle.GetBaseDelay());
        Assert.Equal(DelayBackoffType.Constant, handle.GetBackoffType());
        Assert.False(handle.GetUseJitter());
    }

    [Fact]
    public void UpdateState_ShouldReturnPreviousState()
    {
        // Arrange
        var initialOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        };
        var handle = new RetryReloadingPolicyHandle<string>(initialOptions);
        var newState = new RetryState(5, TimeSpan.FromSeconds(2), null, DelayBackoffType.Exponential, true);

        // Act
        var previousState = handle.UpdateState(newState);

        // Assert
        Assert.Equal(3, previousState.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), previousState.BaseDelay);
        Assert.Equal(5, handle.GetMaxRetryAttempts());
        Assert.Equal(TimeSpan.FromSeconds(2), handle.GetBaseDelay());
    }

    [Fact]
    public void CompareAndUpdateState_WithMatchingState_ShouldUpdate()
    {
        // Arrange
        var initialOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1)
        };
        var handle = new RetryReloadingPolicyHandle<string>(initialOptions);
        var currentState = handle.GetCurrentState();
        var newState = new RetryState(5, TimeSpan.FromSeconds(2), null, DelayBackoffType.Exponential, true);

        // Act
        var result = handle.CompareAndUpdateState(newState, currentState);

        // Assert
        Assert.Equal(currentState.MaxRetryAttempts, result.MaxRetryAttempts);
        Assert.Equal(5, handle.GetMaxRetryAttempts());
    }

    [Fact]
    public void MultipleThreads_CanUpdateStateConcurrently()
    {
        // Arrange
        var initialOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromSeconds(1)
        };
        var handle = new RetryReloadingPolicyHandle<string>(initialOptions);
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var barrier = new Barrier(threadCount);

        // Act
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var newState = new RetryState(threadId * operationsPerThread + j + 1, TimeSpan.FromMilliseconds(j + 1), null, DelayBackoffType.Constant, false);
                    handle.UpdateState(newState);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions should occur and state should be valid
        var finalState = handle.GetCurrentState();
        Assert.True(finalState.MaxRetryAttempts > 0);
    }
}