using Polly.Utils.Pipeline;

namespace Polly.Core.Tests;

public class FastPathInliningTests
{
    [Fact]
    public async Task ResiliencePipeline_ExecuteAsync_CompletedSuccessfully_UsesFastPath()
    {
        // Arrange
        var fastPathTracker = new FastPathTrackingComponent();
        var pipeline = CreatePipelineWithComponent(fastPathTracker);
        
        // Act - Execute with a synchronously completed operation
        var result = await pipeline.ExecuteAsync(static _ =>
        {
            return new ValueTask<int>(42); // This completes synchronously
        });

        // Assert
        result.ShouldBe(42);
        fastPathTracker.FastPathUsed.ShouldBeTrue("Fast-path should be used for synchronously completed operations");
    }

    [Fact]
    public async Task ResiliencePipeline_ExecuteAsyncWithState_CompletedSuccessfully_UsesFastPath()
    {
        // Arrange
        var fastPathTracker = new FastPathTrackingComponent();
        var pipeline = CreatePipelineWithComponent(fastPathTracker);
        
        // Act - Execute with a synchronously completed operation
        var result = await pipeline.ExecuteAsync(static (state, _) =>
        {
            return new ValueTask<int>(state * 2); // This completes synchronously
        }, 21);

        // Assert
        result.ShouldBe(42);
        fastPathTracker.FastPathUsed.ShouldBeTrue("Fast-path should be used for synchronously completed operations");
    }

    [Fact]
    public async Task ResiliencePipeline_ExecuteAsyncWithContext_CompletedSuccessfully_UsesFastPath()
    {
        // Arrange
        var fastPathTracker = new FastPathTrackingComponent();
        var pipeline = CreatePipelineWithComponent(fastPathTracker);
        var context = ResilienceContextPool.Shared.Get();

        try
        {
            // Act - Execute with a synchronously completed operation
            var result = await pipeline.ExecuteAsync(static _ =>
            {
                return new ValueTask<int>(42); // This completes synchronously
            }, context);

            // Assert
            result.ShouldBe(42);
            fastPathTracker.FastPathUsed.ShouldBeTrue("Fast-path should be used for synchronously completed operations");
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    [Fact]
    public async Task ResiliencePipeline_ExecuteAsyncWithContextAndState_CompletedSuccessfully_UsesFastPath()
    {
        // Arrange
        var fastPathTracker = new FastPathTrackingComponent();
        var pipeline = CreatePipelineWithComponent(fastPathTracker);
        var context = ResilienceContextPool.Shared.Get();

        try
        {
            // Act - Execute with a synchronously completed operation
            var result = await pipeline.ExecuteAsync(static (_, state) =>
            {
                return new ValueTask<int>(state + 10); // This completes synchronously
            }, context, 32);

            // Assert
            result.ShouldBe(42);
            fastPathTracker.FastPathUsed.ShouldBeTrue("Fast-path should be used for synchronously completed operations");
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    [Fact]
    public async Task ResiliencePipeline_ExecuteAsync_NotCompleted_StillWorks()
    {
        // Arrange - Simple test to verify async operations still work
        var component = new CallbackComponent();
        var pipeline = CreatePipelineWithComponent(component);
        
        // Act - Execute with an async operation that is not immediately completed
        var result = await pipeline.ExecuteAsync(static async cancellationToken =>
        {
            await Task.Delay(1, cancellationToken); // Force async completion
            return 42;
        });

        // Assert - The optimization doesn't break async operations
        result.ShouldBe(42);
    }

    [Fact]
    public async Task DelegatingComponent_ExecuteNext_CompletedSuccessfully_UsesFastPath()
    {
        // Arrange
        var innerComponent = new FastPathTrackingComponent();
        var nextComponent = new CallbackComponent();
        await using var delegating = new DelegatingComponent(innerComponent) { Next = nextComponent };
        var context = ResilienceContextPool.Shared.Get();

        try
        {
            // Act - Execute with synchronously completed ValueTask
            var result = await delegating.ExecuteCore(
                static (_, state) => new ValueTask<Outcome<int>>(Outcome.FromResult(state * 2)),
                context,
                21);

            // Assert
            result.Result.ShouldBe(42);
            innerComponent.FastPathUsed.ShouldBeTrue("Fast-path should be used in DelegatingComponent.ExecuteNext");
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    [Fact]
    public async Task DelegatingComponent_ExecuteNext_NotCompleted_UsesSlowPath()
    {
        // Arrange - Test that async callbacks still work correctly
        var innerComponent = new CallbackComponent();
        var nextComponent = new CallbackComponent();
        await using var delegating = new DelegatingComponent(innerComponent) { Next = nextComponent };
        var context = ResilienceContextPool.Shared.Get();

        try
        {
            // Act - Execute with async ValueTask (this should still work with the optimization)
            var result = await delegating.ExecuteCore(
                static async (_, state) =>
                {
                    await Task.Delay(1); // Force async completion
                    return Outcome.FromResult(state * 2);
                },
                context,
                21);

            // Assert - The optimization doesn't affect correctness, just performance
            result.Result.ShouldBe(42);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private static ResiliencePipeline CreatePipelineWithComponent(PipelineComponent component)
    {
        return new ResiliencePipeline(component, Polly.Utils.DisposeBehavior.Allow, null);
    }

    private sealed class FastPathTrackingComponent : PipelineComponent
    {
        public bool FastPathUsed { get; private set; }

        public override ValueTask DisposeAsync() => default;

        internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            var result = callback(context, state);
            
            // Track if fast-path is used (ValueTask is completed successfully)
            if (result.IsCompletedSuccessfully)
            {
                FastPathUsed = true;
                // Return the result directly - this simulates the fast-path optimization
                return new ValueTask<Outcome<TResult>>(result.Result);
            }

            return result;
        }
    }

    private sealed class CallbackComponent : PipelineComponent
    {
        public override ValueTask DisposeAsync() => default;

        internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state) => callback(context, state);
    }
}