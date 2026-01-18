using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a text paint object.
/// </summary>
public sealed class Text : Paint
{
    /// <inheritdoc cref="ThorVGNative.tvg_font_load_data(string, byte[], uint, string, bool)" />
    public static void LoadFont(string name, byte[] data, string mimeType, bool copy)
    {
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_font_load_data(name, data, (uint)data.Length, mimeType, copy));
    }
    
    internal Text(PaintHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_text_new" />
    public Text()
        : base(ThorVGNative.tvg_text_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_font(PaintHandle, string)" />
    public void SetFont(string name)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_font(Handle, name));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_size(PaintHandle, float)" />
    public void SetSize(float size)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_size(Handle, size));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_text(PaintHandle, string)" />
    public void SetText(string text)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_text(Handle, text));

    /// <inheritdoc cref="ThorVGNative.tvg_text_align(PaintHandle, float, float)" />
    public void Align(float x, float y)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_align(Handle, x, y));

    /// <inheritdoc cref="ThorVGNative.tvg_text_layout(PaintHandle, float, float)" />
    public void Layout(float width, float height)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_layout(Handle, width, height));

    /// <inheritdoc cref="ThorVGNative.tvg_text_wrap_mode(PaintHandle, TextWrap)" />
    public void SetWrap(TextWrap mode)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_wrap_mode(Handle, mode));

    /// <inheritdoc cref="ThorVGNative.tvg_text_spacing(PaintHandle, float, float)" />
    public void SetSpacing(float letter, float line)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_spacing(Handle, letter, line));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_italic(PaintHandle, float)" />
    public void SetItalic(float shear)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_italic(Handle, shear));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_outline(PaintHandle, float, byte, byte, byte)" />
    public void SetOutline(float width, byte r, byte g, byte b)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_outline(Handle, width, r, g, b));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_color(PaintHandle, byte, byte, byte)" />
    public void SetColor(byte r, byte g, byte b)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_color(Handle, r, g, b));

    /// <inheritdoc cref="ThorVGNative.tvg_text_set_gradient(PaintHandle, GradientHandle)" />
    public void SetGradient(Gradient gradient)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_text_set_gradient(Handle, gradient.Handle));
}
