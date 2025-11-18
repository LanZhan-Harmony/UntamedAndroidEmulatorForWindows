using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using UntamedAndroidSubsystem.Core.ViewModels;

namespace UntamedAndroidSubsystem.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesViewModel ViewModel { get; private set; } =
        Ioc.Default.GetRequiredService<DevicesViewModel>();

    public DevicesPage()
    {
        InitializeComponent();
    }
}
