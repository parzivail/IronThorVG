using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Saves paints or animations to files.
/// </summary>
public sealed class Saver : IDisposable
{
    internal SaverHandle Handle { get; }
    private bool _disposed;

    /// <inheritdoc cref="ThorVGNative.tvg_saver_new" />
    public Saver()
    {
        Handle = ThorVGNative.tvg_saver_new();
        if (Handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create saver.");
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_saver_save_paint(SaverHandle, PaintHandle, string, uint)" />
    public void Save(Paint paint, string path, uint quality)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_saver_save_paint(Handle, paint.Handle, path, quality));

    /// <inheritdoc cref="ThorVGNative.tvg_saver_save_animation(SaverHandle, AnimationHandle, string, uint, uint)" />
    public void Save(Animation animation, string path, uint quality, uint fps)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_saver_save_animation(Handle, animation.Handle, path, quality, fps));

    /// <inheritdoc cref="ThorVGNative.tvg_saver_sync(SaverHandle)" />
    public void Sync() => ResultGuard.EnsureSuccess(ThorVGNative.tvg_saver_sync(Handle));

    /// <summary>
    /// Releases the saver resources.
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
