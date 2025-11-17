using Microsoft.UI.Xaml.Media.Imaging;

namespace UntamedAndroidSubsystem.Core.Models;

public class DeviceInfo
{
    public bool IsStarted { get; set; }
    public string Name { get; set; }
    public BitmapImage Preview { get; set; }
}
