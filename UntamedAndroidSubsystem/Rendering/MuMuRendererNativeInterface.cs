using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace UntamedAndroidSubsystem.Rendering;

internal sealed class MuMuRendererNativeInterface : IDisposable
{
    private const uint LoadWithAlteredSearchPath = 0x00000008;
    private readonly nint _libraryHandle;
    private NativeInterface? _nativeInterface;
    private RendererCallbacks? _callbacks;
    private nint _callbacksTable;
    private bool _stopRequested;

    private MuMuRendererNativeInterface(nint libraryHandle)
    {
        _libraryHandle = libraryHandle;
    }

    public static MuMuRendererNativeInterface Load(string shellDirectory)
    {
        if (!Directory.Exists(shellDirectory))
        {
            throw new DirectoryNotFoundException(
                $"MuMu shell directory was not found: {shellDirectory}"
            );
        }

        string rendererPath = Path.Combine(shellDirectory, "libRenderer.dll");
        if (!File.Exists(rendererPath))
        {
            throw new FileNotFoundException("libRenderer.dll was not found.", rendererPath);
        }

        nint libraryHandle = NativeMethods.LoadLibraryEx(
            rendererPath,
            nint.Zero,
            LoadWithAlteredSearchPath
        );
        if (libraryHandle == nint.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            string detail = new Win32Exception(error).Message;
            if (error == 193)
            {
                detail +=
                    $" Current process architecture is {RuntimeInformation.ProcessArchitecture}; "
                    + "MuMu WOA renderer must be loaded by an ARM64 process.";
            }

            throw new InvalidOperationException(
                $"Failed to load {rendererPath}. Win32 error {error}: {detail}"
            );
        }

        return new MuMuRendererNativeInterface(libraryHandle);
    }

    public void SetCallbacks(RendererCallbacks callbacks)
    {
        nint exchangeAddress = NativeMethods.GetProcAddress(
            _libraryHandle,
            "emulation_interface_exchange"
        );
        if (exchangeAddress == nint.Zero)
        {
            throw new EntryPointNotFoundException("emulation_interface_exchange");
        }

        FreeCallbackTable();
        _callbacks = callbacks;
        _callbacksTable = callbacks.AllocateNativeTable();

        var exchange = Marshal.GetDelegateForFunctionPointer<EmulationInterfaceExchange>(
            exchangeAddress
        );
        nint nativeInterface = exchange(_callbacksTable);
        if (nativeInterface == nint.Zero)
        {
            throw new InvalidOperationException("emulation_interface_exchange returned null.");
        }

        _nativeInterface = NativeInterface.FromPointer(nativeInterface);
    }

    public int InitRenderer(string configJson)
    {
        EnsureNativeInterface();
        return _nativeInterface!.InitRenderer(configJson);
    }

    public void StopRenderer()
    {
        if (_stopRequested)
        {
            return;
        }

        _stopRequested = true;
        if (_nativeInterface?.StopRenderer is not null)
        {
            _nativeInterface.StopRenderer();
        }
    }

    public RendererLastError GetLastError()
    {
        if (_nativeInterface?.GetLastError is null)
        {
            return new RendererLastError(0, "");
        }

        _nativeInterface.GetLastError(out int bufferSize, out nint messagePointer);
        string message = messagePointer == nint.Zero
            ? ""
            : Marshal.PtrToStringUTF8(messagePointer) ?? "";
        return new RendererLastError(bufferSize, message);
    }

    public int SendMouseEvent(nint hwnd, int x, int y, int action, float wheelDelta)
    {
        if (_nativeInterface?.SendMouseEvent is null)
        {
            return 0;
        }

        return _nativeInterface.SendMouseEvent(hwnd, x, y, action, wheelDelta);
    }

    public void Render(int reason)
    {
        _nativeInterface?.Render?.Invoke(reason);
    }

    public void Dispose()
    {
        StopRenderer();
        FreeCallbackTable();
        if (_libraryHandle != nint.Zero)
        {
            NativeMethods.FreeLibrary(_libraryHandle);
        }
    }

    private void EnsureNativeInterface()
    {
        if (_nativeInterface is null)
        {
            throw new InvalidOperationException("Renderer native interface has not been exchanged.");
        }
    }

