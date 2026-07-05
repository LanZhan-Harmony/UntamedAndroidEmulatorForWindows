using System.Text.Json.Serialization;

namespace UntamedAndroidSubsystem.Core.HyperV;

public sealed class HyperVLaunchPlan
{
    public HcsSystemConfiguration HcsSystem { get; set; } = new();

    public HcnNetworkConfiguration HcnNetwork { get; set; } = new();

    public HcnEndpointConfiguration HcnEndpoint { get; set; } = new();
}

public sealed class HcsSystemConfiguration
{
    public string Owner { get; set; } = "";

    public SchemaVersion SchemaVersion { get; set; } = new();

    public VirtualMachineConfiguration VirtualMachine { get; set; } = new();

    public bool ShouldTerminateOnLastHandleClosed { get; set; } = true;
}

public sealed class SchemaVersion
{
    public int Major { get; set; } = 2;

    public int Minor { get; set; } = 3;
}

public sealed class VirtualMachineConfiguration
{
    public bool StopOnReset { get; set; } = true;

    public ChipsetConfiguration Chipset { get; set; } = new();

    public ComputeTopologyConfiguration ComputeTopology { get; set; } = new();

    public DeviceConfiguration Devices { get; set; } = new();
}

public sealed class ChipsetConfiguration
{
    public UefiConfiguration Uefi { get; set; } = new();
}

public sealed class UefiConfiguration
{
    public BootDeviceConfiguration BootThis { get; set; } = new();
}

public sealed class BootDeviceConfiguration
{
    public string DevicePath { get; set; } = HyperVConfigurationDefaults.BootDiskControllerName;

    public int DiskNumber { get; set; } = 3;

    public string DeviceType { get; set; } = "ScsiDrive";
}

public sealed class ComputeTopologyConfiguration
{
    public MemoryConfiguration Memory { get; set; } = new();

    public ProcessorConfiguration Processor { get; set; } = new();
}

public sealed class MemoryConfiguration
{
    public int SizeInMB { get; set; } = 4096;

    public bool AllowOvercommit { get; set; } = true;

    [JsonIgnore]
    public string BackingPageSize { get; set; } = "Small";

    [JsonIgnore]
    public int FaultClusterSizeShift { get; set; } = 4;

    [JsonIgnore]
    public int DirectMapFaultClusterSizeShift { get; set; } = 4;

    [JsonIgnore]
    public bool EnableColdDiscardHint { get; set; } = true;

    [JsonIgnore]
    public bool EnableDeferredCommit { get; set; } = true;

    [JsonIgnore]
    public string HostingProcessNameSuffix { get; set; } = "UntamedAndroid";
}

public sealed class ProcessorConfiguration
{
    public int Count { get; set; } = 4;
}

public sealed class DeviceConfiguration
{
    public Dictionary<string, ComPortConfiguration> ComPorts { get; set; } = [];

    public Dictionary<string, NetworkAdapterConfiguration> NetworkAdapters { get; set; } = [];

    public Plan9Configuration Plan9 { get; set; } = new();

    public Dictionary<string, ScsiControllerConfiguration> Scsi { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, FlexibleIovConfiguration> FlexibleIov { get; set; } = [];
}

public sealed class ComPortConfiguration
{
    public string NamedPipe { get; set; } = "";

    public bool OptimizeForDebugger { get; set; } = true;
}

public sealed class NetworkAdapterConfiguration
{
    public string EndpointId { get; set; } = HyperVConfigurationDefaults.EmptyGuid;

    public string MacAddress { get; set; } = "";
}

public sealed class Plan9Configuration
{
    public List<Plan9ShareConfiguration> Shares { get; set; } = [];
}

public sealed class Plan9ShareConfiguration
{
    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public string AccessName { get; set; } = "";

    public int Flags { get; set; } = 44;

    public int Port { get; set; } = 50000;
}

public sealed class ScsiControllerConfiguration
{
    public Dictionary<string, ScsiDiskAttachmentConfiguration> Attachments { get; set; } = [];
}

public sealed class ScsiDiskAttachmentConfiguration
{
    public string Type { get; set; } = "VirtualDisk";

    public string Path { get; set; } = "";

    public bool ReadOnly { get; set; } = true;

    public string CachingMode { get; set; } = "Cached";
}

public sealed class FlexibleIovConfiguration
{
    public string EmulatorId { get; set; } = HyperVConfigurationDefaults.FlexibleIovDeviceId;

    public string HostingModel { get; set; } = "External";

    public List<string> Configuration { get; set; } = [""];
}

public sealed class HcnNetworkConfiguration
{
    public string ID { get; set; } = "";

    public string Name { get; set; } = HyperVConfigurationDefaults.NetworkName;

    public string Owner { get; set; } = HyperVConfigurationDefaults.NetworkName;

    public string Type { get; set; } = "ICS";

    public int Flags { get; set; } = 11;

    public string DNSServerList { get; set; } = "10.224.112.2,10.248.2.2";

    public int MaxConcurrentEndpoints { get; set; } = 128;

    public List<HcnSubnetConfiguration> Subnets { get; set; } = [];

    public List<HcnMacAddressPoolConfiguration> MacPools { get; set; } = [];

    public List<string> Policies { get; set; } = [];

    public string SwitchName { get; set; } = HyperVConfigurationDefaults.NetworkName;
}

public sealed class HcnSubnetConfiguration
{
    public string AddressPrefix { get; set; } = "10.0.2.0/24";

    public string GatewayAddress { get; set; } = "10.0.2.1";

    public List<string> Policies { get; set; } = [];
}

public sealed class HcnMacAddressPoolConfiguration
{
    public string StartMacAddress { get; set; } = "08-2A-3D-65-C0-00";

    public string EndMacAddress { get; set; } = "08-2A-3D-65-CF-FF";
}

public sealed class HcnEndpointConfiguration
{
    [JsonIgnore]
    public string ID { get; set; } = "";

    public string VirtualNetwork { get; set; } = "";
}
