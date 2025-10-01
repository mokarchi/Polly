#nullable enable

namespace Polly.Bulkhead;

/// <summary>
/// Manages semaphores for adaptive bulkhead policy, dynamically adjusting parallelization limits.
/// </summary>
internal sealed class AdaptiveBulkheadSemaphoreManager : IDisposable
{
    private readonly int _maxQueueingActions;
    private readonly AdaptiveBulkheadController _controller;
    private readonly SemaphoreSlim _maxQueuedActionsSemaphore;
    private readonly ReaderWriterLockSlim _lock = new();
    
    private SemaphoreSlim _maxParallelizationSemaphore;
    private int _lastKnownMaxParallelization;
    private volatile bool _disposed;

    public AdaptiveBulkheadSemaphoreManager(int maxQueueingActions, AdaptiveBulkheadController controller)
    {
        _maxQueueingActions = maxQueueingActions;
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _lastKnownMaxParallelization = controller.CurrentMaxParallelization;
        
        // Initialize semaphores
        _maxParallelizationSemaphore = new SemaphoreSlim(_lastKnownMaxParallelization, _lastKnownMaxParallelization);
        
        // Follow the same pattern as original bulkhead: add max parallelization to max queuing
        var maxQueuingCompounded = (int)Math.Min((long)maxQueueingActions + _lastKnownMaxParallelization, int.MaxValue);
        _maxQueuedActionsSemaphore = new SemaphoreSlim(maxQueuingCompounded, maxQueuingCompounded);
    }

    /// <summary>
    /// Gets the current parallelization semaphore, creating a new one if the limit has changed.
    /// </summary>
    public SemaphoreSlim GetParallelizationSemaphore()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AdaptiveBulkheadSemaphoreManager));

        var currentMax = _controller.CurrentMaxParallelization;
        
        if (currentMax == _lastKnownMaxParallelization)
        {
            _lock.EnterReadLock();
            try
            {
                return _maxParallelizationSemaphore;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        // Need to update the semaphore
        _lock.EnterWriteLock();
        try
        {
            // Double-check after acquiring write lock
            currentMax = _controller.CurrentMaxParallelization;
            if (currentMax != _lastKnownMaxParallelization)
            {
                var oldSemaphore = _maxParallelizationSemaphore;
                _maxParallelizationSemaphore = new SemaphoreSlim(currentMax, currentMax);
                _lastKnownMaxParallelization = currentMax;
                
                // Dispose old semaphore after a delay to allow in-flight operations to complete
                Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => oldSemaphore.Dispose());
            }
            
            return _maxParallelizationSemaphore;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the queue semaphore.
    /// </summary>
    public SemaphoreSlim QueueSemaphore => _maxQueuedActionsSemaphore;

    /// <summary>
    /// Gets the number of slots currently available for executing actions through the bulkhead.
    /// </summary>
    public int BulkheadAvailableCount
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _maxParallelizationSemaphore.CurrentCount;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets the number of slots currently available for queuing actions for execution through the bulkhead.
    /// </summary>
    public int QueueAvailableCount => Math.Min(_maxQueuedActionsSemaphore.CurrentCount, _maxQueueingActions);

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        _lock.EnterWriteLock();
        try
        {
            _maxParallelizationSemaphore?.Dispose();
            _maxQueuedActionsSemaphore?.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
            _lock?.Dispose();
        }
    }
}