using System;
using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Base class for gradient resources.
/// </summary>
public abstract class Gradient : IDisposable
{
    internal GradientHandle Handle { get; }
    private bool _disposed;

    internal Gradient(GradientHandle handle)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        if (handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create gradient.");
        }
    }

    /// <summary>
    /// Gets whether the gradient has been disposed.
    /// </summary>
    public bool IsDisposed => Handle.IsClosed || Handle.IsInvalid;

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_set_color_stops(GradientHandle, in ColorStop, uint)" />
    public void SetColorStops(ReadOnlySpan<ColorStop> stops)
    {
        if (stops.IsEmpty)
        {
            throw new ArgumentException("Color stops must be non-empty.", nameof(stops));
        }

        ResultGuard.EnsureSuccess(ThorVGNative.tvg_gradient_set_color_stops(Handle, in stops[0], (uint)stops.Length));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_set_color_stops(GradientHandle, in ColorStop, uint)" />
    /// <inheritdoc cref="ThorVGNative.tvg_gradient_get_color_stops(GradientHandle, out nint, out uint)" />
    public ColorStop[] ColorStops
    {
        get
        {
            nint ptr;
            uint cnt;
            var result = ThorVGNative.tvg_gradient_get_color_stops(Handle, out ptr, out cnt);
            ResultGuard.EnsureSuccess(result);
            if (cnt == 0 || ptr == nint.Zero)
            {
                return Array.Empty<ColorStop>();
            }

            var stops = new ColorStop[cnt];
            var size = System.Runtime.InteropServices.Marshal.SizeOf<ColorStop>();
            for (var i = 0; i < stops.Length; i++)
            {
                var offset = nint.Add(ptr, i * size);
                stops[i] = System.Runtime.InteropServices.Marshal.PtrToStructure<ColorStop>(offset);
            }

            return stops;
        }
        set => SetColorStops(value ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_set_spread(GradientHandle, StrokeFill)" />
    /// <inheritdoc cref="ThorVGNative.tvg_gradient_get_spread(GradientHandle, out StrokeFill)" />
    public StrokeFill Spread
    {
        get
        {
            StrokeFill spread;
            var result = ThorVGNative.tvg_gradient_get_spread(Handle, out spread);
            ResultGuard.EnsureSuccess(result);
            return spread;
        }
        set => _ = ThorVGNative.tvg_gradient_set_spread(Handle, value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_set_transform(GradientHandle, in Matrix)" />
    /// <inheritdoc cref="ThorVGNative.tvg_gradient_get_transform(GradientHandle, out Matrix)" />
    public Matrix Transform
    {
        get
        {
            Matrix matrix;
            var result = ThorVGNative.tvg_gradient_get_transform(Handle, out matrix);
            ResultGuard.EnsureSuccess(result);
            return matrix;
        }
        set => _ = ThorVGNative.tvg_gradient_set_transform(Handle, in value);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_get_type(GradientHandle, out Type)" />
    public Type GradientType
    {
        get
        {
            Type type;
            var result = ThorVGNative.tvg_gradient_get_type(Handle, out type);
            ResultGuard.EnsureSuccess(result);
            return type;
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_gradient_duplicate(GradientHandle)" />
    public Gradient? Duplicate()
    {
        var handle = ThorVGNative.tvg_gradient_duplicate(Handle);
        return FromHandle(handle);
    }

    /// <summary>
    /// Releases the gradient resources.
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

    internal static Gradient? FromHandle(GradientHandle handle)
    {
        if (handle is null || handle.IsInvalid)
        {
            return null;
        }

        Type type;
        var result = ThorVGNative.tvg_gradient_get_type(handle, out type);
        if (result != Result.Success)
        {
            return new RawGradient(handle);
        }

        return type switch
        {
            Type.LinearGrad => new LinearGradient(handle),
            Type.RadialGrad => new RadialGradient(handle),
            _ => new RawGradient(handle),
        };
    }

    private sealed class RawGradient : Gradient
    {
        internal RawGradient(GradientHandle handle) : base(handle)
        {
        }
    }
}
