using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UntamedAndroidSubsystem.Core.Models;
using UntamedAndroidSubsystem.Core.Services;

namespace UntamedAndroidSubsystem.Core.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    private const string EmptyPreviewUri = "ms-appx:///Assets/Images/device_empty_landscape.png";
    private readonly IEmulatorInstanceStore _instanceStore;
    private readonly IEmulatorRuntimeService _runtimeService;

    public DevicesViewModel(
        IEmulatorInstanceStore instanceStore,
        IEmulatorRuntimeService runtimeService
    )
    {
        _instanceStore = instanceStore;
        _runtimeService = runtimeService;
        RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
        StartStopDeviceCommand = new AsyncRelayCommand<DeviceInfo>(StartStopDeviceAsync);

        _ = RefreshDevicesCommand.ExecuteAsync(null);
    }

    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public IAsyncRelayCommand RefreshDevicesCommand { get; }

    public IAsyncRelayCommand<DeviceInfo> StartStopDeviceCommand { get; }

    private async Task RefreshDevicesAsync()
    {
        IReadOnlyList<AndroidEmulatorInstance> instances =
            await _instanceStore.GetInstancesAsync();

        Devices.Clear();
        foreach (AndroidEmulatorInstance instance in instances)
        {
            Devices.Add(CreateDeviceInfo(instance));
        }
    }

    private async Task StartStopDeviceAsync(DeviceInfo? device)
    {
        if (device is null)
        {
            return;
        }

        try
        {
            if (device.IsStarted)
            {
                device.Status = "停止中";
                await _runtimeService.StopAsync(device.Instance);
                UpdateRuntimeState(device);
                return;
            }

            device.Status = "启动中";
            await _runtimeService.StartAsync(device.Instance);
            UpdateRuntimeState(device);
        }
        catch (Exception ex)
        {
            device.IsStarted = _runtimeService.IsRunning(device.Instance);
            device.Status = "错误";
            device.Detail = ex.Message;
        }
    }

    private DeviceInfo CreateDeviceInfo(AndroidEmulatorInstance instance)
    {
        var device = new DeviceInfo
        {
            Instance = instance,
            StartStopCommand = StartStopDeviceCommand,
            Name = instance.Name,
            PreviewImageUri = ToImageUri(instance.PreviewImagePath),
            Detail =
                $"{instance.CpuCount} vCPU / {instance.MemorySizeInMb} MB / "
                + $"{instance.FramebufferWidth}x{instance.FramebufferHeight}",
        };
        UpdateRuntimeState(device);
        return device;
    }

    private void UpdateRuntimeState(DeviceInfo device)
    {
        device.IsStarted = _runtimeService.IsRunning(device.Instance);
        device.Status = device.IsStarted ? "运行中" : "已停止";
    }

    private static string ToImageUri(string previewImagePath)
    {
        return string.IsNullOrWhiteSpace(previewImagePath) || !File.Exists(previewImagePath)
            ? EmptyPreviewUri
            : new Uri(previewImagePath).AbsoluteUri;
    }
}
