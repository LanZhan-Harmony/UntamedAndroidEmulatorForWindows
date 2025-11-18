using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UntamedAndroidSubsystem.Core.ViewModels;
using WinUIEx;

namespace UntamedAndroidSubsystem;

public partial class App : Application
{
    public WindowEx? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        ConfigureServices();
        UnhandledException += App_UnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private static void ConfigureServices()
    {
        Ioc.Default.ConfigureServices(
            new ServiceCollection().AddTransient<DevicesViewModel>().BuildServiceProvider()
        );
    }

    private void App_UnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        e.Handled = true;
    }
}