    private void FreeCallbackTable()
    {
        if (_callbacksTable != nint.Zero)
        {
            Marshal.FreeHGlobal(_callbacksTable);
            _callbacksTable = nint.Zero;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EmulationInterfaceExchange(nint callbacks);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitRendererFunc([MarshalAs(UnmanagedType.LPUTF8Str)] string configJson);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int UnknownFunc1(int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StopRendererFunc();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GetLastErrorFunc(out int bufferSize, out nint message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SendMouseEventFunc(nint hwnd, int x, int y, int action, float wheelDelta);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetKeyboardFocusFunc(int enabled);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SendTouchInputFunc(
        int slot,
        int action,
        int pointerId,
        int x,
        int y,
        float pressure,
        int toolType,
        float size,
        int source
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RenderFunc(int reason);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SendRawCommandFunc(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string command,
        int length
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NotifyPropertyChangeFunc(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SendCommandFunc(
        int requestId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    private sealed class NativeInterface
    {
        private NativeInterface(nint nativeInterface)
        {
            int offset = 0;
            InitRenderer = ReadDelegate<InitRendererFunc>(nativeInterface, ref offset);
            UnknownFunc1 = ReadDelegate<UnknownFunc1>(nativeInterface, ref offset);
            StopRenderer = ReadDelegate<StopRendererFunc>(nativeInterface, ref offset);
            GetLastError = ReadDelegate<GetLastErrorFunc>(nativeInterface, ref offset);
            SendMouseEvent = ReadDelegate<SendMouseEventFunc>(nativeInterface, ref offset);
            SetKeyboardFocus = ReadDelegate<SetKeyboardFocusFunc>(nativeInterface, ref offset);
            SendTouchInput = ReadDelegate<SendTouchInputFunc>(nativeInterface, ref offset);
            Render = ReadDelegate<RenderFunc>(nativeInterface, ref offset);
            SendRawCommand = ReadDelegate<SendRawCommandFunc>(nativeInterface, ref offset);
            NotifyPropertyChange = ReadDelegate<NotifyPropertyChangeFunc>(
                nativeInterface,
                ref offset
            );
            SendCommand = ReadDelegate<SendCommandFunc>(nativeInterface, ref offset);
        }

        public InitRendererFunc? InitRenderer { get; }

        public UnknownFunc1? UnknownFunc1 { get; }

        public StopRendererFunc? StopRenderer { get; }

        public GetLastErrorFunc? GetLastError { get; }

        public SendMouseEventFunc? SendMouseEvent { get; }

        public SetKeyboardFocusFunc? SetKeyboardFocus { get; }

        public SendTouchInputFunc? SendTouchInput { get; }

        public RenderFunc? Render { get; }

        public SendRawCommandFunc? SendRawCommand { get; }

        public NotifyPropertyChangeFunc? NotifyPropertyChange { get; }

        public SendCommandFunc? SendCommand { get; }

        public static NativeInterface FromPointer(nint nativeInterface)
        {
            var result = new NativeInterface(nativeInterface);
            if (result.InitRenderer is null || result.StopRenderer is null)
            {
                throw new InvalidOperationException("Renderer native interface is incomplete.");
            }

            return result;
        }

        private static T? ReadDelegate<T>(nint nativeInterface, ref int offset)
            where T : Delegate
        {
            nint address = Marshal.ReadIntPtr(nativeInterface, offset);
            offset += nint.Size;
            return address == nint.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
        }
    }

    private static partial class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint LoadLibraryEx(string fileName, nint reserved, uint flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern nint GetProcAddress(nint module, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(nint module);
    }
}

internal sealed class RendererCallbacks
{
    private const int CallbackCount = 11;

    public RendererCallbacks(
        OnRendererStartedCallback onRendererStarted,
        OnRendererStoppedCallback onRendererStopped,
        OnRomCallShellCallback onRomCallShell,
        OnShellCallRomCallback onShellCallRom,
        OnRomToShellCallback onRomToShell,
        OnAsyncRequestSuccessCallback onAsyncRequestSuccess,
        OnAsyncRequestErrorCallback onAsyncRequestError,
        OnFpsChangedCallback onFpsChanged,
        OnLogCallback onLog,
        OnUnknownCallback1 onUnknownCallback1,
        OnUnknownCallback2 onUnknownCallback2
    )
    {
        OnRendererStarted = onRendererStarted;
        OnRendererStopped = onRendererStopped;
        OnRomCallShell = onRomCallShell;
        OnShellCallRom = onShellCallRom;
        OnRomToShell = onRomToShell;
        OnAsyncRequestSuccess = onAsyncRequestSuccess;
        OnAsyncRequestError = onAsyncRequestError;
        OnFpsChanged = onFpsChanged;
        OnLog = onLog;
        OnUnknown1 = onUnknownCallback1;
        OnUnknown2 = onUnknownCallback2;
    }

    private OnRendererStartedCallback OnRendererStarted { get; }

    private OnRendererStoppedCallback OnRendererStopped { get; }

    private OnRomCallShellCallback OnRomCallShell { get; }

    private OnShellCallRomCallback OnShellCallRom { get; }

    private OnRomToShellCallback OnRomToShell { get; }

    private OnAsyncRequestSuccessCallback OnAsyncRequestSuccess { get; }

    private OnAsyncRequestErrorCallback OnAsyncRequestError { get; }

    private OnFpsChangedCallback OnFpsChanged { get; }

    private OnLogCallback OnLog { get; }

    private OnUnknownCallback1 OnUnknown1 { get; }

    private OnUnknownCallback2 OnUnknown2 { get; }

    public nint AllocateNativeTable()
    {
        nint callbacks = Marshal.AllocHGlobal(nint.Size * CallbackCount);
        int offset = 0;
        WriteDelegate(callbacks, ref offset, OnRendererStarted);
        WriteDelegate(callbacks, ref offset, OnRendererStopped);
        WriteDelegate(callbacks, ref offset, OnRomCallShell);
        WriteDelegate(callbacks, ref offset, OnShellCallRom);
        WriteDelegate(callbacks, ref offset, OnRomToShell);
        WriteDelegate(callbacks, ref offset, OnAsyncRequestSuccess);
        WriteDelegate(callbacks, ref offset, OnAsyncRequestError);
        WriteDelegate(callbacks, ref offset, OnFpsChanged);
        WriteDelegate(callbacks, ref offset, OnLog);
        WriteDelegate(callbacks, ref offset, OnUnknown1);
        WriteDelegate(callbacks, ref offset, OnUnknown2);
        return callbacks;
    }

    private static void WriteDelegate(nint table, ref int offset, Delegate callback)
    {
        Marshal.WriteIntPtr(table, offset, Marshal.GetFunctionPointerForDelegate(callback));
        offset += nint.Size;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnRendererStartedCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string infoJson
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnRendererStoppedCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string message
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public delegate string OnRomCallShellCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnShellCallRomCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnRomToShellCallback(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAsyncRequestSuccessCallback(
        int requestId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAsyncRequestErrorCallback(
        int requestId,
        int errorCode,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string payload
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnFpsChangedCallback(int fps);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnLogCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnUnknownCallback1();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnUnknownCallback2(double value);
}

internal readonly record struct RendererLastError(int BufferSize, string Message);
