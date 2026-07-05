using System.Text.Json;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.Models;
using UntamedAndroidSubsystem.Core.Serialization;

namespace UntamedAndroidSubsystem.Core.Services;

public sealed class FileSystemEmulatorInstanceStore : IEmulatorInstanceStore
{
    private readonly EmulatorPaths _paths;

    public FileSystemEmulatorInstanceStore(EmulatorPaths paths)
    {
        _paths = paths;
    }

    public Task<IReadOnlyList<AndroidEmulatorInstance>> GetInstancesAsync(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(_paths.VmsRoot))
        {
            return Task.FromResult<IReadOnlyList<AndroidEmulatorInstance>>([]);
        }

        var instances = Directory
            .EnumerateDirectories(_paths.VmsRoot, "vm*.madoa")
            .Select(CreateInstance)
            .Where(instance => instance is not null)
            .Cast<AndroidEmulatorInstance>()
            .OrderBy(instance => instance.Id)
            .ToArray();

        return Task.FromResult<IReadOnlyList<AndroidEmulatorInstance>>(instances);
    }

    private AndroidEmulatorInstance? CreateInstance(string instanceDirectory)
    {
        string directoryName = Path.GetFileNameWithoutExtension(instanceDirectory);
        if (
            directoryName.Length < 3
            || !int.TryParse(directoryName.AsSpan(2), out int instanceId)
        )
        {
            return null;
        }

        MuMuInstanceSettings settings = ReadSettings(instanceDirectory);
        string instanceName = string.IsNullOrWhiteSpace(settings.VmName)
            ? $"Android device-{instanceId + 1}"
            : settings.VmName!;

        int cpu = settings.Custom?.Cpu ?? settings.PerformancePreset?.Cpu ?? 4;
        int memory = settings.Custom?.MemoryOfMB ?? settings.PerformancePreset?.MemoryOfMB ?? 4096;
        int framebufferWidth = settings.FramebufferWidth <= 0 ? 2560 : settings.FramebufferWidth;
        int framebufferHeight = settings.FramebufferHeight <= 0 ? 1440 : settings.FramebufferHeight;
        string previewPath = Path.Combine(
            instanceDirectory,
            "private_shared",
            "boot_screenshot.png"
        );

        return new AndroidEmulatorInstance
        {
            Id = instanceId,
            Name = instanceName,
            InstanceDirectory = instanceDirectory,
            BaseDirectory = _paths.BaseVmDirectory,
            CpuCount = cpu,
            MemorySizeInMb = memory,
            FramebufferWidth = framebufferWidth,
            FramebufferHeight = framebufferHeight,
            FramebufferDpi = settings.FramebufferDPI <= 0 ? 360 : settings.FramebufferDPI,
            InitialRotation = ResolveInitialRotation(
                settings.DeviceOrientation,
                framebufferWidth,
                framebufferHeight
            ),
            PhoneBrand = ReadOrDefault(settings.PhoneProp?.Brand, "HUAWEI"),
            PhoneModel = ReadOrDefault(settings.PhoneProp?.Model, "Mate 60 Pro"),
            PhoneMiit = ReadOrDefault(settings.PhoneProp?.Miit, "ALN-AL00"),
            PhoneImei = settings.PhoneIMEI ?? "",
            GpuName = ReadOrDefault(settings.GpuProp?.Model, "Adreno (TM) 740"),
            DisplayCutout = ResolveDisplayCutout(settings.DisplayCutout),
            IsSystemWritable = settings.SystemWritable,
            PreviewImagePath = File.Exists(previewPath) ? previewPath : "",
            Disks = new EmulatorDiskLayout
            {
                SystemDiskPath = ResolveDisk(
                    instanceDirectory,
                    "system.vhdx",
                    settings.SystemWritable
                ),
                DataDiskPath = ResolveDisk(instanceDirectory, "data.vhdx", preferInstance: true),
                SwapDiskPath = ResolveDisk(instanceDirectory, "swap.vhdx", preferInstance: true),
                BootDiskPath = ResolveDisk(instanceDirectory, "boot.vhdx", preferInstance: false),
                SystemDiskReadOnly = !settings.SystemWritable,
            },
            SharedFolders = new SharedFolderLayout
            {
                DeviceSharedPath = _paths.DeviceSharedDirectory,
                GlobalSharedPath = _paths.GlobalSharedDirectory,
                PrivateSharedPath = Path.Combine(instanceDirectory, "private_shared"),
            },
        };
    }

    private MuMuInstanceSettings ReadSettings(string instanceDirectory)
    {
        string settingsPath = Path.Combine(instanceDirectory, "setting.json");
        if (!File.Exists(settingsPath))
        {
            return new MuMuInstanceSettings();
        }

        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize(
                    json,
                    EmulatorJsonSerializerContext.Default.MuMuInstanceSettings
                ) ?? new MuMuInstanceSettings();
        }
        catch (JsonException)
        {
            return new MuMuInstanceSettings();
        }
        catch (IOException)
        {
            return new MuMuInstanceSettings();
        }
    }

    private static int ResolveInitialRotation(
        string? deviceOrientation,
        int framebufferWidth,
        int framebufferHeight
    )
    {
        if (string.Equals(deviceOrientation, "Landscape", StringComparison.OrdinalIgnoreCase))
        {
            return 90;
        }

        if (string.Equals(deviceOrientation, "Portrait", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return framebufferWidth > framebufferHeight ? 90 : 0;
    }

    private static int ResolveDisplayCutout(string? displayCutout)
    {
        return string.Equals(displayCutout, "None", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static string ReadOrDefault(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private string ResolveDisk(string instanceDirectory, string diskName, bool preferInstance)
    {
        string instanceDisk = Path.Combine(instanceDirectory, diskName);
        string baseDisk = Path.Combine(_paths.BaseVmDirectory, diskName);

        if (preferInstance)
        {
            return instanceDisk;
        }

        if (File.Exists(baseDisk))
        {
            return baseDisk;
        }

        return File.Exists(instanceDisk) ? instanceDisk : baseDisk;
    }
}
