using Polly.Bulkhead;
using Polly.Specs.Helpers;

namespace Polly.Specs.Bulkhead;

public class AdaptiveBulkheadBehaviorSpecs
{
    [Fact]
    public void Should_increase_parallelization_when_conditions_are_good()
    {
        var adjustmentMade = false;
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 5,
            LatencyThreshold = TimeSpan.FromMilliseconds(100),
            ErrorRateThreshold = 0.1,
            AdditiveIncrease = 2,
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 3
        };

        var policy = Policy.AdaptiveBulkhead(options);
        
        policy.OnParallelizationAdjusted += adjustment =>
        {
            adjustmentMade = true;
            adjustment.NewMaxParallelization.ShouldBeGreaterThan(5);
        };

        // Execute several fast, successful operations
        for (int i = 0; i < 5; i++)
        {
            policy.Execute(() =>
            {
                Thread.Sleep(10); // Fast execution, well below threshold
                return "fast success";
            });
        }

        // Wait for adjustment interval
        Thread.Sleep(200);

        adjustmentMade.ShouldBeTrue();
        policy.CurrentMaxParallelization.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void Should_decrease_parallelization_when_latency_exceeds_threshold()
    {
        var adjustmentMade = false;
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 10,
            LatencyThreshold = TimeSpan.FromMilliseconds(50),
            ErrorRateThreshold = 0.1,
            MultiplicativeDecrease = 0.5,
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 2
        };

        var policy = Policy.AdaptiveBulkhead(options);
        var initialMax = policy.CurrentMaxParallelization;
        
        policy.OnParallelizationAdjusted += adjustment =>
        {
            adjustmentMade = true;
            adjustment.NewMaxParallelization.ShouldBeLessThan(initialMax);
        };

        // Execute operations that exceed latency threshold
        for (int i = 0; i < 3; i++)
        {
            policy.Execute(() =>
            {
                Thread.Sleep(100); // Slow execution, above threshold
                return "slow success";
            });
        }

        // Wait for adjustment interval
        Thread.Sleep(200);

