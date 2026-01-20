using System.Runtime.InteropServices;

namespace IronThorVG;

/// <summary>
/// Represents an RGBA color.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Color(byte R, byte G, byte B, byte A);

/// <summary>
/// Represents a stroke dash pattern.
/// </summary>
public readonly record struct StrokeDashPattern(float[] DashPattern, float Offset);

/// <summary>
/// Represents a segment range with a beginning and end value.
/// </summary>
public readonly record struct SegmentRange(float Begin, float End);

/// <summary>
/// Parameters describing a radial gradient.
/// </summary>
public readonly record struct RadialGradientParameters(float Cx, float Cy, float R, float Fx, float Fy, float Fr);

/// <summary>
/// Mask method metadata returned by native API.
/// </summary>
public readonly record struct MaskMethodState(Paint? Target, MaskMethod Method);
