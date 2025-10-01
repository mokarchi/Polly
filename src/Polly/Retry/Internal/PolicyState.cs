#nullable enable
using System;
using System.Collections.Generic;

namespace Polly.Retry.Internal;

/// <summary>
/// Represents the state required for retry policy execution.
/// </summary>
internal readonly struct PolicyState<TResult>
{
    public readonly ExceptionPredicates ExceptionPredicates;
    public readonly ResultPredicates<TResult> ResultPredicates;
    public readonly Action<DelegateResult<TResult>, TimeSpan, int, Context> OnRetry;
    public readonly int PermittedRetryCount;
    public readonly IEnumerable<TimeSpan>? SleepDurationsEnumerable;
    public readonly Func<int, DelegateResult<TResult>, Context, TimeSpan>? SleepDurationProvider;

    public PolicyState(
        ExceptionPredicates exceptionPredicates,
        ResultPredicates<TResult> resultPredicates,
        Action<DelegateResult<TResult>, TimeSpan, int, Context> onRetry,
        int permittedRetryCount,
        IEnumerable<TimeSpan>? sleepDurationsEnumerable,
        Func<int, DelegateResult<TResult>, Context, TimeSpan>? sleepDurationProvider)
    {
        ExceptionPredicates = exceptionPredicates;
        ResultPredicates = resultPredicates;
        OnRetry = onRetry;
        PermittedRetryCount = permittedRetryCount;
        SleepDurationsEnumerable = sleepDurationsEnumerable;
        SleepDurationProvider = sleepDurationProvider;
    }
}