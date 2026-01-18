using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a radial gradient.
/// </summary>
public sealed class RadialGradient : Gradient
{
    internal RadialGradient(GradientHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_radial_gradient_new" />
    public RadialGradient()
        : base(ThorVGNative.tvg_radial_gradient_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_radial_gradient_set(GradientHandle, float, float, float, float, float, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_radial_gradient_get(GradientHandle, out float, out float, out float, out float, out float, out float)" />
    public RadialGradientParameters Parameters
    {
        get
        {
            float cx;
            float cy;
            float r;
            float fx;
            float fy;
            float fr;
            var result = ThorVGNative.tvg_radial_gradient_get(Handle, out cx, out cy, out r, out fx, out fy, out fr);
            ResultGuard.EnsureSuccess(result);
            return new RadialGradientParameters(cx, cy, r, fx, fy, fr);
        }
        set => _ = ThorVGNative.tvg_radial_gradient_set(Handle, value.Cx, value.Cy, value.R, value.Fx, value.Fy, value.Fr);
    }
}
