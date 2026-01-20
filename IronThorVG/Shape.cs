using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a vector shape paint object.
/// </summary>
public sealed class Shape : Paint
{
    internal Shape(PaintHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_new" />
    public Shape()
        : base(ThorVGNative.tvg_shape_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_reset(PaintHandle)" />
    public void Reset() => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_reset(Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_move_to(PaintHandle, float, float)" />
    public void MoveTo(float x, float y) => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_move_to(Handle, x, y));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_line_to(PaintHandle, float, float)" />
    public void LineTo(float x, float y) => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_line_to(Handle, x, y));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_cubic_to(PaintHandle, float, float, float, float, float, float)" />
    public void CubicTo(float cx1, float cy1, float cx2, float cy2, float x, float y)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_cubic_to(Handle, cx1, cy1, cx2, cy2, x, y));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_close(PaintHandle)" />
    public void Close() => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_close(Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_append_rect(PaintHandle, float, float, float, float, float, float, bool)" />
    public void AppendRect(float x, float y, float width, float height, float rx, float ry, bool clockwise)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_append_rect(Handle, x, y, width, height, rx, ry, clockwise));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_append_circle(PaintHandle, float, float, float, float, bool)" />
    public void AppendCircle(float cx, float cy, float rx, float ry, bool clockwise)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_append_circle(Handle, cx, cy, rx, ry, clockwise));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_append_path(PaintHandle, in byte, uint, in Point, uint)" />
    public void AppendPath(ReadOnlySpan<byte> commands, ReadOnlySpan<Point> points)
    {
        if (commands.IsEmpty || points.IsEmpty)
        {
            throw new ArgumentException("Commands and points must be non-empty.");
        }

        ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_append_path(Handle, in commands[0], (uint)commands.Length, in points[0], (uint)points.Length));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_path(PaintHandle, out nint, out uint, out nint, out uint)" />
    public (byte[] Commands, Point[] Points) GetPath()
    {
        var result = ThorVGNative.tvg_shape_get_path(Handle, out var cmdsPtr, out var cmdsCnt, out var ptsPtr, out var ptsCnt);
        ResultGuard.EnsureSuccess(result);
        var commands = new byte[cmdsCnt];
        var points = new Point[ptsCnt];

        if (cmdsCnt > 0 && cmdsPtr != nint.Zero)
        {
            System.Runtime.InteropServices.Marshal.Copy(cmdsPtr, commands, 0, commands.Length);
        }

        if (ptsCnt > 0 && ptsPtr != nint.Zero)
        {
            var size = System.Runtime.InteropServices.Marshal.SizeOf<Point>();
            for (var i = 0; i < points.Length; i++)
            {
                var offset = nint.Add(ptsPtr, i * size);
                points[i] = System.Runtime.InteropServices.Marshal.PtrToStructure<Point>(offset);
            }
        }

        return (commands, points);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_width(PaintHandle, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_width(PaintHandle, out float)" />
    public float StrokeWidth
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_width(Handle, out var width);
            ResultGuard.EnsureSuccess(result);
            return width;
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_width(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_color(PaintHandle, byte, byte, byte, byte)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_color(PaintHandle, out byte, out byte, out byte, out byte)" />
    public Color StrokeColor
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_color(Handle, out var r, out var g, out var b, out var a);
            ResultGuard.EnsureSuccess(result);
            return new Color(r, g, b, a);
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_color(Handle, value.R, value.G, value.B, value.A);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_gradient(PaintHandle, GradientHandle)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_gradient(PaintHandle, out GradientHandle)" />
    public Gradient? StrokeGradient
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_gradient(Handle, out var handle);
            ResultGuard.EnsureSuccess(result);
            return Gradient.FromHandle(handle);
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_gradient(Handle, value?.Handle ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_dash(PaintHandle, nint, uint, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_dash(PaintHandle, out nint, out uint, out float)" />
    public StrokeDashPattern StrokeDash
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_dash(Handle, out var patternPtr, out var cnt, out var offset);
            ResultGuard.EnsureSuccess(result);
            if (cnt == 0 || patternPtr == nint.Zero)
            {
                return new StrokeDashPattern(Array.Empty<float>(), offset);
            }

            var dashPattern = new float[cnt];
            System.Runtime.InteropServices.Marshal.Copy(patternPtr, dashPattern, 0, dashPattern.Length);
            return new StrokeDashPattern(dashPattern, offset);
        }
        set
        {
            var dashPattern = value.DashPattern;
            var offset = value.Offset;
            if (dashPattern is null || dashPattern.Length == 0)
            {
                _ = ThorVGNative.tvg_shape_set_stroke_dash(Handle, nint.Zero, 0, offset);
                return;
            }

            unsafe
            {
                fixed (float* ptr = dashPattern)
                {
                    _ = ThorVGNative.tvg_shape_set_stroke_dash(Handle, (nint)ptr, (uint)dashPattern.Length, offset);
                }
            }
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_cap(PaintHandle, StrokeCap)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_cap(PaintHandle, out StrokeCap)" />
    public StrokeCap StrokeCap
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_cap(Handle, out var cap);
            ResultGuard.EnsureSuccess(result);
            return cap;
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_cap(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_join(PaintHandle, StrokeJoin)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_join(PaintHandle, out StrokeJoin)" />
    public StrokeJoin StrokeJoin
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_join(Handle, out var join);
            ResultGuard.EnsureSuccess(result);
            return join;
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_join(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_stroke_miterlimit(PaintHandle, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_stroke_miterlimit(PaintHandle, out float)" />
    public float StrokeMiterLimit
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_stroke_miterlimit(Handle, out var miterLimit);
            ResultGuard.EnsureSuccess(result);
            return miterLimit;
        }
        set => _ = ThorVGNative.tvg_shape_set_stroke_miterlimit(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_trimpath(PaintHandle, float, float, bool)" />
    public void SetTrimPath(float begin, float end, bool simultaneous)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_set_trimpath(Handle, begin, end, simultaneous));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_fill_color(PaintHandle, byte, byte, byte, byte)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_fill_color(PaintHandle, out byte, out byte, out byte, out byte)" />
    public Color FillColor
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_fill_color(Handle, out var r, out var g, out var b, out var a);
            ResultGuard.EnsureSuccess(result);
            return new Color(r, g, b, a);
        }
        set => _ = ThorVGNative.tvg_shape_set_fill_color(Handle, value.R, value.G, value.B, value.A);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_fill_rule(PaintHandle, FillRule)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_fill_rule(PaintHandle, out FillRule)" />
    public FillRule FillRule
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_fill_rule(Handle, out var rule);
            ResultGuard.EnsureSuccess(result);
            return rule;
        }
        set => _ = ThorVGNative.tvg_shape_set_fill_rule(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_paint_order(PaintHandle, bool)" />
    public void SetPaintOrder(bool strokeFirst)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_shape_set_paint_order(Handle, strokeFirst));

    /// <inheritdoc cref="ThorVGNative.tvg_shape_set_gradient(PaintHandle, GradientHandle)" />
    /// <inheritdoc cref="ThorVGNative.tvg_shape_get_gradient(PaintHandle, out GradientHandle)" />
    public Gradient? Gradient
    {
        get
        {
            var result = ThorVGNative.tvg_shape_get_gradient(Handle, out var handle);
            ResultGuard.EnsureSuccess(result);
            return Gradient.FromHandle(handle);
        }
        set => _ = ThorVGNative.tvg_shape_set_gradient(Handle, value?.Handle ?? throw new ArgumentNullException(nameof(value)));
    }
}
