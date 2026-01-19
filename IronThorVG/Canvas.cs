using System;
using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Base class for canvas implementations.
/// </summary>
public abstract class Canvas : IDisposable
{
    internal CanvasHandle Handle { get; }
    private bool _disposed;

    /// <summary>
    /// Initializes a canvas from a native handle.
    /// </summary>
    internal Canvas(CanvasHandle handle)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        if (handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create canvas.");
        }
    }

    /// <summary>
    /// Gets whether the canvas has been disposed.
    /// </summary>
    public bool IsDisposed => Handle.IsClosed || Handle.IsInvalid;

    /// <summary>
    /// Releases the canvas resources.
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

    /// <summary>
    /// Throws if the canvas has been disposed.
    /// </summary>
    protected void EnsureNotDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Creates a software canvas with the given engine options.
    /// </summary>
    public static SwCanvas CreateSoftware(EngineOptions options = EngineOptions.Default)
    {
        return new SwCanvas(options);
    }

    /// <summary>
    /// Creates an OpenGL canvas.
    /// </summary>
    public static GlCanvas CreateOpenGl(EngineOptions options = EngineOptions.Default)
    {
        return new GlCanvas(options);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_add(CanvasHandle, PaintHandle)" />
    public void Add(Paint paint)
    {
        EnsureNotDisposed();
        if (paint is null)
        {
            throw new ArgumentNullException(nameof(paint));
        }

        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_add(Handle, paint.Handle));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_insert(CanvasHandle, PaintHandle, PaintHandle)" />
    public void Insert(Paint paint, Paint? before)
    {
        EnsureNotDisposed();
        if (paint is null)
        {
            throw new ArgumentNullException(nameof(paint));
        }

        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_insert(Handle, paint.Handle, before?.Handle ?? PaintHandle.Null));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_remove(CanvasHandle, PaintHandle)" />
    public void Remove(Paint? paint = null)
    {
        EnsureNotDisposed();
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_remove(Handle, paint?.Handle ?? PaintHandle.Null));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_update(CanvasHandle)" />
    public void Update()
    {
        EnsureNotDisposed();
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_update(Handle));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_draw(CanvasHandle, bool)" />
    public void Draw(bool clear = true)
    {
        EnsureNotDisposed();
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_draw(Handle, clear));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_sync(CanvasHandle)" />
    public void Sync()
    {
        EnsureNotDisposed();
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_sync(Handle));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_canvas_set_viewport(CanvasHandle, int, int, int, int)" />
    public void SetViewport(int x, int y, int width, int height)
    {
        EnsureNotDisposed();
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_canvas_set_viewport(Handle, x, y, width, height));
    }
}
