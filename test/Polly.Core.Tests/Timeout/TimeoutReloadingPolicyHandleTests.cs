using Polly.Timeout;
using Xunit;

namespace Polly.Core.Tests.Timeout;

public class TimeoutReloadingPolicyHandleTests
{
    [Fact]
    public void Constructor_WithValidOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var options = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var handle = new TimeoutReloadingPolicyHandle(options);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), handle.GetTimeout());
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TimeoutReloadingPolicyHandle(null!));
    }

    [Fact]
    public void OnConfigurationChanged_WithValidOptions_ShouldUpdateState()
    {
        // Arrange
        var initialOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);

        var newOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        // Act
        handle.OnConfigurationChanged(newOptions);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(60), handle.GetTimeout());
    }

    [Fact]
    public void OnConfigurationChanged_WithInvalidType_ShouldNotUpdateState()
    {
        // Arrange
        var initialOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);

        // Act
        handle.OnConfigurationChanged("invalid options");

        // Assert - state should remain unchanged
        Assert.Equal(TimeSpan.FromSeconds(30), handle.GetTimeout());
    }

    [Fact]
    public void UpdateState_ShouldReturnPreviousState()
    {
        // Arrange
        var initialOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);
        var newState = new TimeoutState(TimeSpan.FromSeconds(60));

        // Act
        var previousState = handle.UpdateState(newState);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), previousState.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(60), handle.GetTimeout());
    }

    [Fact]
    public void CompareAndUpdateState_WithMatchingState_ShouldUpdate()
    {
        // Arrange
        var initialOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);
        var currentState = handle.GetCurrentState();
        var newState = new TimeoutState(TimeSpan.FromSeconds(60));

        // Act
        var result = handle.CompareAndUpdateState(newState, currentState);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), result.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(60), handle.GetTimeout());
    }

    [Fact]
    public void MultipleThreads_CanUpdateStateConcurrently()
    {
        // Arrange
        var initialOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(1)
        };
        var handle = new TimeoutReloadingPolicyHandle(initialOptions);
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
                    var newState = new TimeoutState(TimeSpan.FromMilliseconds((threadId * operationsPerThread + j + 1) * 10));
                    handle.UpdateState(newState);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - no exceptions should occur and state should be valid
        var finalState = handle.GetCurrentState();
        Assert.True(finalState.Timeout > TimeSpan.Zero);
    }
}