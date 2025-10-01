#nullable enable
using System.Diagnostics;

namespace Polly.Bulkhead;

/// <summary>
/// An adaptive bulkhead-isolation policy which automatically adjusts the maximum concurrency 
/// based on latency and error rate using AIMD algorithm.
/// </summary>
#pragma warning disable CA1063
public class AdaptiveBulkheadPolicy : Policy, IBulkheadPolicy
#pragma warning restore CA1063
{
    private readonly AdaptiveBulkheadController _controller;
    private readonly Action<Context> _onBulkheadRejected;
    private readonly AdaptiveBulkheadSemaphoreManager _semaphoreManager;

    internal AdaptiveBulkheadPolicy(
        AdaptiveBulkheadOptions options,
        Action<Context> onBulkheadRejected)
    {
        _controller = new AdaptiveBulkheadController(options);
        _onBulkheadRejected = onBulkheadRejected;
        _semaphoreManager = new AdaptiveBulkheadSemaphoreManager(options.MaxQueueingActions, _controller);
    }

    /// <inheritdoc/>
    [DebuggerStepThrough]
    protected override TResult Implementation<TResult>(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return AdaptiveBulkheadEngine.Implementation(
            action,
            context,
            _onBulkheadRejected,
            _semaphoreManager,
            _controller,
            cancellationToken);
    }

    /// <summary>
    /// Gets the number of slots currently available for executing actions through the bulkhead.
    /// </summary>
    public int BulkheadAvailableCount => _semaphoreManager.BulkheadAvailableCount;

    /// <summary>
    /// Gets the number of slots currently available for queuing actions for execution through the bulkhead.
    /// </summary>
    public int QueueAvailableCount => _semaphoreManager.QueueAvailableCount;

    /// <summary>
    /// Gets the current maximum parallelization level.
    /// </summary>
    public int CurrentMaxParallelization => _controller.CurrentMaxParallelization;

    /// <summary>
    /// Gets the current metrics snapshot.
    /// </summary>
    public AdaptiveBulkheadMetrics.MetricsSnapshot GetCurrentMetrics() => _controller.GetCurrentMetrics();

    /// <summary>
    /// Event raised when parallelization is adjusted.
    /// </summary>
    public event Action<AdaptiveBulkheadController.ParallelizationAdjustment>? OnParallelizationAdjusted
    {
        add => _controller.OnParallelizationAdjusted += value;
        remove => _controller.OnParallelizationAdjusted -= value;
    }

#pragma warning disable CA1063
    /// <summary>
    /// Disposes of the <see cref="AdaptiveBulkheadPolicy"/>, allowing it to dispose its internal resources.
    /// </summary>
    public void Dispose()
#pragma warning restore CA1063
    {
        _controller?.Dispose();
        _semaphoreManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// An adaptive bulkhead-isolation policy which automatically adjusts the maximum concurrency 
/// based on latency and error rate using AIMD algorithm.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
#pragma warning disable CA1063
public class AdaptiveBulkheadPolicy<TResult> : Policy<TResult>, IBulkheadPolicy<TResult>
#pragma warning restore CA1063
{
    private readonly AdaptiveBulkheadController _controller;
    private readonly Action<Context> _onBulkheadRejected;
    private readonly AdaptiveBulkheadSemaphoreManager _semaphoreManager;

    internal AdaptiveBulkheadPolicy(
        AdaptiveBulkheadOptions options,
        Action<Context> onBulkheadRejected)
    {
        _controller = new AdaptiveBulkheadController(options);
        _onBulkheadRejected = onBulkheadRejected;
        _semaphoreManager = new AdaptiveBulkheadSemaphoreManager(options.MaxQueueingActions, _controller);
    }

    /// <inheritdoc/>
    [DebuggerStepThrough]
    protected override TResult Implementation(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return AdaptiveBulkheadEngine.Implementation(
            action,
            context,
            _onBulkheadRejected,
            _semaphoreManager,
            _controller,
            cancellationToken);
    }

    /// <summary>
    /// Gets the number of slots currently available for executing actions through the bulkhead.
    /// </summary>
    public int BulkheadAvailableCount => _semaphoreManager.BulkheadAvailableCount;

    /// <summary>
    /// Gets the number of slots currently available for queuing actions for execution through the bulkhead.
    /// </summary>
    public int QueueAvailableCount => _semaphoreManager.QueueAvailableCount;

    /// <summary>
    /// Gets the current maximum parallelization level.
    /// </summary>
    public int CurrentMaxParallelization => _controller.CurrentMaxParallelization;

    /// <summary>
    /// Gets the current metrics snapshot.
    /// </summary>
    public AdaptiveBulkheadMetrics.MetricsSnapshot GetCurrentMetrics() => _controller.GetCurrentMetrics();

    /// <summary>
    /// Event raised when parallelization is adjusted.
    /// </summary>
    public event Action<AdaptiveBulkheadController.ParallelizationAdjustment>? OnParallelizationAdjusted
    {
        add => _controller.OnParallelizationAdjusted += value;
        remove => _controller.OnParallelizationAdjusted -= value;
    }

#pragma warning disable CA1063
    /// <summary>
    /// Disposes of the <see cref="AdaptiveBulkheadPolicy"/>, allowing it to dispose its internal resources.
    /// </summary>
    public void Dispose()
#pragma warning restore CA1063
    {
        _controller?.Dispose();
        _semaphoreManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}