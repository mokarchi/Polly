#nullable enable
using System.Collections.Concurrent;

namespace Polly.Bulkhead;

/// <summary>
/// Tracks execution metrics for adaptive bulkhead policy including latency and error rates.
/// </summary>
public sealed class AdaptiveBulkheadMetrics
{
    private readonly int _windowSize;
    private readonly ConcurrentQueue<ExecutionSample> _samples = new();
    private readonly object _lock = new();
    private volatile int _sampleCount;

    public AdaptiveBulkheadMetrics(int windowSize)
    {
        _windowSize = windowSize;
    }

    /// <summary>
    /// Records an execution sample with its latency and success/failure status.
    /// </summary>
    public void RecordExecution(TimeSpan latency, bool isError)
    {
        var sample = new ExecutionSample(DateTime.UtcNow, latency, isError);
        
        lock (_lock)
        {
            _samples.Enqueue(sample);
            Interlocked.Increment(ref _sampleCount);

            // Remove old samples beyond window size
            while (_sampleCount > _windowSize)
            {
                if (_samples.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _sampleCount);
                }
            }
        }
    }

    /// <summary>
    /// Gets the current metrics including average latency and error rate.
    /// </summary>
    public MetricsSnapshot GetCurrentMetrics()
    {
        lock (_lock)
        {
            var samples = _samples.ToArray();
            if (samples.Length == 0)
            {
                return new MetricsSnapshot(TimeSpan.Zero, 0.0, 0);
            }

            var totalLatency = TimeSpan.Zero;
            var errorCount = 0;

            foreach (var sample in samples)
            {
                totalLatency = totalLatency.Add(sample.Latency);
                if (sample.IsError)
                {
                    errorCount++;
                }
            }

            var averageLatency = TimeSpan.FromMilliseconds(totalLatency.TotalMilliseconds / samples.Length);
            var errorRate = (double)errorCount / samples.Length;

            return new MetricsSnapshot(averageLatency, errorRate, samples.Length);
        }
    }

    /// <summary>
    /// Represents a single execution sample.
    /// </summary>
    private readonly struct ExecutionSample
    {
        public ExecutionSample(DateTime timestamp, TimeSpan latency, bool isError)
        {
            Timestamp = timestamp;
            Latency = latency;
            IsError = isError;
        }

        public DateTime Timestamp { get; }
        public TimeSpan Latency { get; }
        public bool IsError { get; }
    }

    /// <summary>
    /// Represents a snapshot of current metrics.
    /// </summary>
    public readonly struct MetricsSnapshot
    {
        public MetricsSnapshot(TimeSpan averageLatency, double errorRate, int sampleCount)
        {
            AverageLatency = averageLatency;
            ErrorRate = errorRate;
            SampleCount = sampleCount;
        }

        public TimeSpan AverageLatency { get; }
        public double ErrorRate { get; }
        public int SampleCount { get; }
    }
}