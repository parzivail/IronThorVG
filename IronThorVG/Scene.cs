using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a scene paint that can contain child paints and effects.
/// </summary>
public sealed class Scene : Paint
{
    internal Scene(PaintHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_scene_new" />
    public Scene()
        : base(ThorVGNative.tvg_scene_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push(PaintHandle, PaintHandle)" />
    public void Push(Paint paint)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push(Handle, paint.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_at(PaintHandle, PaintHandle, PaintHandle)" />
    public void PushAt(Paint paint, Paint? before)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_at(Handle, paint.Handle, before?.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_remove(PaintHandle, PaintHandle)" />
    public void Remove(Paint? paint = null)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_remove(Handle, paint?.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_reset_effects(PaintHandle)" />
    public void ResetEffects()
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_reset_effects(Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_effect_gaussian_blur(PaintHandle, double, int, int, int)" />
    public void PushGaussianBlur(double sigma, int direction, int border, int quality)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_effect_gaussian_blur(Handle, sigma, direction, border, quality));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_effect_drop_shadow(PaintHandle, int, int, int, int, double, double, double, int)" />
    public void PushDropShadow(int r, int g, int b, int a, double angle, double distance, double sigma, int quality)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_effect_drop_shadow(Handle, r, g, b, a, angle, distance, sigma, quality));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_effect_fill(PaintHandle, int, int, int, int)" />
    public void PushFill(int r, int g, int b, int a)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_effect_fill(Handle, r, g, b, a));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_effect_tint(PaintHandle, int, int, int, int, int, int, double)" />
    public void PushTint(int blackR, int blackG, int blackB, int whiteR, int whiteG, int whiteB, double intensity)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_effect_tint(Handle, blackR, blackG, blackB, whiteR, whiteG, whiteB, intensity));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_push_effect_tritone(PaintHandle, int, int, int, int, int, int, int, int, int, int)" />
    public void PushTritone(int shadowR, int shadowG, int shadowB, int midtoneR, int midtoneG, int midtoneB, int highlightR, int highlightG, int highlightB, int blend)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_push_effect_tritone(Handle, shadowR, shadowG, shadowB, midtoneR, midtoneG, midtoneB, highlightR, highlightG, highlightB, blend));
}
