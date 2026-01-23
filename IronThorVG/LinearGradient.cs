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
    public LinearGradientBounds Bounds
    {
        get
        {
            var result = ThorVGNative.tvg_linear_gradient_get(Handle, out var x1, out var y1, out var x2, out var y2);
            ResultGuard.EnsureSuccess(result);
            return new LinearGradientBounds(new Point(x1, y1), new Point(x2, y2));
        }
        set => _ = ThorVGNative.tvg_linear_gradient_set(Handle, value.Start.X, value.Start.Y, value.End.X, value.End.Y);
    }
}
