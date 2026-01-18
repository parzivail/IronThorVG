using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Base class for paint objects that can be pushed to a canvas or scene.
/// </summary>
public abstract class Paint : IDisposable
{
    internal PaintHandle Handle { get; }
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance from a native handle.
    /// </summary>
    internal Paint(PaintHandle handle)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        if (handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create paint object.");
        }
    }

    /// <summary>
    /// Gets whether the paint has been disposed.
    /// </summary>
    public bool IsDisposed => Handle.IsClosed || Handle.IsInvalid;

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_visible" />
    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_visible" />
    public bool Visible
    {
        get => ThorVGNative.tvg_paint_get_visible(Handle);
        set => _ = ThorVGNative.tvg_paint_set_visible(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_ref(PaintHandle)" />
    public ushort AddRef() => ThorVGNative.tvg_paint_ref(Handle);

    /// <inheritdoc cref="ThorVGNative.tvg_paint_unref(PaintHandle, bool)" />
    public ushort ReleaseRef(bool free = false) => ThorVGNative.tvg_paint_unref(Handle, free);

    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_ref(PaintHandle)" />
    public ushort GetRefCount() => ThorVGNative.tvg_paint_get_ref(Handle);

    /// <inheritdoc cref="ThorVGNative.tvg_paint_scale(PaintHandle, float)" />
    public void Scale(float factor)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_paint_scale(Handle, factor));

    /// <inheritdoc cref="ThorVGNative.tvg_paint_rotate(PaintHandle, float)" />
    public void Rotate(float degree)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_paint_rotate(Handle, degree));

    /// <inheritdoc cref="ThorVGNative.tvg_paint_translate(PaintHandle, float, float)" />
    public void Translate(float x, float y)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_paint_translate(Handle, x, y));

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_transform(PaintHandle, in Matrix)" />
    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_transform(PaintHandle, out Matrix)" />
    public Matrix Transform
    {
        get
        {
            Matrix matrix;
            var result = ThorVGNative.tvg_paint_get_transform(Handle, out matrix);
            ResultGuard.EnsureSuccess(result);
            return matrix;
        }
        set => _ = ThorVGNative.tvg_paint_set_transform(Handle, in value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_opacity(PaintHandle, byte)" />
    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_opacity(PaintHandle, out byte)" />
    public byte Opacity
    {
        get
        {
            byte opacity;
            var result = ThorVGNative.tvg_paint_get_opacity(Handle, out opacity);
            ResultGuard.EnsureSuccess(result);
            return opacity;
        }
        set => _ = ThorVGNative.tvg_paint_set_opacity(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_duplicate(PaintHandle)" />
    public Paint? Duplicate()
    {
        var handle = ThorVGNative.tvg_paint_duplicate(Handle);
        return FromHandle(handle);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_intersects(PaintHandle, int, int, int, int)" />
    public bool Intersects(int x, int y, int width, int height) => ThorVGNative.tvg_paint_intersects(Handle, x, y, width, height);

    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_aabb(PaintHandle, out float, out float, out float, out float)" />
    public RectF AxisAlignedBoundingBox
    {
        get
        {
            float x;
            float y;
            float width;
            float height;
            var result = ThorVGNative.tvg_paint_get_aabb(Handle, out x, out y, out width, out height);
            ResultGuard.EnsureSuccess(result);
            return new RectF(x, y, width, height);
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_obb(PaintHandle, out Point)" />
    public Point GetOrientedBoundingBox()
    {
        Point point;
        var result = ThorVGNative.tvg_paint_get_obb(Handle, out point);
        ResultGuard.EnsureSuccess(result);
        return point;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_mask_method(PaintHandle,PaintHandle,IronThorVG.MaskMethod)" />
    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_mask_method(PaintHandle,PaintHandle,IronThorVG.MaskMethod)" />
    public MaskMethodState MaskMethod
    {
        get
        {
            PaintHandle targetHandle;
            MaskMethod method;
            var result = ThorVGNative.tvg_paint_get_mask_method(Handle, out targetHandle, out method);
            ResultGuard.EnsureSuccess(result);
            var target = FromHandle(targetHandle);
            return new MaskMethodState(target, method);
        }
        set
        {
            var target = value.Target ?? throw new ArgumentNullException(nameof(value.Target));
            _ = ThorVGNative.tvg_paint_set_mask_method(Handle, target.Handle, value.Method);
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_clip(PaintHandle, PaintHandle)" />
    public void SetClip(Paint clipper)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_paint_set_clip(Handle, clipper.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_clip(PaintHandle)" />
    public Paint? GetClip()
    {
        var handle = ThorVGNative.tvg_paint_get_clip(Handle);
        return FromHandle(handle);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_get_parent(PaintHandle)" />
    public Paint? GetParent()
    {
        var handle = ThorVGNative.tvg_paint_get_parent(Handle);
        return FromHandle(handle);
    }

    /// <summary>
    /// Gets the paint type reported by the native API.
    /// </summary>
    public Type PaintType
    {
        get
        {
            Type type;
            var result = ThorVGNative.tvg_paint_get_type(Handle, out type);
            ResultGuard.EnsureSuccess(result);
            return type;
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_paint_set_blend_method(PaintHandle, BlendMethod)" />
    public void SetBlendMethod(BlendMethod method)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_paint_set_blend_method(Handle, method));

    /// <summary>
    /// Releases the native paint handle.
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

    internal static Paint? FromHandle(PaintHandle handle)
    {
        if (handle is null || handle.IsInvalid)
        {
            return null;
        }

        Type type;
        var result = ThorVGNative.tvg_paint_get_type(handle, out type);
        if (result != Result.Success)
        {
            return new RawPaint(handle);
        }

        return type switch
        {
            Type.Shape => new Shape(handle),
            Type.Picture => new Picture(handle),
            Type.Scene => new Scene(handle),
            Type.Text => new Text(handle),
            _ => new RawPaint(handle),
        };
    }

    private sealed class RawPaint : Paint
    {
        internal RawPaint(PaintHandle handle) : base(handle)
        {
        }
    }
}
