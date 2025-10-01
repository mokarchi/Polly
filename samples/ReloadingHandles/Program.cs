using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions;
using Polly.Retry;
using Polly.Timeout;

// Example demonstrating ReloadingPolicyHandle usage with IOptionsMonitor
// This allows for atomic updates of retry/timeout configuration without rebuilding pipelines

var services = new ServiceCollection();

// Register options services
services.AddOptions();

// Configure retry options that can change at runtime
services.Configure<RetryStrategyOptions<string>>(options =>
{
    options.MaxRetryAttempts = 3;
    options.Delay = TimeSpan.FromSeconds(1);
    options.BackoffType = DelayBackoffType.Exponential;
    options.UseJitter = true;
});

// Configure timeout options
services.Configure<TimeoutStrategyOptions>(options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
});

var serviceProvider = services.BuildServiceProvider();

// Get options monitor
var retryOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RetryStrategyOptions<string>>>();
var timeoutOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<TimeoutStrategyOptions>>();

// Create reloading handles that will automatically update when options change
var retryHandle = retryOptionsMonitor.CreateReloadingHandle<string>();
var timeoutHandle = timeoutOptionsMonitor.CreateReloadingHandle();

// Build resilience pipeline with reloading handles
var pipeline = new ResiliencePipelineBuilder<string>()
    .AddRetry(new RetryStrategyOptions<string>(), retryHandle)
    .AddTimeout(new TimeoutStrategyOptions(), timeoutHandle)
    .Build();

Console.WriteLine("=== ReloadingPolicyHandle Demo ===");
Console.WriteLine($"Initial retry attempts: {retryHandle.GetMaxRetryAttempts()}");
Console.WriteLine($"Initial retry delay: {retryHandle.GetBaseDelay()}");
Console.WriteLine($"Initial timeout: {timeoutHandle.GetTimeout()}");

// Simulate configuration change (in real app, this would come from config providers)
Console.WriteLine("\n=== Simulating Configuration Change ===");

// Update retry options
var updatedRetryOptions = new RetryStrategyOptions<string>
{
    MaxRetryAttempts = 5,
    Delay = TimeSpan.FromSeconds(2),
    BackoffType = DelayBackoffType.Linear,
    UseJitter = false
};
retryHandle.OnConfigurationChanged(updatedRetryOptions);

// Update timeout options
var updatedTimeoutOptions = new TimeoutStrategyOptions
{
    Timeout = TimeSpan.FromSeconds(60)
};
timeoutHandle.OnConfigurationChanged(updatedTimeoutOptions);

Console.WriteLine($"Updated retry attempts: {retryHandle.GetMaxRetryAttempts()}");
Console.WriteLine($"Updated retry delay: {retryHandle.GetBaseDelay()}");
Console.WriteLine($"Updated timeout: {timeoutHandle.GetTimeout()}");

// Demonstrate concurrent access safety
Console.WriteLine("\n=== Concurrent Access Test ===");
var tasks = new List<Task>();

for (int i = 0; i < 10; i++)
{
    int taskId = i;
    tasks.Add(Task.Run(() =>
    {
        for (int j = 0; j < 100; j++)
        {
            // Read configuration concurrently
            var attempts = retryHandle.GetMaxRetryAttempts();
            var delay = retryHandle.GetBaseDelay();
            var timeout = timeoutHandle.GetTimeout();

            // Simulate some configuration updates
            if (j % 20 == 0)
            {
                var newRetryOptions = new RetryStrategyOptions<string>
                {
                    MaxRetryAttempts = 3 + (taskId % 5),
                    Delay = TimeSpan.FromMilliseconds(500 + (taskId * 100)),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                };
                retryHandle.OnConfigurationChanged(newRetryOptions);
            }
        }
        Console.WriteLine($"Task {taskId} completed successfully");
    }));
}

await Task.WhenAll(tasks);

Console.WriteLine($"Final retry attempts: {retryHandle.GetMaxRetryAttempts()}");
Console.WriteLine($"Final retry delay: {retryHandle.GetBaseDelay()}");
Console.WriteLine($"Final timeout: {timeoutHandle.GetTimeout()}");

// Demonstrate pipeline usage with updated configuration
Console.WriteLine("\n=== Testing Pipeline with Updated Configuration ===");

try
{
    var result = await pipeline.ExecuteAsync(async (ct) =>
    {
        Console.WriteLine("Executing operation...");
        // Simulate an operation that might fail
        if (Random.Shared.Next(10) < 7) // 70% chance of failure
        {
            throw new InvalidOperationException("Simulated failure");
        }
        return "Success!";
    });

    Console.WriteLine($"Pipeline result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Pipeline failed: {ex.Message}");
}

Console.WriteLine("\n=== Demo Complete ===");
Console.WriteLine("The ReloadingPolicyHandle allows atomic updates to retry and timeout configuration");
Console.WriteLine("without rebuilding the entire resilience pipeline, improving performance and maintaining state.");

serviceProvider.Dispose();