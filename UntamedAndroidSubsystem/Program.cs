using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinRT;

namespace UntamedAndroidSubsystem;

public partial class Program
{
    private static nint redirectEventHandle = nint.Zero;

    [STAThread]
    public static int Main()
    {
        ComWrappersSupport.InitializeComWrappers();
        var isRedirect = DecideRedirection();
        if (!isRedirect)
        {
            Application.Start(
                (p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread()
                    );
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                }
            );
        }
        return 0;
    }

    private static bool DecideRedirection()
    {
        var isRedirect = false;
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var kind = args.Kind;
        var keyInstance = AppInstance.FindOrRegisterForKey("UntamedAndroidSubsystem");
        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
        }
        else
        {
            isRedirect = true;
            RedirectActivationTo(args, keyInstance);
        }
        return isRedirect;
    }

    private static void OnActivated(object? sender, AppActivationArguments e)
    {
        _ = e.Kind;
    }

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "CreateEventW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static partial nint CreateEvent(
        nint lpEventAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
        [MarshalAs(UnmanagedType.Bool)] bool bInitialState,
        string lpName
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetEvent(nint hEvent);

    [LibraryImport("ole32.dll")]
    private static partial uint CoWaitForMultipleObjects(
        uint dwFlags,
        uint dwMilliseconds,
        ulong nHandles,
        [In] nint[] pHandles,
        out uint dwIndex
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    // 在另一个线程上执行重定向，并使用非阻塞等待方法等待重定向完成。
    public static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        redirectEventHandle = CreateEvent(nint.Zero, true, false, null!);
        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            SetEvent(redirectEventHandle);
        });
        uint CWMO_DEFAULT = 0;
        var INFINITE = 0xFFFFFFFF;
        _ = CoWaitForMultipleObjects(
            CWMO_DEFAULT,
            INFINITE,
            1,
            [redirectEventHandle],
            out var handleIndex
        );
        var process = Process.GetProcessById((int)keyInstance.ProcessId);
        SetForegroundWindow(process.MainWindowHandle); // 将窗口移至前台
    }
}
