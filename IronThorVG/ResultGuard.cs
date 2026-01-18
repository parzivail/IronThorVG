namespace IronThorVG;

/// <summary>
/// Throws exceptions based on ThorVG result codes.
/// </summary>
public static class ResultGuard
{
    /// <summary>
    /// Throws an exception if the result indicates failure.
    /// </summary>
    public static void EnsureSuccess(Result result, string? message = null)
    {
        if (result == Result.Success)
        {
            return;
        }

        var detail = message ?? $"ThorVG operation failed with {result}.";
        throw result switch
        {
            Result.InvalidArgument => new ArgumentException(detail),
            Result.InsufficientCondition => new InvalidOperationException(detail),
            Result.FailedAllocation => new OutOfMemoryException(detail),
            Result.MemoryCorruption => new AccessViolationException(detail),
            Result.NotSupported => new NotSupportedException(detail),
            Result.Unknown => new InvalidOperationException(detail),
            _ => new InvalidOperationException(detail),
        };
    }
}
