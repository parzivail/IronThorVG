using System;
using System.Runtime.InteropServices;
using IronThorVG;

namespace IronThorVG.Native;

/// <summary>
/// SafeHandle for Tvg_Canvas.
/// </summary>
internal sealed class CanvasHandle : SafeHandle
{
    public CanvasHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_canvas_destroy_raw(handle);
        return result == Result.Success;
    }

    internal static CanvasHandle FromRaw(nint raw)
    {
        var handle = new CanvasHandle();
        handle.SetHandle(raw);
        return handle;
    }
}

/// <summary>
/// SafeHandle for Tvg_Paint.
/// </summary>
internal sealed class PaintHandle : SafeHandle
{
    public static readonly PaintHandle Null = new(false);

    private PaintHandle(bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
    {
    }
    
    public PaintHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_paint_unref_raw(handle, free: true);
        return result == Result.Success;
    }

    internal static PaintHandle FromRaw(nint raw)
    {
        var handle = new PaintHandle();
        handle.SetHandle(raw);
        return handle;
    }
}

/// <summary>
/// SafeHandle for Tvg_Gradient.
/// </summary>
internal sealed class GradientHandle : SafeHandle
{
    public GradientHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_gradient_del_raw(handle);
        return result == Result.Success;
    }

    internal static GradientHandle FromRaw(nint raw)
    {
        var handle = new GradientHandle();
        handle.SetHandle(raw);
        return handle;
    }
}

/// <summary>
/// SafeHandle for Tvg_Saver.
/// </summary>
internal sealed class SaverHandle : SafeHandle
{
    public SaverHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_saver_del_raw(handle);
        return result == Result.Success;
    }

    internal static SaverHandle FromRaw(nint raw)
    {
        var handle = new SaverHandle();
        handle.SetHandle(raw);
        return handle;
    }
}

/// <summary>
/// SafeHandle for Tvg_Animation.
/// </summary>
internal sealed class AnimationHandle : SafeHandle
{
    public AnimationHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_animation_del_raw(handle);
        return result == Result.Success;
    }

    internal static AnimationHandle FromRaw(nint raw)
    {
        var handle = new AnimationHandle();
        handle.SetHandle(raw);
        return handle;
    }
}

/// <summary>
/// SafeHandle for Tvg_Accessor.
/// </summary>
internal sealed class AccessorHandle : SafeHandle
{
    public AccessorHandle() : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        var result = ThorVGNative.tvg_accessor_del_raw(handle);
        return result == Result.Success;
    }

    internal static AccessorHandle FromRaw(nint raw)
    {
        var handle = new AccessorHandle();
        handle.SetHandle(raw);
        return handle;
    }
}
