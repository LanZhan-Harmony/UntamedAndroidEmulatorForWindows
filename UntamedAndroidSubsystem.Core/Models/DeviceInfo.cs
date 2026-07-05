using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UntamedAndroidSubsystem.Core.Models;

public class DeviceInfo : ObservableObject
{
    private bool _isStarted;
    private string _name = "";
    private string _previewImageUri = "";
    private string _status = "";
    private string _detail = "";

    public required AndroidEmulatorInstance Instance { get; init; }

    public required IAsyncRelayCommand<DeviceInfo> StartStopCommand { get; init; }

    public bool IsStarted
    {
        get => _isStarted;
        set => SetProperty(ref _isStarted, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string PreviewImageUri
    {
        get => _previewImageUri;
        set => SetProperty(ref _previewImageUri, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }
}
