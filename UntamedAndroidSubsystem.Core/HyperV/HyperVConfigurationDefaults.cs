namespace UntamedAndroidSubsystem.Core.HyperV;

public static class HyperVConfigurationDefaults
{
    public const string BootDiskControllerName = "Boot Disk Controller";
    public const string FlexibleIovDeviceId = "bd9b5e82-db25-4dee-b2a7-c10f269b3707";
    public const string DeviceSharedFolderName = "MuMu12Shared";
    public const string GlobalSharedFolderName = "global_shared";
    public const string PrivateSharedFolderName = "private_shared";
    public const string NetworkAdapterName = "default";
    public const string NetworkName = "UntamedAndroidVNet";
    public const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    public static readonly Guid NetworkId = new("3bfa0e34-29da-45ac-874f-07c5ef322129");
}
