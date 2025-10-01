using Polly.RateLimiting;

namespace Snippets.Docs;

internal static class CompositeRateLimiter
{
    public static async Task Usage()
    {
        #region composite-rate-limiter

        // Create a composite rate limiter with Token Bucket + Leap Second Window
        // that adapts its parameters based on success/timeout metrics
        var options = new CompositeRateLimiterOptions
        {
            // Token Bucket configuration
            InitialTokenLimit = 100,
            MinTokenLimit = 10,
            MaxTokenLimit = 1000,
            InitialTokensPerPeriod = 10,
            MaxTokensPerPeriod = 100,
            TokenReplenishmentPeriod = TimeSpan.FromSeconds(1),

            // Leap Second Window configuration
            InitialPermitLimit = 50,
            MinPermitLimit = 5,
            MaxPermitLimit = 500,
            LeapSecondWindow = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 10,

            // Adaptive parameters
            MetricsWindow = TimeSpan.FromMinutes(1),
            HighSuccessThreshold = 0.9,
            LowSuccessThreshold = 0.5,
            IncreaseMultiplier = 1.2,
            DecreaseMultiplier = 0.8
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddCompositeRateLimiter(options)
            .Build();

        #endregion

        var cancellationToken = CancellationToken.None;

        #region composite-rate-limiter-execution

        try
        {
            // Execute an operation with adaptive rate limiting
            var result = await pipeline.ExecuteAsync(
                static async token =>
                {
                    // Your business logic here
                    await Task.Delay(100, token);
                    return "Operation completed successfully";
                },
                cancellationToken);

            Console.WriteLine($"Result: {result}");
        }
        catch (RateLimiterRejectedException ex)
        {
            // Handle rate limiting
            if (ex.RetryAfter is TimeSpan retryAfter)
            {
                Console.WriteLine($"Rate limited. Retry after: {retryAfter}");
            }
            else
            {
                Console.WriteLine("Rate limited. No retry information available.");
            }
        }

        #endregion

        // Cleanup
        pipeline.Dispose();
    }

    public static async Task AdaptiveBehavior()
    {
        #region composite-rate-limiter-adaptive

        // The composite rate limiter automatically adjusts its parameters
        // based on the moving average of success/timeout rates

        var options = new CompositeRateLimiterOptions
        {
            InitialTokenLimit = 50,
            InitialPermitLimit = 25,
            HighSuccessThreshold = 0.8, // When success rate > 80%, increase capacity
            LowSuccessThreshold = 0.3,  // When success rate < 30%, decrease capacity
            IncreaseMultiplier = 1.5,   // Increase capacity by 50%
            DecreaseMultiplier = 0.7,   // Decrease capacity by 30%
            MetricsWindow = TimeSpan.FromMinutes(2) // Analyze last 2 minutes
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddCompositeRateLimiter(options)
            .Build();

        // Simulate varying load conditions
        for (int i = 0; i < 100; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(static async _ =>
                {
                    // Simulate operations with varying success rates
                    await Task.Delay(Random.Shared.Next(10, 100));
                    
                    // Occasionally simulate failures/timeouts
                    if (Random.Shared.NextDouble() < 0.1)
                    {
                        throw new TimeoutException("Simulated timeout");
                    }
                    
                    return "Success";
                });
            }
            catch (RateLimiterRejectedException)
            {
                // Rate limited - the limiter will learn from this
                Console.WriteLine($"Rate limited at iteration {i}");
            }
            catch (TimeoutException)
            {
                // Business logic failure - the limiter will learn from this too
                Console.WriteLine($"Operation timeout at iteration {i}");
            }
        }

        #endregion

        // Cleanup
        pipeline.Dispose();
    }
}