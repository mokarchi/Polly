# Adaptive Bulkhead Policy

The Adaptive Bulkhead Policy is an enhancement of Polly's traditional bulkhead isolation pattern that automatically adjusts the maximum parallelization based on execution latency and error rates using an AIMD (Additive Increase/Multiplicative Decrease) algorithm inspired by TCP congestion control.

## Key Features

- **Automatic Capacity Adjustment**: Dynamically increases or decreases the maximum parallelization based on system performance
- **AIMD Algorithm**: Uses Additive Increase/Multiplicative Decrease for stable and responsive adjustments
- **Latency-Based Control**: Monitors average execution latency and reduces capacity when it exceeds thresholds
- **Error Rate Monitoring**: Tracks error rates and decreases capacity during high failure periods
- **Configurable Parameters**: Full control over thresholds, adjustment intervals, and AIMD parameters
- **Real-time Metrics**: Access to current performance metrics and adjustment events

## When to Use Adaptive Bulkhead

The adaptive bulkhead is particularly beneficial in scenarios with:

- **Variable Load Patterns**: When your downstream services experience fluctuating traffic
- **Dynamic Performance**: When service response times vary significantly over time
- **Elastic Infrastructure**: When working with auto-scaling cloud services
- **Mixed Workloads**: When handling requests with different resource requirements
- **Unknown Optimal Capacity**: When the ideal parallelization level is difficult to determine

## Basic Usage

```csharp
// Create adaptive bulkhead with default settings
var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions
{
    InitialMaxParallelization = 10,
    MinMaxParallelization = 2,
    MaxMaxParallelization = 50,
    LatencyThreshold = TimeSpan.FromMilliseconds(100),
    ErrorRateThreshold = 0.1 // 10% error rate threshold
});

// Execute operations - capacity adjusts automatically
var result = policy.Execute(() =>
{
    // Your protected operation
    return CallDownstreamService();
});
```

## Configuration Options

### Core Parameters

- **InitialMaxParallelization**: Starting number of concurrent slots (default: 10)
- **MinMaxParallelization**: Lower bound for capacity adjustments (default: 1)
- **MaxMaxParallelization**: Upper bound for capacity adjustments (default: 1000)
- **MaxQueueingActions**: Maximum number of requests that can queue (default: 0)

### Threshold Settings

- **LatencyThreshold**: Average latency threshold that triggers capacity reduction (default: 100ms)
- **ErrorRateThreshold**: Error rate threshold (0.0-1.0) that triggers capacity reduction (default: 0.1)

### AIMD Parameters

- **AdditiveIncrease**: Amount to add when conditions are good (default: 1)
- **MultiplicativeDecrease**: Factor to multiply by when conditions are poor (default: 0.5)

### Measurement Settings

- **SamplingWindowSize**: Number of recent executions to track (default: 100)
- **AdjustmentInterval**: How often to evaluate and adjust capacity (default: 10 seconds)
- **MinSamplesForAdjustment**: Minimum samples needed before making adjustments (default: 10)

## Advanced Configuration

```csharp
var policy = Policy.AdaptiveBulkhead(options =>
{
    // Capacity bounds
    options.InitialMaxParallelization = 15;
    options.MinMaxParallelization = 5;
    options.MaxMaxParallelization = 100;
    
    // Performance thresholds
    options.LatencyThreshold = TimeSpan.FromMilliseconds(150);
    options.ErrorRateThreshold = 0.15; // 15%
    
    // AIMD behavior
    options.AdditiveIncrease = 2; // Increase by 2 when good
    options.MultiplicativeDecrease = 0.7; // Reduce to 70% when poor
    
    // Measurement settings
    options.SamplingWindowSize = 50;
    options.AdjustmentInterval = TimeSpan.FromSeconds(5);
    options.MinSamplesForAdjustment = 10;
});
```

## Monitoring and Events

