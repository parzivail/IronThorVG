using System.Runtime.InteropServices;
using IronThorVG.Native;

namespace IronThorVG;

/// <summary>
/// Represents the engine version information.
/// </summary>
public readonly record struct EngineVersion(uint Major, uint Minor, uint Micro, string? VersionString);

/// <summary>
/// Static helpers for engine initialization and version queries.
/// </summary>
public static class ThorVG
{
    /// <inheritdoc cref="ThorVGNative.tvg_engine_init(uint)" />
    public static void Initialize(uint threads = 0)
    {
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_engine_init(threads));
    }

    /// <inheritdoc cref="ThorVGNative.tvg_engine_term" />
    public static void Terminate()
    {
        ResultGuard.EnsureSuccess(ThorVGNative.tvg_engine_term());
    }

    /// <inheritdoc cref="ThorVGNative.tvg_engine_version()" />
    public static EngineVersion GetVersion()
    {
        uint major;
        uint minor;
        uint micro;
        nint versionPtr;
        var result = ThorVGNative.tvg_engine_version(out major, out minor, out micro, out versionPtr);
        ResultGuard.EnsureSuccess(result);
        var versionString = versionPtr == nint.Zero ? null : Marshal.PtrToStringUTF8(versionPtr);
        return new EngineVersion(major, minor, micro, versionString);
    }
}
