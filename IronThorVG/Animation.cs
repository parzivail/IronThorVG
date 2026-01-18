using System.Runtime.InteropServices;
using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents an animation controller.
/// </summary>
public sealed class Animation : IDisposable
{
    internal AnimationHandle Handle { get; }
    private bool _disposed;

    internal Animation(AnimationHandle handle)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        if (handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create animation.");
        }
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_new" />
    public Animation()
        : this(ThorVGNative.tvg_animation_new())
    {
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_set_frame(AnimationHandle, float)" />
    public void SetFrame(float frame)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_animation_set_frame(Handle, frame));

    /// <inheritdoc cref="ThorVGNative.tvg_animation_get_picture(AnimationHandle)" />
    public Picture? GetPicture()
    {
        var handle = ThorVGNative.tvg_animation_get_picture(Handle);
        return Paint.FromHandle(handle) as Picture;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_get_frame(AnimationHandle, nint)" />
    public float GetFrame()
    {
        float value = 0;
        unsafe
        {
            ResultGuard.EnsureSuccess(ThorVGNative.tvg_animation_get_frame(Handle, (nint)(&value)));
        }
        return value;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_get_total_frame(AnimationHandle, nint)" />
    public float GetTotalFrames()
    {
        float value = 0;
        unsafe
        {
            ResultGuard.EnsureSuccess(ThorVGNative.tvg_animation_get_total_frame(Handle, (nint)(&value)));
        }
        return value;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_get_duration(AnimationHandle, nint)" />
    public float GetDuration()
    {
        float value = 0;
        unsafe
        {
            ResultGuard.EnsureSuccess(ThorVGNative.tvg_animation_get_duration(Handle, (nint)(&value)));
        }
        return value;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_animation_set_segment(AnimationHandle, float, float)" />
    /// <inheritdoc cref="ThorVGNative.tvg_animation_get_segment(AnimationHandle, out float, out float)" />
    public SegmentRange Segment
    {
        get
        {
            float begin;
            float end;
            var result = ThorVGNative.tvg_animation_get_segment(Handle, out begin, out end);
            ResultGuard.EnsureSuccess(result);
            return new SegmentRange(begin, end);
        }
        set => _ = ThorVGNative.tvg_animation_set_segment(Handle, value.Begin, value.End);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_new" />
    public static Animation CreateLottie()
    {
        var handle = ThorVGNative.tvg_lottie_animation_new();
        return new Animation(handle);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_gen_slot(AnimationHandle, string)" />
    public uint GenerateSlot(string slotJson) => ThorVGNative.tvg_lottie_animation_gen_slot(Handle, slotJson);

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_apply_slot(AnimationHandle, uint)" />
    public void ApplySlot(uint id)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_apply_slot(Handle, id));

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_del_slot(AnimationHandle, uint)" />
    public void DeleteSlot(uint id)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_del_slot(Handle, id));

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_set_marker(AnimationHandle, string)" />
    public void SetMarker(string marker)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_set_marker(Handle, marker));

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_get_markers_cnt(AnimationHandle, out uint)" />
    public uint GetMarkerCount()
    {
        uint count;
        var result = ThorVGNative.tvg_lottie_animation_get_markers_cnt(Handle, out count);
        ResultGuard.EnsureSuccess(result);
        return count;
    }

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_get_marker(AnimationHandle, uint, out nint)" />
    public string? GetMarker(uint index)
    {
        nint namePtr;
        var result = ThorVGNative.tvg_lottie_animation_get_marker(Handle, index, out namePtr);
        ResultGuard.EnsureSuccess(result);
        return namePtr == nint.Zero ? null : Marshal.PtrToStringUTF8(namePtr);
    }

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_tween(AnimationHandle, float, float, float)" />
    public void Tween(float from, float to, float progress)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_tween(Handle, from, to, progress));

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_assign(AnimationHandle, string, uint, string, float)" />
    public void Assign(string layer, uint index, string variable, float value)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_assign(Handle, layer, index, variable, value));

    /// <inheritdoc cref="ThorVGNative.tvg_lottie_animation_set_quality(AnimationHandle, byte)" />
    public void SetQuality(byte value)
        => ResultGuard.EnsureSuccess(ThorVGNative.tvg_lottie_animation_set_quality(Handle, value));

    /// <summary>
    /// Releases the animation resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!Handle.IsClosed)
        {
            Handle.Dispose();
        }
    }
}
