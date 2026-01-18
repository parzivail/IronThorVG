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

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add(PaintHandle, PaintHandle)" />
    public void Add(Paint paint)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add(Handle, paint.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_insert(PaintHandle, PaintHandle, PaintHandle)" />
    public void Insert(Paint paint, Paint? before)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_insert(Handle, paint.Handle, before?.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_remove(PaintHandle, PaintHandle)" />
    public void Remove(Paint? paint = null)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_remove(Handle, paint?.Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_clear_effects(PaintHandle)" />
    public void ClearEffects()
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_clear_effects(Handle));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add_effect_gaussian_blur(PaintHandle, double, int, int, int)" />
    public void AddGaussianBlur(double sigma, int direction, int border, int quality)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add_effect_gaussian_blur(Handle, sigma, direction, border, quality));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add_effect_drop_shadow(PaintHandle, int, int, int, int, double, double, double, int)" />
    public void AddDropShadow(int r, int g, int b, int a, double angle, double distance, double sigma, int quality)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add_effect_drop_shadow(Handle, r, g, b, a, angle, distance, sigma, quality));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add_effect_fill(PaintHandle, int, int, int, int)" />
    public void AddFill(int r, int g, int b, int a)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add_effect_fill(Handle, r, g, b, a));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add_effect_tint(PaintHandle, int, int, int, int, int, int, double)" />
    public void AddTint(int blackR, int blackG, int blackB, int whiteR, int whiteG, int whiteB, double intensity)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add_effect_tint(Handle, blackR, blackG, blackB, whiteR, whiteG, whiteB, intensity));

    /// <inheritdoc cref="ThorVGNative.tvg_scene_add_effect_tritone(PaintHandle, int, int, int, int, int, int, int, int, int, int)" />
    public void AddTritone(int shadowR, int shadowG, int shadowB, int midtoneR, int midtoneG, int midtoneB, int highlightR, int highlightG, int highlightB, int blend)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_scene_add_effect_tritone(Handle, shadowR, shadowG, shadowB, midtoneR, midtoneG, midtoneB, highlightR, highlightG, highlightB, blend));
}
