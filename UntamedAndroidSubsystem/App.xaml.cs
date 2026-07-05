using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.HyperV;
using UntamedAndroidSubsystem.Core.Services;
using UntamedAndroidSubsystem.Core.ViewModels;
using UntamedAndroidSubsystem.Views;

namespace UntamedAndroidSubsystem;

public partial class App : Application
{
    public Window? MainWindow { get; private set; }

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
        var services = new ServiceCollection();
        services.AddSingleton(EmulatorPaths.CreateDefault());
        services.AddSingleton<HcsConfigurationBuilder>();
        services.AddSingleton<IEmulatorInstanceStore, FileSystemEmulatorInstanceStore>();
        services.AddSingleton<IEmulatorRuntimeService, HcsEmulatorRuntimeService>();
        services.AddSingleton<AndroidDisplayWindowManager>();
        services.AddSingleton<DevicesViewModel>();

        Ioc.Default.ConfigureServices(
            services.BuildServiceProvider()
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
