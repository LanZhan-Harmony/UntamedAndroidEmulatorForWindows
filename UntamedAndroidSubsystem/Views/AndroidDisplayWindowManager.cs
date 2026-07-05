using System.Collections.Generic;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Views;

public sealed class AndroidDisplayWindowManager
{
    private readonly EmulatorPaths _paths;
    private readonly Dictionary<int, AndroidDisplayWindow> _windows = [];

    public AndroidDisplayWindowManager(EmulatorPaths paths)
    {
        _paths = paths;
    }

    public void Show(AndroidEmulatorInstance instance)
    {
        if (_windows.TryGetValue(instance.Id, out AndroidDisplayWindow? existingWindow))
        {
            existingWindow.Activate();
            return;
        }

        var window = new AndroidDisplayWindow(instance, _paths, OnWindowClosed);
        _windows.Add(instance.Id, window);
        window.Activate();
    }

    private void OnWindowClosed(AndroidDisplayWindow window)
    {
        int matchingId = -1;
        foreach ((int instanceId, AndroidDisplayWindow candidate) in _windows)
        {
            if (ReferenceEquals(candidate, window))
            {
                matchingId = instanceId;
                break;
            }
        }

        if (matchingId >= 0)
        {
            _windows.Remove(matchingId);
        }
    }
}
