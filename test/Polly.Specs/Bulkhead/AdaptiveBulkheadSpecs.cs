using System.Diagnostics;
using Polly.Bulkhead;
using Polly.Specs.Helpers;

namespace Polly.Specs.Bulkhead;

public class AdaptiveBulkheadSpecs
{
    [Fact]
    public void Should_create_adaptive_bulkhead_with_default_options()
    {
        var options = new AdaptiveBulkheadOptions();
        var policy = Policy.AdaptiveBulkhead(options);

        policy.ShouldNotBeNull();
        policy.CurrentMaxParallelization.ShouldBe(10); // Default initial value
        policy.BulkheadAvailableCount.ShouldBe(10);
    }

    [Fact]
    public void Should_create_adaptive_bulkhead_with_custom_options()
    {
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 5,
            MinMaxParallelization = 2,
            MaxMaxParallelization = 20,
            LatencyThreshold = TimeSpan.FromMilliseconds(50),
            ErrorRateThreshold = 0.05
        };

        var policy = Policy.AdaptiveBulkhead(options);

        policy.CurrentMaxParallelization.ShouldBe(5);
        policy.BulkheadAvailableCount.ShouldBe(5);
    }

    [Fact]
    public void Should_create_adaptive_bulkhead_with_configuration_action()
    {
        var policy = Policy.AdaptiveBulkhead(opts =>
        {
            opts.InitialMaxParallelization = 7;
            opts.LatencyThreshold = TimeSpan.FromMilliseconds(200);
        });

        policy.CurrentMaxParallelization.ShouldBe(7);
        policy.BulkheadAvailableCount.ShouldBe(7);
    }

    [Fact]
    public void Should_validate_options_on_creation()
    {
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 0 // Invalid
        };

        Should.Throw<ArgumentOutOfRangeException>(() => Policy.AdaptiveBulkhead(options));
    }

    [Fact]
    public void Should_execute_action_successfully()
    {
        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions());
        var executed = false;

        var result = policy.Execute(() =>
        {
            executed = true;
            return "success";
        });

        result.ShouldBe("success");
        executed.ShouldBeTrue();
    }

    [Fact]
    public void Should_record_execution_metrics()
    {
        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions());

        policy.Execute(() =>
        {
            Thread.Sleep(10); // Small delay to create measurable latency
            return "success";
        });

        // Give time for metrics to be recorded
        Thread.Sleep(50);

        var metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBe(1);
        metrics.AverageLatency.ShouldBeGreaterThan(TimeSpan.Zero);
        metrics.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_record_error_metrics_on_exception()
    {
        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions());

        Should.Throw<InvalidOperationException>(() =>
        {
            policy.Execute(() =>
            {
                throw new InvalidOperationException("Test exception");
            });
        });

        // Give time for metrics to be recorded
        Thread.Sleep(50);

        var metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBe(1);
        metrics.ErrorRate.ShouldBe(1.0);
    }

    [Fact]
    public void Should_limit_concurrency_based_on_current_max_parallelization()
    {
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 2,
            MaxQueueingActions = 0 // No queuing
        };
        var policy = Policy.AdaptiveBulkhead(options);

        var executing = 0;
        var maxConcurrent = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    policy.Execute(() =>
                    {
                        var current = Interlocked.Increment(ref executing);
                        var currentMax = Math.Max(maxConcurrent, current);
                        Interlocked.Exchange(ref maxConcurrent, currentMax);

                        Thread.Sleep(100); // Hold the slot for a bit

                        Interlocked.Decrement(ref executing);
                    });
                }
                catch (BulkheadRejectedException)
                {
                    // Expected for some tasks
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void Should_raise_rejection_event_when_capacity_exceeded()
    {
        var rejectionCalled = false;
        var options = new AdaptiveBulkheadOptions
        {
            InitialMaxParallelization = 1,
            MaxQueueingActions = 0
        };

        var policy = Policy.AdaptiveBulkhead(options, ctx => rejectionCalled = true);

        var task1 = Task.Run(() =>
        {
            policy.Execute(() => Thread.Sleep(200));
        });

        Thread.Sleep(50); // Ensure first task is executing

        Should.Throw<BulkheadRejectedException>(() =>
        {
            policy.Execute(() => "should be rejected");
        });

        rejectionCalled.ShouldBeTrue();
        task1.Wait();
    }

    [Fact]
    public async Task Should_work_with_async_methods()
    {
        var policy = Policy.AdaptiveBulkheadAsync(new AdaptiveBulkheadOptions());

        var result = await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return "async success";
        });

        result.ShouldBe("async success");
    }

    [Fact]
    public async Task Should_record_metrics_for_async_executions()
    {
        var policy = Policy.AdaptiveBulkheadAsync(new AdaptiveBulkheadOptions());

        await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return "success";
        });

        // Give time for metrics to be recorded
        await Task.Delay(50);

        var metrics = policy.GetCurrentMetrics();
        metrics.SampleCount.ShouldBe(1);
        metrics.AverageLatency.ShouldBeGreaterThan(TimeSpan.Zero);
        metrics.ErrorRate.ShouldBe(0.0);
    }

    [Fact]
    public void Should_dispose_cleanly()
    {
        var policy = Policy.AdaptiveBulkhead(new AdaptiveBulkheadOptions());

        Should.NotThrow(() => policy.Dispose());
    }

    [Fact]
    public void Should_support_generic_results()
    {
        var policy = Policy.AdaptiveBulkhead<int>(new AdaptiveBulkheadOptions());

        var result = policy.Execute(() => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task Should_support_generic_async_results()
    {
        var policy = Policy.AdaptiveBulkheadAsync<string>(new AdaptiveBulkheadOptions());

        var result = await policy.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            return "async result";
        });

        result.ShouldBe("async result");
    }
}