using Polly.Bulkhead;

namespace Snippets.Docs;

internal static partial class AdaptiveBulkheadExamples
{
    public static void BasicUsage()
    {
        #region adaptive-bulkhead-basic

        // Create adaptive bulkhead with default options
        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 10,
            MinMaxParallelization = 2,
            MaxMaxParallelization = 50,
            LatencyThreshold = TimeSpan.FromMilliseconds(100),
            ErrorRateThreshold = 0.1 // 10% error rate threshold
        });

        // Execute operations - the bulkhead will automatically adjust
        for (int i = 0; i < 100; i++)
        {
            try
            {
                var result = policy.Execute(() =>
                {
                    // Simulate some work with varying latency
                    Thread.Sleep(Random.Shared.Next(50, 200));
                    
                    // Simulate occasional errors
                    if (Random.Shared.NextDouble() < 0.05) // 5% error rate
                        throw new InvalidOperationException("Simulated error");
                    
                    return $"Result {i}";
                });
                
                Console.WriteLine($"Success: {result}");
            }
            catch (BulkheadRejectedException)
            {
                Console.WriteLine($"Request {i} was rejected - bulkhead full");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Request {i} failed: {ex.Message}");
            }
        }

        #endregion
    }

    public static void ConfigurationOptions()
    {
        #region adaptive-bulkhead-configuration

        var policy = Policy.AdaptiveBulkhead(options =>
        {
            // Initial parallelization level
            options.InitialMaxParallelization = 15;
            
            // Bounds for adaptive adjustments
            options.MinMaxParallelization = 5;
            options.MaxMaxParallelization = 100;
            
            // Queue capacity for waiting requests
            options.MaxQueueingActions = 20;
            
            // Thresholds for triggering decreases
            options.LatencyThreshold = TimeSpan.FromMilliseconds(150);
            options.ErrorRateThreshold = 0.15; // 15%
            
            // AIMD parameters
            options.AdditiveIncrease = 2; // Add 2 slots when conditions are good
            options.MultiplicativeDecrease = 0.6; // Reduce to 60% when conditions are poor
            
            // Measurement settings
            options.SamplingWindowSize = 50; // Track last 50 executions
            options.AdjustmentInterval = TimeSpan.FromSeconds(5); // Adjust every 5 seconds
            options.MinSamplesForAdjustment = 10; // Need at least 10 samples
        });

        #endregion
    }

    public static void MonitoringAndEvents()
    {
        #region adaptive-bulkhead-monitoring

        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 10,
            AdjustmentInterval = TimeSpan.FromSeconds(2)
        });

        // Subscribe to adjustment events
        policy.OnParallelizationAdjusted += adjustment =>
        {
            Console.WriteLine($"Bulkhead adjusted: {adjustment.NewMaxParallelization} slots " +
                             $"(avg latency: {adjustment.AverageLatency.TotalMilliseconds:F1}ms, " +
                             $"error rate: {adjustment.ErrorRate:P1})");
        };

        // Monitor current state
        var timer = new Timer(_ =>
        {
            var metrics = policy.GetCurrentMetrics();
            Console.WriteLine($"Current: {policy.CurrentMaxParallelization} max slots, " +
                             $"{policy.BulkheadAvailableCount} available, " +
                             $"{metrics.SampleCount} samples");
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        #endregion
    }

    public static async Task AsyncUsage()
    {
        #region adaptive-bulkhead-async

        var policy = Policy.AdaptiveBulkheadAsync(new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 8,
            MaxQueueingActions = 15
        });

        // Execute concurrent async operations
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            try
            {
                var result = await policy.ExecuteAsync(async () =>
                {
                    // Simulate async work
                    await Task.Delay(Random.Shared.Next(50, 300));
                    
                    if (Random.Shared.NextDouble() < 0.1) // 10% error rate
                        throw new HttpRequestException("Simulated network error");
                    
                    return $"Async result {i}";
                });
                
                return result;
            }
            catch (BulkheadRejectedException)
            {
                return $"Request {i} rejected";
            }
            catch (HttpRequestException)
            {
                return $"Request {i} failed";
            }
        });

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Console.WriteLine(result);
        }

        #endregion
    }

    public static void ComparisonWithStaticBulkhead()
    {
        #region adaptive-vs-static-bulkhead

        // Static bulkhead - fixed capacity
        var staticPolicy = Policy.Bulkhead(
            maxParallelization: 10,
            maxQueuingActions: 5);

        // Adaptive bulkhead - automatically adjusts based on conditions
        var adaptivePolicy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 10,
            MinMaxParallelization = 5,
            MaxMaxParallelization = 25,
            LatencyThreshold = TimeSpan.FromMilliseconds(100),
            ErrorRateThreshold = 0.1
        });

        // Benefits of adaptive bulkhead:
        // - Increases capacity during low latency periods
        // - Decreases capacity when latency increases or errors occur
        // - Provides better throughput under varying load conditions
        // - Reduces resource contention during high error rates

        #endregion
    }
}