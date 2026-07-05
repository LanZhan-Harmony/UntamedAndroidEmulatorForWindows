using System;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.Models;
using UntamedAndroidSubsystem.Rendering;
using Windows.Graphics;

namespace UntamedAndroidSubsystem.Views;

internal sealed class AndroidDisplayWindow : Window
{
    private readonly AndroidEmulatorInstance _instance;
    private readonly AndroidRendererSession _rendererSession;
    private readonly RendererHostControl _rendererHost;
    private readonly Action<AndroidDisplayWindow> _closedCallback;
    private bool _startAttempted;

    public AndroidDisplayWindow(
        AndroidEmulatorInstance instance,
        EmulatorPaths paths,
        Action<AndroidDisplayWindow> closedCallback
    )
    {
        _instance = instance;
        _closedCallback = closedCallback;
        _rendererSession = new AndroidRendererSession(instance, paths);
        _rendererSession.StatusChanged += OnRendererStatusChanged;
        _rendererHost = new RendererHostControl(this);
        _rendererHost.HostReady += OnRendererHostReady;

        Title = $"{instance.Name} - Android";
        Content = new Grid
        {
            Background = new SolidColorBrush(Colors.Black),
            Children = { _rendererHost },
        };

        Closed += OnClosed;
        ResizeForInstance(instance);
    }

    private void ResizeForInstance(AndroidEmulatorInstance instance)
    {
        int width;
        int height;
        if (instance.FramebufferWidth > instance.FramebufferHeight)
        {
            width = 960;
            height = 540;
        }
        else
        {
            width = 450;
            height = 800;
        }

        AppWindow.Resize(new SizeInt32(width, height));
    }

    private void OnRendererHostReady(object? sender, RendererHostReadyEventArgs e)
    {
        if (_startAttempted)
        {
            return;
        }

        _startAttempted = true;
        try
        {
            _rendererSession.Start(e.Hwnd, e.RasterizationScale);
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
        }
    }

    private void ShowStartupError(Exception exception)
    {
        _rendererHost.Dispose();
        Content = new Grid
        {
            Padding = new Thickness(24),
            Background = new SolidColorBrush(Colors.Black),
            Children =
            {
                new TextBlock
                {
                    Text = exception.Message,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
        Title = $"{_instance.Name} - renderer 启动失败";
    }

    private void OnRendererStatusChanged(object? sender, string status)
    {
        DispatcherQueue dispatcherQueue = DispatcherQueue;
        if (!dispatcherQueue.HasThreadAccess)
        {
            dispatcherQueue.TryEnqueue(() => SetStatus(status));
            return;
        }

        SetStatus(status);
    }

    private void SetStatus(string status)
    {
        Title = $"{_instance.Name} - {status}";
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _rendererHost.HostReady -= OnRendererHostReady;
        _rendererHost.Dispose();
        _rendererSession.StatusChanged -= OnRendererStatusChanged;
        _rendererSession.Dispose();
        _closedCallback(this);
    }
}
