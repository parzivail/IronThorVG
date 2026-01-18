using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// OpenGL rasterizer canvas.
/// </summary>
public sealed class GlCanvas : Canvas
{
    /// <inheritdoc cref="ThorVGNative.tvg_glcanvas_create" />
    public GlCanvas(EngineOptions options = EngineOptions.Default)
        : base(ThorVGNative.tvg_glcanvas_create(options))
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_glcanvas_set_target(CanvasHandle, nint, nint, nint, int, uint, uint, Colorspace)" />
    public void SetTarget(nint display, nint surface, nint context, int id, uint width, uint height, Colorspace colorspace)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_glcanvas_set_target(Handle, display, surface, context, id, width, height, colorspace));
}
