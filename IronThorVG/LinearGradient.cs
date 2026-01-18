using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a linear gradient.
/// </summary>
public sealed class LinearGradient : Gradient
{
    internal LinearGradient(GradientHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_linear_gradient_new" />
    public LinearGradient()
        : base(ThorVGNative.tvg_linear_gradient_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_linear_gradient_set(GradientHandle, float, float, float, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_linear_gradient_get(GradientHandle, out float, out float, out float, out float)" />
    public (float X1, float Y1, float X2, float Y2) Bounds
    {
        get
        {
            float x1;
            float y1;
            float x2;
            float y2;
            var result = ThorVGNative.tvg_linear_gradient_get(Handle, out x1, out y1, out x2, out y2);
            ResultGuard.EnsureSuccess(result);
            return (x1, y1, x2, y2);
        }
        set => _ = ThorVGNative.tvg_linear_gradient_set(Handle, value.X1, value.Y1, value.X2, value.Y2);
    }
}