        adjustmentMade.ShouldBeTrue();
        policy.CurrentMaxParallelization.ShouldBeLessThan(initialMax);
    }

    [Fact]
    public void Should_decrease_parallelization_when_error_rate_exceeds_threshold()
    {
        var adjustmentMade = false;
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 8,
            LatencyThreshold = TimeSpan.FromSeconds(1), // High threshold so latency doesn't trigger
            ErrorRateThreshold = 0.3,
            MultiplicativeDecrease = 0.7,
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 2
        };

        var policy = Policy.AdaptiveBulkhead(options);
        var initialMax = policy.CurrentMaxParallelization;
        
        policy.OnParallelizationAdjusted += adjustment =>
        {
            adjustmentMade = true;
            adjustment.NewMaxParallelization.ShouldBeLessThan(initialMax);
            adjustment.ErrorRate.ShouldBeGreaterThan(0.3);
        };

        // Execute operations with high error rate
        for (int i = 0; i < 5; i++)
        {
            try
            {
                policy.Execute(() =>
                {
                    if (i % 2 == 0) // 60% error rate
                        throw new InvalidOperationException("Simulated error");
                    
                    Thread.Sleep(5); // Fast when successful
                    return "success";
                });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Wait for adjustment interval
        Thread.Sleep(200);

        adjustmentMade.ShouldBeTrue();
        policy.CurrentMaxParallelization.ShouldBeLessThan(initialMax);
    }

    [Fact]
    public void Should_respect_min_and_max_parallelization_bounds()
    {
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 5,
            MinMaxParallelization = 3,
            MaxMaxParallelization = 10,
            LatencyThreshold = TimeSpan.FromMilliseconds(50),
            ErrorRateThreshold = 0.1,
            MultiplicativeDecrease = 0.1, // Very aggressive decrease
            AdditiveIncrease = 20, // Very aggressive increase
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 2
        };

        var policy = Policy.AdaptiveBulkhead(options);

        // First, trigger decrease to test min bound
        for (int i = 0; i < 3; i++)
        {
            policy.Execute(() =>
            {
                Thread.Sleep(200); // Very slow, triggers decrease
                return "slow";
            });
        }

        Thread.Sleep(200);
        policy.CurrentMaxParallelization.ShouldBeGreaterThanOrEqualTo(3); // Should not go below min

        // Reset to initial and test max bound
        var resetOptions = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 8,
            MinMaxParallelization = 3,
            MaxMaxParallelization = 10,
            LatencyThreshold = TimeSpan.FromMilliseconds(500), // High threshold
            ErrorRateThreshold = 0.9, // High threshold
            AdditiveIncrease = 5,
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 2
        };

        var policy2 = Policy.AdaptiveBulkhead(resetOptions);

        // Execute fast, successful operations to trigger increase
        for (int i = 0; i < 5; i++)
        {
            policy2.Execute(() =>
            {
                Thread.Sleep(1); // Very fast
                return "fast";
            });
        }

        Thread.Sleep(200);
        policy2.CurrentMaxParallelization.ShouldBeLessThanOrEqualTo(10); // Should not exceed max
    }

    [Fact]
    public void Should_not_adjust_without_minimum_samples()
    {
        var adjustmentMade = false;
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 5,
            LatencyThreshold = TimeSpan.FromMilliseconds(50),
            AdjustmentInterval = TimeSpan.FromMilliseconds(100),
            MinSamplesForAdjustment = 10 // High requirement
        };

        var policy = Policy.AdaptiveBulkhead(options);
        var initialMax = policy.CurrentMaxParallelization;
        
        policy.OnParallelizationAdjusted += adjustment =>
        {
            adjustmentMade = true;
        };

        // Execute only a few operations (below minimum)
        for (int i = 0; i < 3; i++)
        {
            policy.Execute(() =>
            {
                Thread.Sleep(200); // Slow execution
                return "slow";
            });
        }

        Thread.Sleep(200);

        adjustmentMade.ShouldBeFalse();
        policy.CurrentMaxParallelization.ShouldBe(initialMax); // Should not change
    }

    [Fact]
    public void Should_calculate_moving_average_correctly()
    {
        var options = new AdaptiveBulkheadOptions
        {
            SamplingWindowSize = 3, // Small window for testing
            MinSamplesForAdjustment = 1,
            AdjustmentInterval = TimeSpan.FromMilliseconds(50)
        };

        var policy = Policy.AdaptiveBulkhead(options);

        // Execute operations with known latencies
        policy.Execute(() => { Thread.Sleep(100); return "100ms"; });
        policy.Execute(() => { Thread.Sleep(200); return "200ms"; });
        policy.Execute(() => { Thread.Sleep(300); return "300ms"; });

        Thread.Sleep(100);

        var metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBe(3);
        
        // Average should be around 200ms
        metrics.AverageLatency.TotalMilliseconds.ShouldBeInRange(150, 250);

        // Add one more sample, should drop the oldest (100ms)
        policy.Execute(() => { Thread.Sleep(400); return "400ms"; });

        Thread.Sleep(50);

        metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBe(3); // Window size limit
        
        // New average should be around 300ms (200, 300, 400)
        metrics.AverageLatency.TotalMilliseconds.ShouldBeInRange(250, 350);
    }

    [Fact]
    public async Task Should_work_with_concurrent_async_executions()
    {
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 10,
            MaxQueueingActions = 15, // Allow more queuing for concurrent test
            AdjustmentInterval = TimeSpan.FromMilliseconds(200),
            MinSamplesForAdjustment = 5
        };

        var policy = Policy.AdaptiveBulkheadAsync(options);
        var tasks = new List<Task<string>>();

        // Execute many concurrent operations
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(policy.ExecuteAsync(async () =>
            {
                await Task.Delay(Random.Shared.Next(10, 50));
                return $"result-{i}";
            }));
        }

        var results = await Task.WhenAll(tasks);
        
        results.Length.ShouldBe(20);
        results.All(r => r.StartsWith("result-")).ShouldBeTrue();

        // Should have collected metrics
        var metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBeGreaterThan(0);
    }
}