using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Iterates through scene descendants using a callback.
/// </summary>
public sealed class Accessor : IDisposable
{
    internal AccessorHandle Handle { get; }
    private bool _disposed;

    /// <inheritdoc cref="ThorVGNative.tvg_accessor_new" />
    public Accessor()
    {
        Handle = ThorVGNative.tvg_accessor_new();
        if (Handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create accessor.");
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_accessor_set(AccessorHandle, PaintHandle, nint, nint)" />
    public void Set(Paint paint, nint callback, nint data)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_accessor_set(Handle, paint.Handle, callback, data));

    /// <inheritdoc cref="ThorVGNative.tvg_accessor_generate_id(string)" />
    public uint GenerateId(string name) => ThorVGNative.tvg_accessor_generate_id(name);

    /// <summary>
    /// Releases the accessor resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!Handle.IsClosed)
        {
            Handle.Dispose();
        }
    }
}
