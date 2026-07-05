using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Windows.Foundation;

namespace UntamedAndroidSubsystem.Rendering;

internal sealed class RendererHostControl : FrameworkElement, IDisposable
{
    private readonly Window _owner;
    private nint _hwnd;
    private bool _hostReadyRaised;

    public RendererHostControl(Window owner)
    {
        _owner = owner;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    public event EventHandler<RendererHostReadyEventArgs>? HostReady;

    public nint Hwnd => _hwnd;

    public double HostRasterizationScale => XamlRoot?.RasterizationScale ?? 1.0;

    public void Dispose()
    {
        DestroyHostWindow();
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        SizeChanged -= OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureHostWindow();
        UpdateHostBounds();
        RaiseHostReady();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DestroyHostWindow();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateHostBounds();
    }

    private void EnsureHostWindow()
    {
        if (_hwnd != nint.Zero)
        {
            return;
        }

        nint parentHwnd = WindowNative.GetWindowHandle(_owner);
        if (parentHwnd == nint.Zero)
        {
            throw new InvalidOperationException("WinUI window handle is not available.");
        }

        _hwnd = NativeHostWindow.Create(parentHwnd);
    }

    private void UpdateHostBounds()
    {
        if (_hwnd == nint.Zero || XamlRoot is null)
        {
            return;
        }

        double scale = HostRasterizationScale;
        Point origin = TransformToVisual(null).TransformPoint(new Point(0, 0));
        int x = (int)Math.Round(origin.X * scale);
        int y = (int)Math.Round(origin.Y * scale);
        int width = Math.Max(1, (int)Math.Round(ActualWidth * scale));
        int height = Math.Max(1, (int)Math.Round(ActualHeight * scale));
        NativeHostWindow.SetBounds(_hwnd, x, y, width, height);
    }

    private void RaiseHostReady()
    {
        if (_hostReadyRaised || _hwnd == nint.Zero)
        {
            return;
        }

        _hostReadyRaised = true;
        HostReady?.Invoke(this, new RendererHostReadyEventArgs(_hwnd, HostRasterizationScale));
    }

    private void DestroyHostWindow()
    {
        if (_hwnd != nint.Zero)
        {
            NativeHostWindow.Destroy(_hwnd);
            _hwnd = nint.Zero;
        }
    }

    private static class NativeHostWindow
    {
        private const string ClassName = "UntamedAndroidRendererHostWindow";
        private const uint WsChild = 0x40000000;
        private const uint WsVisible = 0x10000000;
        private const uint WsClipChildren = 0x02000000;
        private const uint WsClipSiblings = 0x04000000;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private const int WmEraseBackground = 0x0014;
        private static readonly WndProc WindowProc = WndProcCallback;
        private static ushort _classAtom;

        public static nint Create(nint parentHwnd)
        {
            EnsureRegistered();
            nint hwnd = NativeMethods.CreateWindowEx(
                0,
                ClassName,
                "",
                WsChild | WsVisible | WsClipChildren | WsClipSiblings,
                0,
                0,
                1,
                1,
                parentHwnd,
                nint.Zero,
                NativeMethods.GetModuleHandle(null),
                nint.Zero
            );
            if (hwnd == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateWindowEx failed.");
            }

            return hwnd;
        }

        public static void SetBounds(nint hwnd, int x, int y, int width, int height)
        {
            if (
                !NativeMethods.SetWindowPos(
                    hwnd,
                    nint.Zero,
                    x,
                    y,
                    width,
                    height,
                    SwpNoZOrder | SwpNoActivate | SwpShowWindow
                )
            )
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetWindowPos failed.");
            }
        }

        public static void Destroy(nint hwnd)
        {
            NativeMethods.DestroyWindow(hwnd);
        }

        private static void EnsureRegistered()
        {
            if (_classAtom != 0)
            {
                return;
            }

            var windowClass = new WndClassEx
            {
                Size = (uint)Marshal.SizeOf<WndClassEx>(),
                WindowProc = Marshal.GetFunctionPointerForDelegate(WindowProc),
                Instance = NativeMethods.GetModuleHandle(null),
                ClassName = ClassName,
            };

            _classAtom = NativeMethods.RegisterClassEx(ref windowClass);
            if (_classAtom == 0)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 1410)
                {
                    throw new Win32Exception(error, "RegisterClassEx failed.");
                }

                _classAtom = 1;
            }
        }

        private static nint WndProcCallback(nint hwnd, uint message, nint wParam, nint lParam)
        {
            if (message == WmEraseBackground)
            {
                return 1;
            }

            return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate nint WndProc(nint hwnd, uint message, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WndClassEx
        {
            public uint Size;
            public uint Style;
            public nint WindowProc;
            public int ClassExtra;
            public int WindowExtra;
            public nint Instance;
            public nint Icon;
            public nint Cursor;
            public nint Background;
            public string? MenuName;
            public string ClassName;
            public nint IconSmall;
        }

        private static partial class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern nint GetModuleHandle(string? moduleName);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern ushort RegisterClassEx(ref WndClassEx windowClass);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern nint CreateWindowEx(
                uint extendedStyle,
                string className,
                string windowName,
                uint style,
                int x,
                int y,
                int width,
                int height,
                nint parent,
                nint menu,
                nint instance,
                nint parameter
            );

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWindowPos(
                nint hwnd,
                nint hwndInsertAfter,
                int x,
                int y,
                int width,
                int height,
                uint flags
            );

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyWindow(nint hwnd);

            [DllImport("user32.dll")]
            public static extern nint DefWindowProc(
                nint hwnd,
                uint message,
                nint wParam,
                nint lParam
            );
        }
    }
}

internal sealed class RendererHostReadyEventArgs : EventArgs
{
    public RendererHostReadyEventArgs(nint hwnd, double rasterizationScale)
    {
        Hwnd = hwnd;
        RasterizationScale = rasterizationScale;
    }

    public nint Hwnd { get; }

    public double RasterizationScale { get; }
}
