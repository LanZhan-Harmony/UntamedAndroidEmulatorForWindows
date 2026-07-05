using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using UntamedAndroidSubsystem.Core.Models;
using UntamedAndroidSubsystem.Core.ViewModels;

namespace UntamedAndroidSubsystem.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesViewModel ViewModel { get; } =
        Ioc.Default.GetRequiredService<DevicesViewModel>();
    private AndroidDisplayWindowManager DisplayWindowManager { get; } =
        Ioc.Default.GetRequiredService<AndroidDisplayWindowManager>();

    public DevicesPage()
    {
        InitializeComponent();
    }

    private async void ShowDisplay_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { CommandParameter: DeviceInfo device })
        {
            return;
        }

        if (!device.IsStarted)
        {
            await ViewModel.StartStopDeviceCommand.ExecuteAsync(device);
        }

        if (device.IsStarted)
        {
            DisplayWindowManager.Show(device.Instance);
        }
    }
}
