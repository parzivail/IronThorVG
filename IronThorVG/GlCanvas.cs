using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// OpenGL rasterizer canvas.
/// </summary>
public sealed class GlCanvas : Canvas
{
    /// <inheritdoc cref="ThorVGNative.tvg_glcanvas_create" />
    public GlCanvas()
        : base(ThorVGNative.tvg_glcanvas_create())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_glcanvas_set_target(CanvasHandle, nint, int, uint, uint, Colorspace)" />
    public void SetTarget(nint context, int id, uint width, uint height, Colorspace colorspace)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_glcanvas_set_target(Handle, context, id, width, height, colorspace));
}
