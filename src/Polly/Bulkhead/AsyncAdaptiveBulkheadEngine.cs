#nullable enable
using System.Diagnostics;

namespace Polly.Bulkhead;

internal static class AsyncAdaptiveBulkheadEngine
{
    internal static async Task<TResult> ImplementationAsync<TResult>(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        Func<Context, Task> onBulkheadRejectedAsync,
        AdaptiveBulkheadSemaphoreManager semaphoreManager,
        AdaptiveBulkheadController controller,
        bool continueOnCapturedContext,
        CancellationToken cancellationToken)
    {
        // Try to acquire queue slot first
        if (!await semaphoreManager.QueueSemaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(continueOnCapturedContext))
        {
            await onBulkheadRejectedAsync(context).ConfigureAwait(continueOnCapturedContext);
            throw new BulkheadRejectedException();
        }

        try
        {
            // Get current parallelization semaphore (may be updated)
            var parallelizationSemaphore = semaphoreManager.GetParallelizationSemaphore();
            
            // Wait for execution slot
            await parallelizationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                bool isError = false;
                TResult result;

                try
                {
                    result = await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
                }
                catch (Exception)
                {
                    isError = true;
                    throw;
                }
                finally
                {
                    stopwatch.Stop();
                    
                    // Record execution metrics for adaptive algorithm
                    controller.RecordExecution(stopwatch.Elapsed, isError);
                }

                return result;
            }
            finally
            {
                parallelizationSemaphore.Release();
            }
        }
        finally
        {
            semaphoreManager.QueueSemaphore.Release();
        }
    }
}