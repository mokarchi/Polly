namespace Polly.RateLimiting;

/// <summary>
/// Tracks adaptive metrics for the composite rate limiter.
/// </summary>
internal sealed class AdaptiveMetrics
{
    private readonly TimeSpan _window;
    private readonly TimeProvider _timeProvider;
    private readonly Queue<MetricPoint> _dataPoints;
    private readonly object _lock = new();

    public AdaptiveMetrics(TimeSpan window, TimeProvider timeProvider)
    {
        _window = window;
        _timeProvider = timeProvider;
        _dataPoints = new Queue<MetricPoint>();
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            CleanOldData();
            _dataPoints.Enqueue(new MetricPoint(_timeProvider.GetUtcNow(), true));
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            CleanOldData();
            _dataPoints.Enqueue(new MetricPoint(_timeProvider.GetUtcNow(), false));
        }
    }

    public MetricsSnapshot GetMetrics()
    {
        lock (_lock)
        {
            CleanOldData();
            
            var successCount = 0;
            var totalCount = _dataPoints.Count;

            foreach (var point in _dataPoints)
            {
                if (point.IsSuccess)
                {
                    successCount++;
                }
            }

            return new MetricsSnapshot(successCount, totalCount);
        }
    }

    public MovingAverageSnapshot GetMovingAverage()
    {
        lock (_lock)
        {
            CleanOldData();
            
            if (_dataPoints.Count == 0)
            {
                return new MovingAverageSnapshot(0.5, 0); // Default 50% success rate
            }

            // Calculate moving average over different time windows
            var now = _timeProvider.GetUtcNow();
            var shortWindow = TimeSpan.FromTicks(_window.Ticks / 4); // 25% of window
            var mediumWindow = TimeSpan.FromTicks(_window.Ticks / 2); // 50% of window

            var recentPoints = _dataPoints.Where(p => now - p.Timestamp <= shortWindow).ToList();
            var mediumPoints = _dataPoints.Where(p => now - p.Timestamp <= mediumWindow).ToList();

            double recentSuccessRate = 0.5;
            double mediumSuccessRate = 0.5;

            if (recentPoints.Count > 0)
            {
                recentSuccessRate = (double)recentPoints.Count(p => p.IsSuccess) / recentPoints.Count;
            }

            if (mediumPoints.Count > 0)
            {
                mediumSuccessRate = (double)mediumPoints.Count(p => p.IsSuccess) / mediumPoints.Count;
            }

            // Weighted average favoring recent data
            var weightedSuccessRate = (recentSuccessRate * 0.6) + (mediumSuccessRate * 0.4);

            return new MovingAverageSnapshot(weightedSuccessRate, _dataPoints.Count);
        }
    }

    private void CleanOldData()
    {
        var cutoff = _timeProvider.GetUtcNow() - _window;
        while (_dataPoints.Count > 0 && _dataPoints.Peek().Timestamp < cutoff)
        {
            _dataPoints.Dequeue();
        }
    }

    private readonly record struct MetricPoint(DateTimeOffset Timestamp, bool IsSuccess);
}

/// <summary>
/// Represents a snapshot of metrics at a point in time.
/// </summary>
public readonly record struct MetricsSnapshot(int SuccessCount, int TotalAttempts)
{
    /// <summary>
    /// Gets the success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessCount / TotalAttempts : 0.5;
}

/// <summary>
/// Represents a snapshot of moving average metrics.
/// </summary>
public readonly record struct MovingAverageSnapshot(double SuccessRate, int SampleSize);