using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Core.HyperV;

public sealed class HcsConfigurationBuilder
{
    public HyperVLaunchPlan Build(AndroidEmulatorInstance instance)
    {
        var endpointId = Guid.NewGuid();
        var macPool = new HcnMacAddressPoolConfiguration();
        string macAddress = GenerateMacAddress(
            instance.Id,
            macPool.StartMacAddress,
            macPool.EndMacAddress
        );

        return new HyperVLaunchPlan
        {
            HcsSystem = BuildHcsSystem(instance, endpointId, macAddress),
            HcnNetwork = BuildNetwork(macPool),
            HcnEndpoint = BuildEndpoint(endpointId),
        };
    }

    private static HcsSystemConfiguration BuildHcsSystem(
        AndroidEmulatorInstance instance,
        Guid endpointId,
        string macAddress
    )
    {
        return new HcsSystemConfiguration
        {
            Owner = $"UntamedAndroid-vm{instance.Id}",
            VirtualMachine = new VirtualMachineConfiguration
            {
                ComputeTopology = new ComputeTopologyConfiguration
                {
                    Memory = new MemoryConfiguration { SizeInMB = instance.MemorySizeInMb },
                    Processor = new ProcessorConfiguration { Count = instance.CpuCount },
                },
                Devices = new DeviceConfiguration
                {
                    ComPorts = new Dictionary<string, ComPortConfiguration>
                    {
                        ["0"] = new()
                        {
                            NamedPipe = $@"\\.\pipe\UntamedAndroid_vm{instance.Id}_console",
                        },
                        ["1"] = new()
                        {
                            NamedPipe = $@"\\.\pipe\UntamedAndroid_vm{instance.Id}_kernel",
                        },
                    },
                    NetworkAdapters = new Dictionary<string, NetworkAdapterConfiguration>
                    {
                        [HyperVConfigurationDefaults.NetworkAdapterName] = new()
                        {
                            EndpointId = endpointId.ToString("D"),
                            MacAddress = macAddress,
                        },
                    },
                    Plan9 = new Plan9Configuration
                    {
                        Shares =
                        [
                            CreatePlan9Share(
                                HyperVConfigurationDefaults.DeviceSharedFolderName,
                                instance.SharedFolders.DeviceSharedPath
                            ),
                            CreatePlan9Share(
                                HyperVConfigurationDefaults.GlobalSharedFolderName,
                                instance.SharedFolders.GlobalSharedPath
                            ),
                            CreatePlan9Share(
                                HyperVConfigurationDefaults.PrivateSharedFolderName,
                                instance.SharedFolders.PrivateSharedPath
                            ),
                        ],
                    },
                    Scsi = new Dictionary<string, ScsiControllerConfiguration>
                    {
                        [HyperVConfigurationDefaults.BootDiskControllerName] = new()
                        {
                            Attachments = new Dictionary<string, ScsiDiskAttachmentConfiguration>
                            {
                                ["0"] = new()
                                {
                                    Path = instance.Disks.SystemDiskPath,
                                    ReadOnly = instance.Disks.SystemDiskReadOnly,
                                },
                                ["1"] = new()
                                {
                                    Path = instance.Disks.DataDiskPath,
                                    ReadOnly = false,
                                },
                                ["2"] = new()
                                {
                                    Path = instance.Disks.SwapDiskPath,
                                    ReadOnly = false,
                                },
                                ["3"] = new()
                                {
                                    Path = instance.Disks.BootDiskPath,
                                    ReadOnly = true,
                                },
                            },
                        },
                    },
                },
            },
        };
    }

    private static HcnNetworkConfiguration BuildNetwork(HcnMacAddressPoolConfiguration macPool)
    {
        return new HcnNetworkConfiguration
        {
            ID = HyperVConfigurationDefaults.NetworkId.ToString("D"),
            MacPools = [macPool],
            Subnets = [new HcnSubnetConfiguration()],
        };
    }

    private static HcnEndpointConfiguration BuildEndpoint(Guid endpointId)
    {
        return new HcnEndpointConfiguration
        {
            ID = endpointId.ToString("D"),
            VirtualNetwork = HyperVConfigurationDefaults.NetworkId.ToString("D"),
        };
    }

    private static Plan9ShareConfiguration CreatePlan9Share(string name, string path)
    {
        return new Plan9ShareConfiguration
        {
            Name = name,
            AccessName = name,
            Path = path,
        };
    }

    private static string GenerateMacAddress(int instanceId, string startMacAddress, string endMacAddress)
    {
        long start = Convert.ToInt64(startMacAddress.Replace("-", ""), 16);
        long end = Convert.ToInt64(endMacAddress.Replace("-", ""), 16);
        long value = start + instanceId + 1L;
        if (value >= end)
        {
            value = end - 1L;
        }

        string hex = value.ToString("X12");
        return string.Join("-", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
    }
}
