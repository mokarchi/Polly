#nullable enable
using System.Diagnostics;

namespace Polly.Bulkhead;

/// <summary>
/// An adaptive async bulkhead-isolation policy which automatically adjusts the maximum concurrency 
/// based on latency and error rate using AIMD algorithm.
/// </summary>
#pragma warning disable CA1063
public class AsyncAdaptiveBulkheadPolicy : AsyncPolicy, IBulkheadPolicy
#pragma warning restore CA1063
{
    private readonly AdaptiveBulkheadController _controller;
    private readonly Func<Context, Task> _onBulkheadRejectedAsync;
    private readonly AdaptiveBulkheadSemaphoreManager _semaphoreManager;

    internal AsyncAdaptiveBulkheadPolicy(
        AdaptiveBulkheadOptions options,
        Func<Context, Task> onBulkheadRejectedAsync)
    {
        _controller = new AdaptiveBulkheadController(options);
        _onBulkheadRejectedAsync = onBulkheadRejectedAsync;
        _semaphoreManager = new AdaptiveBulkheadSemaphoreManager(options.MaxQueueingActions, _controller);
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

    /// <inheritdoc/>
    [DebuggerStepThrough]
    protected override Task<TResult> ImplementationAsync<TResult>(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return AsyncAdaptiveBulkheadEngine.ImplementationAsync(
            action,
            context,
            _onBulkheadRejectedAsync,
            _semaphoreManager,
            _controller,
            continueOnCapturedContext,
            cancellationToken);
    }

#pragma warning disable CA1063
    /// <inheritdoc/>
    public void Dispose()
#pragma warning restore CA1063
    {
        _controller?.Dispose();
        _semaphoreManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// An adaptive async bulkhead-isolation policy which automatically adjusts the maximum concurrency 
/// based on latency and error rate using AIMD algorithm.
/// </summary>
/// <typeparam name="TResult">The return type of delegates which may be executed through the policy.</typeparam>
#pragma warning disable CA1063
public class AsyncAdaptiveBulkheadPolicy<TResult> : AsyncPolicy<TResult>, IBulkheadPolicy<TResult>
#pragma warning restore CA1063
{
    private readonly AdaptiveBulkheadController _controller;
    private readonly Func<Context, Task> _onBulkheadRejectedAsync;
    private readonly AdaptiveBulkheadSemaphoreManager _semaphoreManager;

    internal AsyncAdaptiveBulkheadPolicy(
        AdaptiveBulkheadOptions options,
        Func<Context, Task> onBulkheadRejectedAsync)
    {
        _controller = new AdaptiveBulkheadController(options);
        _onBulkheadRejectedAsync = onBulkheadRejectedAsync;
        _semaphoreManager = new AdaptiveBulkheadSemaphoreManager(options.MaxQueueingActions, _controller);
    }

    /// <inheritdoc/>
    [DebuggerStepThrough]
    protected override Task<TResult> ImplementationAsync(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return AsyncAdaptiveBulkheadEngine.ImplementationAsync(
            action,
            context,
            _onBulkheadRejectedAsync,
            _semaphoreManager,
            _controller,
            continueOnCapturedContext,
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
    /// <inheritdoc/>
    public void Dispose()
#pragma warning restore CA1063
    {
        _controller?.Dispose();
        _semaphoreManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}