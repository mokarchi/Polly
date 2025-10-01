using Polly.Bulkhead;

namespace Polly.Specs.Bulkhead;

public class AdaptiveBulkheadMetricsSpecs
{
    [Fact]
    public void Should_start_with_empty_metrics()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        
        var snapshot = metrics.GetCurrentMetrics();
        
        snapshot.SampleCount.ShouldBe(0);
        snapshot.AverageLatency.ShouldBe(TimeSpan.Zero);
        snapshot.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_record_single_successful_execution()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        var latency = TimeSpan.FromMilliseconds(50);
        
        metrics.RecordExecution(latency, isError: false);
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(1);
        snapshot.AverageLatency.ShouldBe(latency);
        snapshot.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_record_single_failed_execution()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        var latency = TimeSpan.FromMilliseconds(75);
        
        metrics.RecordExecution(latency, isError: true);
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(1);
        snapshot.AverageLatency.ShouldBe(latency);
        snapshot.ErrorRate.ShouldBe(1.0);
    }

    [Fact]
    public void Should_calculate_average_latency_correctly()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        
        metrics.RecordExecution(TimeSpan.FromMilliseconds(100), false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(200), false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(300), false);
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(3);
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(200, tolerance: 1);
        snapshot.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_calculate_error_rate_correctly()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        
        metrics.RecordExecution(TimeSpan.FromMilliseconds(50), false); // Success
        metrics.RecordExecution(TimeSpan.FromMilliseconds(60), true);  // Error
        metrics.RecordExecution(TimeSpan.FromMilliseconds(70), false); // Success
        metrics.RecordExecution(TimeSpan.FromMilliseconds(80), true);  // Error
        metrics.RecordExecution(TimeSpan.FromMilliseconds(90), false); // Success
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(5);
        snapshot.ErrorRate.ShouldBe(0.4); // 2 errors out of 5 = 40%
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(70, tolerance: 1);
    }

    [Fact]
    public void Should_maintain_window_size_limit()
    {
        var windowSize = 3;
        var metrics = new AdaptiveBulkheadMetrics(windowSize);
        
        // Add more samples than window size
        metrics.RecordExecution(TimeSpan.FromMilliseconds(100), false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(200), false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(300), false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(400), false); // Should evict first sample
        metrics.RecordExecution(TimeSpan.FromMilliseconds(500), false); // Should evict second sample
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(windowSize);
        
        // Average should be based on last 3 samples: 300, 400, 500 = 400ms average
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(400, tolerance: 1);
    }

    [Fact]
    public void Should_handle_mixed_success_and_error_in_sliding_window()
    {
        var windowSize = 4;
        var metrics = new AdaptiveBulkheadMetrics(windowSize);
        
        // Fill window: S, E, S, E
        metrics.RecordExecution(TimeSpan.FromMilliseconds(100), false); // Success
        metrics.RecordExecution(TimeSpan.FromMilliseconds(150), true);  // Error
        metrics.RecordExecution(TimeSpan.FromMilliseconds(200), false); // Success
        metrics.RecordExecution(TimeSpan.FromMilliseconds(250), true);  // Error
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(4);
        snapshot.ErrorRate.ShouldBe(0.5); // 50% error rate
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(175, tolerance: 1);
        
        // Add one more success, should evict first sample (S)
        // Window becomes: E, S, E, S
        metrics.RecordExecution(TimeSpan.FromMilliseconds(300), false); // Success
        
        snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(4);
        snapshot.ErrorRate.ShouldBe(0.5); // Still 50% error rate
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(225, tolerance: 1); // (150+200+250+300)/4
    }

    [Fact]
    public void Should_be_thread_safe()
    {
        var metrics = new AdaptiveBulkheadMetrics(1000);
        var tasks = new List<Task>();
        var totalOperations = 100;
        
        // Execute many concurrent operations
        for (int i = 0; i < totalOperations; i++)
        {
            var operationId = i;
            tasks.Add(Task.Run(() =>
            {
                var latency = TimeSpan.FromMilliseconds(operationId % 100 + 10);
                var isError = operationId % 4 == 0; // 25% error rate
                metrics.RecordExecution(latency, isError);
            }));
        }
        
        Task.WaitAll(tasks.ToArray());
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(totalOperations);
        snapshot.ErrorRate.ShouldBe(0.25, tolerance: 0.01); // Should be close to 25%
        snapshot.AverageLatency.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Should_handle_zero_latency()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        
        metrics.RecordExecution(TimeSpan.Zero, false);
        metrics.RecordExecution(TimeSpan.FromMilliseconds(100), false);
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(2);
        snapshot.AverageLatency.TotalMilliseconds.ShouldBe(50, tolerance: 1);
        snapshot.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_handle_very_large_latencies()
    {
        var metrics = new AdaptiveBulkheadMetrics(100);
        
        metrics.RecordExecution(TimeSpan.FromMinutes(1), false);
        metrics.RecordExecution(TimeSpan.FromMinutes(2), false);
        
        var snapshot = metrics.GetCurrentMetrics();
        snapshot.SampleCount.ShouldBe(2);
        snapshot.AverageLatency.TotalMinutes.ShouldBe(1.5, tolerance: 0.1);
        snapshot.ErrorRate.ShouldBe(0.0);
    }
}