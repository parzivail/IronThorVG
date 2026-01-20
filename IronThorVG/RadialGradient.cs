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
            var result = ThorVGNative.tvg_radial_gradient_get(Handle, out var cx, out var cy, out var r, out var fx, out var fy, out var fr);
            ResultGuard.EnsureSuccess(result);
            return new RadialGradientParameters(cx, cy, r, fx, fy, fr);
        }
        set => _ = ThorVGNative.tvg_radial_gradient_set(Handle, value.Cx, value.Cy, value.R, value.Fx, value.Fy, value.Fr);
    }
}
