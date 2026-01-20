using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents a picture paint object that can load external assets.
/// </summary>
public sealed class Picture : Paint
{
    internal Picture(PaintHandle handle) : base(handle)
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_picture_new" />
    public Picture()
        : base(ThorVGNative.tvg_picture_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_picture_load(PaintHandle, string)" />
    public void Load(string path)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_picture_load(Handle, path));

    /// <inheritdoc cref="ThorVGNative.tvg_picture_load_raw(PaintHandle, nint, uint, uint, Colorspace, bool)" />
    public void LoadRaw(ReadOnlySpan<uint> data, uint width, uint height, Colorspace colorspace, bool copy)
    {
        unsafe
        {
            fixed (uint* ptr = data)
            {
                ResultGuard.EnsureSuccess(ThorVGNative.tvg_picture_load_raw(Handle, (nint)ptr, width, height, colorspace, copy));
            }
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_picture_load_data(PaintHandle, byte[], uint, string, string, bool)" />
    public void LoadData(byte[] data, uint size, string mimeType, string rpath, bool copy)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_picture_load_data(Handle, data, size, mimeType, rpath, copy));

    /// <inheritdoc cref="ThorVGNative.tvg_picture_set_asset_resolver(PaintHandle, nint, nint)" />
    public void SetAssetResolver(nint resolver, nint data)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_picture_set_asset_resolver(Handle, resolver, data));

    /// <inheritdoc cref="ThorVGNative.tvg_picture_set_size(PaintHandle, float, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_picture_get_size(PaintHandle, out float, out float)" />
    public Point Size
    {
        get
        {
            var result = ThorVGNative.tvg_picture_get_size(Handle, out var width, out var height);
            ResultGuard.EnsureSuccess(result);
            return new Point(width, height);
        }
        set => _ = ThorVGNative.tvg_picture_set_size(Handle, value.X, value.Y);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_picture_set_origin(PaintHandle, float, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_picture_get_origin(PaintHandle, out float, out float)" />
    public Point Origin
    {
        get
        {
            var result = ThorVGNative.tvg_picture_get_origin(Handle, out var x, out var y);
            ResultGuard.EnsureSuccess(result);
            return new Point(x, y);
        }
        set => _ = ThorVGNative.tvg_picture_set_origin(Handle, value.X, value.Y);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_picture_get_paint(PaintHandle, uint)" />
    public Paint? GetPaint(uint id)
    {
        var handle = ThorVGNative.tvg_picture_get_paint(Handle, id);
        return FromHandle(handle);
    }
}
