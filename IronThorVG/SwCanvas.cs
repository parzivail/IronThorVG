using System;
using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Software rasterizer canvas.
/// </summary>
public sealed class SwCanvas : Canvas
{
    /// <inheritdoc cref="ThorVGNative.tvg_swcanvas_create(EngineOptions)" />
    public SwCanvas(EngineOptions options = EngineOptions.Default)
        : base(ThorVGNative.tvg_swcanvas_create(options))
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_swcanvas_set_target(CanvasHandle, nint, uint, uint, uint, Colorspace)" />
    public unsafe void SetTarget(Span<Color> buffer, uint stride, uint width, uint height, Colorspace colorspace)
    {
        EnsureNotDisposed();
        var requiredLength = checked((int)(stride * height));
        if (buffer.Length < requiredLength)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer is smaller than stride * height.");
        }

        fixed (Color* ptr = buffer)
        {
            var result = ThorVGNative.tvg_swcanvas_set_target(Handle, (nint)ptr, stride, width, height, colorspace);
            ResultGuard.EnsureSuccess(result);
        }
    }
}
