using Polly.Utils.Pipeline;

namespace Polly.Core.Tests.Utils.Pipeline;

public static class DelegatingComponentTests
{
    [Fact]
    public static async Task ExecuteComponent_ReturnsCorrectResult()
    {
        await using var component = new CallbackComponent();
        var next = new CallbackComponent();
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);
        var state = 1;

        await using var delegating = new DelegatingComponent(component) { Next = next };

        var actual = await delegating.ExecuteComponent(
            async static (_, state) => await Outcome.FromResultAsValueTask(state + 1),
            context,
            state);

        actual.Result.ShouldBe(2);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public static async Task ExecuteComponentAot_ReturnsCorrectResult()
    {
        await using var component = new CallbackComponent();
        var next = new CallbackComponent();
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);
        var state = 1;

        await using var delegating = new DelegatingComponent(component) { Next = next };

        var actual = await delegating.ExecuteComponentAot(
            async static (_, state) => await Outcome.FromResultAsValueTask(state + 1),
            context,
            state);

        actual.Result.ShouldBe(2);
    }
#endif

    [Fact]
    public static async Task ExecuteNext_FastPathInlining_CompletedSuccessfully_AvoidAwait()
    {
        // This test validates that when a ValueTask is already completed successfully,
        // the fast-path optimization returns the result directly without awaiting

        var fastPathComponent = new FastPathTestComponent();
        var next = new CallbackComponent();
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);
        var state = 42;

        await using var delegating = new DelegatingComponent(fastPathComponent) { Next = next };

        var result = await delegating.ExecuteCore(
            static (_, state) => new ValueTask<Outcome<int>>(Outcome.FromResult(state * 2)),
            context,
            state);

        result.Result.ShouldBe(84);
        fastPathComponent.FastPathUsed.ShouldBeTrue("Fast-path optimization should have been used");
    }

    [Fact]
    public static async Task ExecuteNext_FastPathInlining_NotCompleted_UseNormalPath()
    {
        // This test validates that when a ValueTask is not completed,
        // the normal async path is used

        var slowPathComponent = new SlowPathTestComponent();
        var next = new CallbackComponent();
        var context = ResilienceContextPool.Shared.Get(TestCancellation.Token);
        var state = 42;

        await using var delegating = new DelegatingComponent(slowPathComponent) { Next = next };

        var result = await delegating.ExecuteCore(
            static async (_, state) =>
            {
                await Task.Delay(1); // Force async completion
                return Outcome.FromResult(state * 2);
            },
            context,
            state);

        result.Result.ShouldBe(84);
        slowPathComponent.SlowPathUsed.ShouldBeTrue("Slow path should have been used for non-completed ValueTask");
    }

    private sealed class CallbackComponent : PipelineComponent
    {
        public override ValueTask DisposeAsync() => default;

        internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state) => callback(context, state);
    }

    private sealed class FastPathTestComponent : PipelineComponent
    {
        public bool FastPathUsed { get; private set; }

        public override ValueTask DisposeAsync() => default;

        internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            var result = callback(context, state);
            if (result.IsCompletedSuccessfully)
            {
                FastPathUsed = true;
            }
            return result;
        }
    }

    private sealed class SlowPathTestComponent : PipelineComponent
    {
        public bool SlowPathUsed { get; private set; }

        public override ValueTask DisposeAsync() => default;

        internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            var result = callback(context, state);
            if (!result.IsCompletedSuccessfully)
            {
                SlowPathUsed = true;
            }
            return result;
        }
    }
}
