using System;
using System.Runtime.InteropServices;

namespace Avalonia.Skia;

/// <summary>
/// Applies the i.MX8MM profile before Avalonia initializes its rendering subsystem.
/// Runtime compilation and ThreadPool settings belong in the executable's runtimeconfig.
/// </summary>
internal static class BilanciaiSoftwareProfile
{
    /// <summary>
    /// Selects the qualified three-worker, four-band, 24 Hz profile.
    /// Explicit environment overrides are preserved for controlled experiments.
    /// Call once during startup, before creating any Avalonia resources or windows.
    /// </summary>
    public static void Configure()
    {
        SetDefault("AVALONIA_MASK_SHADOWS", "1");
        SetDefault("AVALONIA_STROKE_CACHE", "1");
        SetDefault("AVALONIA_RASTER_WORKERS", "3");
        SetDefault("AVALONIA_RASTER_TILES", "4");
        SetDefault("AVALONIA_RASTER_AXIS", "horizontal");
        SetDefault("AVALONIA_RENDER_FPS", "24");
        Console.WriteLine("BILANCIAI SOFTWARE RENDERER");
    }

    private static void SetDefault(string name, string value)
    {
        if (Environment.GetEnvironmentVariable(name) == null)
            Environment.SetEnvironmentVariable(name, value);
    }
}
