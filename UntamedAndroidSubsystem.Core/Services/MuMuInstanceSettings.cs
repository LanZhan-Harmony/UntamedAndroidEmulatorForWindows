namespace UntamedAndroidSubsystem.Core.Services;

public sealed class MuMuInstanceSettings
{
    public string? VmName { get; set; }

    public PhoneProperties? PhoneProp { get; set; }

    public string? PhoneIMEI { get; set; }

    public GpuProperties? GpuProp { get; set; }

    public PerformanceSettings? Custom { get; set; }

    public PerformanceSettings? PerformancePreset { get; set; }

    public bool SystemWritable { get; set; }

    public int FramebufferWidth { get; set; }

    public int FramebufferHeight { get; set; }

    public int FramebufferDPI { get; set; }

    public string? DeviceOrientation { get; set; }

    public string? DisplayCutout { get; set; }
}

public sealed class PerformanceSettings
{
    public int Cpu { get; set; }

    public int MemoryOfMB { get; set; }
}

public sealed class PhoneProperties
{
    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string? Miit { get; set; }
}

public sealed class GpuProperties
{
    public string? Model { get; set; }

    public string? Mode { get; set; }
}
