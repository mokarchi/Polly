using System.Threading.RateLimiting;

namespace Polly.RateLimiting;

/// <summary>
/// A rate limit lease that manages multiple underlying leases.
/// </summary>
internal sealed class CompositeRateLimitLease : RateLimitLease
{
    private readonly RateLimitLease _tokenBucketLease;
    private readonly RateLimitLease _slidingWindowLease;
    private bool _disposed;

    public CompositeRateLimitLease(RateLimitLease tokenBucketLease, RateLimitLease slidingWindowLease)
    {
        _tokenBucketLease = tokenBucketLease ?? throw new ArgumentNullException(nameof(tokenBucketLease));
        _slidingWindowLease = slidingWindowLease ?? throw new ArgumentNullException(nameof(slidingWindowLease));
    }

    /// <inheritdoc/>
    public override bool IsAcquired => _tokenBucketLease.IsAcquired && _slidingWindowLease.IsAcquired;

    /// <inheritdoc/>
    public override IEnumerable<string> MetadataNames => 
        _tokenBucketLease.MetadataNames.Concat(_slidingWindowLease.MetadataNames).Distinct();

    /// <inheritdoc/>
    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        // Try token bucket first, then sliding window
        if (_tokenBucketLease.TryGetMetadata(metadataName, out metadata))
        {
            return true;
        }

        return _slidingWindowLease.TryGetMetadata(metadataName, out metadata);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _tokenBucketLease?.Dispose();
            _slidingWindowLease?.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}