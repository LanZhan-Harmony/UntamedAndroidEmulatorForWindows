using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Imaging;
using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Core.ViewModels;

public partial class DevicesViewModel
{
    public ObservableCollection<DeviceInfo> Devices { get; } =
        [
            new DeviceInfo
            {
                Name = "安卓设备-1",
                Preview = new BitmapImage(
                    new Uri("ms-appx:///Assets/Images/device_empty_landscape.png")
                ),
            },
            new DeviceInfo
            {
                Name = "安卓设备-2",
                Preview = new BitmapImage(new Uri("ms-appx:///Assets/Images/device_landscape.png")),
            },
        ];

    public DevicesViewModel() { }
}
