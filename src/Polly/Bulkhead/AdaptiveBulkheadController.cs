#nullable enable

namespace Polly.Bulkhead;

/// <summary>
/// Controls the adaptive adjustment of bulkhead parallelization using AIMD algorithm.
/// </summary>
public sealed class AdaptiveBulkheadController : IDisposable
{
    private readonly AdaptiveBulkheadOptions _options;
    private readonly AdaptiveBulkheadMetrics _metrics;
    private readonly Timer _adjustmentTimer;
    private readonly ReaderWriterLockSlim _parallelizationLock = new();
    
    private volatile int _currentMaxParallelization;
    private volatile bool _disposed;
    private DateTime _lastAdjustment = DateTime.UtcNow;

    public AdaptiveBulkheadController(AdaptiveBulkheadOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = new AdaptiveBulkheadMetrics(options.SamplingWindowSize);
        _currentMaxParallelization = options.InitialMaxParallelization;

        // Start adjustment timer
        _adjustmentTimer = new Timer(
            PerformAdjustment, 
            null, 
            _options.AdjustmentInterval, 
            _options.AdjustmentInterval);
    }

    /// <summary>
    /// Gets the current maximum parallelization value.
    /// </summary>
    public int CurrentMaxParallelization
    {
        get
        {
            _parallelizationLock.EnterReadLock();
            try
            {
                return _currentMaxParallelization;
            }
            finally
            {
                _parallelizationLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Records an execution sample for the adaptive algorithm.
    /// </summary>
    public void RecordExecution(TimeSpan latency, bool isError)
    {
        if (_disposed) return;
        _metrics.RecordExecution(latency, isError);
    }

    /// <summary>
    /// Gets the current metrics snapshot.
    /// </summary>
    public AdaptiveBulkheadMetrics.MetricsSnapshot GetCurrentMetrics()
    {
        return _metrics.GetCurrentMetrics();
    }

    private void PerformAdjustment(object? state)
    {
        if (_disposed) return;

        try
        {
            var metrics = _metrics.GetCurrentMetrics();
            
            // Need minimum samples to make decisions
            if (metrics.SampleCount < _options.MinSamplesForAdjustment)
            {
                return;
            }

            _parallelizationLock.EnterWriteLock();
            try
            {
                var newParallelization = CalculateNewParallelization(metrics);
                
                // Clamp to bounds
                newParallelization = Math.Max(_options.MinMaxParallelization, 
                    Math.Min(_options.MaxMaxParallelization, newParallelization));

                if (newParallelization != _currentMaxParallelization)
                {
                    _currentMaxParallelization = newParallelization;
                    _lastAdjustment = DateTime.UtcNow;
                    
                    // Notify about the adjustment (could add callback here for telemetry)
                    OnParallelizationAdjusted?.Invoke(new ParallelizationAdjustment(
                        _currentMaxParallelization, 
                        metrics.AverageLatency, 
                        metrics.ErrorRate,
                        metrics.SampleCount));
                }
            }
            finally
            {
                _parallelizationLock.ExitWriteLock();
            }
        }
        catch (Exception)
        {
            // Swallow exceptions in timer callback to prevent crashes
            // In production, this should be logged
        }
    }

    private int CalculateNewParallelization(AdaptiveBulkheadMetrics.MetricsSnapshot metrics)
    {
        var shouldDecrease = metrics.AverageLatency > _options.LatencyThreshold || 
                            metrics.ErrorRate > _options.ErrorRateThreshold;

        if (shouldDecrease)
        {
            // Multiplicative decrease (AIMD)
            return (int)Math.Ceiling(_currentMaxParallelization * _options.MultiplicativeDecrease);
        }
        else
        {
            // Additive increase (AIMD)
            return _currentMaxParallelization + _options.AdditiveIncrease;
        }
    }

    /// <summary>
    /// Event raised when parallelization is adjusted.
    /// </summary>
    public event Action<ParallelizationAdjustment>? OnParallelizationAdjusted;

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _adjustmentTimer?.Dispose();
        _parallelizationLock?.Dispose();
    }

    /// <summary>
    /// Information about a parallelization adjustment.
    /// </summary>
    public readonly struct ParallelizationAdjustment
    {
        public ParallelizationAdjustment(int newMaxParallelization, TimeSpan averageLatency, double errorRate, int sampleCount)
        {
            NewMaxParallelization = newMaxParallelization;
            AverageLatency = averageLatency;
            ErrorRate = errorRate;
            SampleCount = sampleCount;
        }

        public int NewMaxParallelization { get; }
        public TimeSpan AverageLatency { get; }
        public double ErrorRate { get; }
        public int SampleCount { get; }
    }
}