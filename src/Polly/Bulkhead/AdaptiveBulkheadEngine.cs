#nullable enable
using System.Diagnostics;

namespace Polly.Bulkhead;

internal static class AdaptiveBulkheadEngine
{
    internal static TResult Implementation<TResult>(
        Func<Context, CancellationToken, TResult> action,
        Context context,
        Action<Context> onBulkheadRejected,
        AdaptiveBulkheadSemaphoreManager semaphoreManager,
        AdaptiveBulkheadController controller,
        CancellationToken cancellationToken)
    {
        // Try to acquire queue slot first
        if (!semaphoreManager.QueueSemaphore.Wait(TimeSpan.Zero, cancellationToken))
        {
            onBulkheadRejected(context);
            throw new BulkheadRejectedException();
        }

        try
        {
            // Get current parallelization semaphore (may be updated)
            var parallelizationSemaphore = semaphoreManager.GetParallelizationSemaphore();
            
            // Wait for execution slot
            parallelizationSemaphore.Wait(cancellationToken);
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                bool isError = false;
                TResult result;

                try
                {
                    result = action(context, cancellationToken);
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
                SafeRelease(parallelizationSemaphore);
            }
        }
        finally
        {
            SafeRelease(semaphoreManager.QueueSemaphore);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        static void SafeRelease(SemaphoreSlim semaphore)
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // Ignore - this can happen if the semaphore was disposed during adaptive adjustment
            }
        }
    }
}