```csharp
var policy = Policy.AdaptiveBulkhead(options);

// Subscribe to capacity adjustments
policy.OnParallelizationAdjusted += adjustment =>
{
    Console.WriteLine($"Capacity adjusted to {adjustment.NewMaxParallelization} " +
                     $"(latency: {adjustment.AverageLatency.TotalMilliseconds:F1}ms, " +
                     $"error rate: {adjustment.ErrorRate:P1})");
};

// Monitor current state
var metrics = policy.GetCurrentMetrics();
Console.WriteLine($"Samples: {metrics.SampleCount}, " +
                 $"Avg Latency: {metrics.AverageLatency.TotalMilliseconds:F1}ms, " +
                 $"Error Rate: {metrics.ErrorRate:P1}");

// Check current capacity
Console.WriteLine($"Max Parallelization: {policy.CurrentMaxParallelization}");
Console.WriteLine($"Available Slots: {policy.BulkheadAvailableCount}");
```

## Async Support

```csharp
var policy = Policy.AdaptiveBulkheadAsync(options);

var result = await policy.ExecuteAsync(async () =>
{
    var response = await httpClient.GetAsync("https://api.example.com/data");
    return await response.Content.ReadAsStringAsync();
});
```

## How the AIMD Algorithm Works

The adaptive bulkhead uses an AIMD (Additive Increase/Multiplicative Decrease) algorithm:

### Additive Increase
When conditions are good (latency below threshold AND error rate below threshold):
- **Action**: Add `AdditiveIncrease` slots to the current capacity
- **Rationale**: Gradually probe for higher capacity to maximize throughput

### Multiplicative Decrease
When conditions are poor (latency above threshold OR error rate above threshold):
- **Action**: Multiply current capacity by `MultiplicativeDecrease` factor
- **Rationale**: Quickly back off from overload conditions to restore stability

### Example Behavior
```
Initial: 10 slots
Good conditions: 10 → 12 → 14 → 16 (additive increase)
Poor conditions: 16 → 8 (multiplicative decrease, 0.5 factor)
Good conditions: 8 → 10 → 12 (recovery)
```

## Comparison with Static Bulkhead

| Aspect | Static Bulkhead | Adaptive Bulkhead |
|--------|----------------|-------------------|
| Capacity | Fixed | Dynamic |
| Load Adaptation | Manual adjustment required | Automatic |
| Resource Utilization | May under/over-utilize | Optimized based on conditions |
| Latency Response | No adjustment | Reduces capacity on high latency |
| Error Handling | Fixed isolation | Adjusts based on error rates |
| Configuration Complexity | Simple | More options but sensible defaults |

## Best Practices

1. **Start Conservative**: Begin with lower `InitialMaxParallelization` and let the system scale up
2. **Set Appropriate Bounds**: Choose `MinMaxParallelization` and `MaxMaxParallelization` based on your system's capabilities
3. **Tune Thresholds**: Adjust `LatencyThreshold` and `ErrorRateThreshold` based on your SLA requirements
4. **Monitor Adjustments**: Use the adjustment events to understand system behavior
5. **Consider Queue Size**: Set `MaxQueueingActions` to handle traffic spikes gracefully
6. **Test Under Load**: Validate the adaptive behavior under realistic load conditions

## Limitations

- **Learning Period**: Requires time to collect samples before making adjustments
- **Reactive Nature**: Responds to conditions after they occur, not predictively
- **Single Metric Focus**: Optimizes primarily for latency and error rate, not other metrics
- **Memory Usage**: Maintains a sliding window of execution samples

## Integration with Other Policies

The adaptive bulkhead can be combined with other Polly policies:

```csharp
var pipeline = Policy.WrapAsync(
    Policy.AdaptiveBulkheadAsync(bulkheadOptions),
    Policy.Handle<HttpRequestException>().RetryAsync(3),
    Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
);
```

This provides comprehensive resilience with automatic capacity management, retries, and timeout protection.