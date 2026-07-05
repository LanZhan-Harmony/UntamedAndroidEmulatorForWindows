namespace UntamedAndroidSubsystem.Core.Models;

public sealed class AndroidEmulatorInstance
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string InstanceDirectory { get; init; }

    public required string BaseDirectory { get; init; }

    public int CpuCount { get; init; }

    public int MemorySizeInMb { get; init; }

    public int FramebufferWidth { get; init; }

    public int FramebufferHeight { get; init; }

    public int FramebufferDpi { get; init; } = 360;

    public int InitialRotation { get; init; }

    public string PhoneBrand { get; init; } = "HUAWEI";

    public string PhoneModel { get; init; } = "Mate 60 Pro";

    public string PhoneMiit { get; init; } = "ALN-AL00";

    public string PhoneImei { get; init; } = "";

    public string GpuName { get; init; } = "Adreno (TM) 740";

    public int DisplayCutout { get; init; }

    public bool IsSystemWritable { get; init; }

    public required EmulatorDiskLayout Disks { get; init; }

    public required SharedFolderLayout SharedFolders { get; init; }

    public string PreviewImagePath { get; init; } = "";
}
