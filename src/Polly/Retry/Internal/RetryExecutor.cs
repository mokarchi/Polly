#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Polly.Retry.Internal;

/// <summary>
/// Struct-based retry executor that eliminates delegate chain allocations.
/// </summary>
internal readonly struct RetryExecutor
{
    [DebuggerDisableUserUnhandledExceptions]
    public static TResult Execute<TResult>(
        ref Context context,
        in PolicyState<TResult> state,
        Func<Context, CancellationToken, TResult> action,
        CancellationToken cancellationToken)
    {
        int tryCount = 0;
        IEnumerator<TimeSpan>? sleepDurationsEnumerator = state.SleepDurationsEnumerable?.GetEnumerator();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool canRetry;
                DelegateResult<TResult> outcome;

                try
                {
                    TResult result = action(context, cancellationToken);

                    if (!state.ResultPredicates.AnyMatch(result))
                    {
                        return result;
                    }

                    canRetry = tryCount < state.PermittedRetryCount && (sleepDurationsEnumerator == null || sleepDurationsEnumerator.MoveNext());

                    if (!canRetry)
                    {
                        return result;
                    }

                    outcome = new DelegateResult<TResult>(result);
                }
                catch (Exception ex)
                {
                    Exception handledException = state.ExceptionPredicates.FirstMatchOrDefault(ex);
                    if (handledException == null)
                    {
                        throw;
                    }

                    canRetry = tryCount < state.PermittedRetryCount && (sleepDurationsEnumerator == null || sleepDurationsEnumerator.MoveNext());

                    if (!canRetry)
                    {
                        handledException.RethrowWithOriginalStackTraceIfDiffersFrom(ex);
                        throw;
                    }

                    outcome = new DelegateResult<TResult>(handledException);
                }

                if (tryCount < int.MaxValue)
                {
                    tryCount++;
                }

                TimeSpan waitDuration = sleepDurationsEnumerator?.Current ?? (state.SleepDurationProvider?.Invoke(tryCount, outcome, context) ?? TimeSpan.Zero);

                state.OnRetry(outcome, waitDuration, tryCount, context);

                if (waitDuration > TimeSpan.Zero)
                {
                    SystemClock.Sleep(waitDuration, cancellationToken);
                }
            }
        }
        finally
        {
            sleepDurationsEnumerator?.Dispose();
        }
    }
}