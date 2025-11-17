using System.Collections.ObjectModel;
using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Core.ViewModels;

public partial class DevicesViewModel
{
    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public DevicesViewModel()
    {
        Devices.Add(new DeviceInfo { Name = "安卓设备-1" });
        Devices.Add(new DeviceInfo { Name = "安卓设备-2" });
    }
}
