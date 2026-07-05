using System;
using System.IO;
using System.Text;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Rendering;

internal sealed class AndroidRendererSession : IDisposable
{
    private readonly AndroidEmulatorInstance _instance;
    private readonly EmulatorPaths _paths;
    private readonly object _logLock = new();
    private MuMuRendererNativeInterface? _native;
    private bool _started;

    public AndroidRendererSession(AndroidEmulatorInstance instance, EmulatorPaths paths)
    {
        _instance = instance;
        _paths = paths;
        LogDirectory = RendererConfigBuilder.GetLogDirectory(paths, instance);
    }

    public string LogDirectory { get; }

    public event EventHandler<string>? StatusChanged;

    public void Start(nint canvasParent, double screenScale)
    {
        if (_started)
        {
            return;
        }

        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(Path.Combine(_instance.InstanceDirectory, "misc"));

        RendererIdentity identity = RendererConfigBuilder.CreateIdentity(_instance);
        string initJson = RendererConfigBuilder.Build(
            _instance,
            _paths,
            canvasParent,
            screenScale,
            identity
        );

        File.WriteAllText(
            Path.Combine(_instance.InstanceDirectory, "misc", "untamed-renderer-init.json"),
            initJson
        );

        try
        {
            UpdateStatus("加载 libRenderer.dll");
            _native = MuMuRendererNativeInterface.Load(_paths.ShellDirectory);
            _native.SetCallbacks(CreateCallbacks());

            UpdateStatus("初始化 renderer");
            WriteLog("InitRenderer JSON:");
            WriteLog(initJson);
            int result = _native.InitRenderer(initJson);
            if (result != 1)
            {
                RendererLastError lastError = _native.GetLastError();
                throw new InvalidOperationException(
                    $"InitRenderer returned {result}. LastError={lastError.BufferSize}, {lastError.Message}"
                );
            }

            _started = true;
            UpdateStatus("renderer 已启动，等待 Android 出帧");
        }
        catch
        {
            _native?.Dispose();
            _native = null;
            throw;
        }
    }

    public void Stop()
    {
        if (_native is null)
        {
            return;
        }

        try
        {
            UpdateStatus("停止 renderer");
            _native.StopRenderer();
        }
        finally
        {
            _native.Dispose();
            _native = null;
            _started = false;
            UpdateStatus("renderer 已停止");
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private RendererCallbacks CreateCallbacks()
    {
        return new RendererCallbacks(
            OnRendererStarted,
            OnRendererStopped,
            OnRomCallShell,
            OnShellCallRom,
            OnRomToShell,
            OnAsyncRequestSuccess,
            OnAsyncRequestError,
            OnFpsChanged,
            OnLog,
            OnUnknownCallback1,
            OnUnknownCallback2
        );
    }

    private void OnRendererStarted(string infoJson)
    {
        WriteLog($"OnRendererStarted: {infoJson}");
        File.WriteAllText(Path.Combine(LogDirectory, "untamed-renderer-info.json"), infoJson);
        UpdateStatus("renderer 初始化成功");
    }

    private void OnRendererStopped(string message)
    {
        WriteLog($"OnRendererStopped: {message}");
        UpdateStatus("renderer 已停止");
    }

    private string OnRomCallShell(string name, string payload)
    {
        WriteLog($"OnRomCallShell: {name} {payload}");
        if (name.StartsWith("write_persist:", StringComparison.Ordinal))
        {
            WritePersist(name["write_persist:".Length..], payload);
            return "";
        }

        if (name.StartsWith("load_persist:", StringComparison.Ordinal))
        {
            return LoadPersist(name["load_persist:".Length..]);
        }

        return "null";
    }

    private void OnShellCallRom(string name, string payload)
    {
        WriteLog($"OnShellCallRom: {name} {payload}");
    }

    private void OnRomToShell(string name, string payload)
    {
        WriteLog($"OnRomToShell: {name} {payload}");
        if (name == "android_boot_status")
        {
            UpdateStatus($"Android 启动状态: {payload}");
        }
    }

    private void OnAsyncRequestSuccess(int requestId, string payload)
    {
        WriteLog($"OnAsyncRequestSuccess: {requestId} {payload}");
    }

    private void OnAsyncRequestError(int requestId, int errorCode, string payload)
    {
        WriteLog($"OnAsyncRequestError: {requestId} {errorCode} {payload}");
    }

    private void OnFpsChanged(int fps)
    {
        UpdateStatus($"FPS {fps}");
    }

    private void OnLog(string message)
    {
        WriteLog($"OnLog: {message}");
    }

    private void OnUnknownCallback1()
    {
        WriteLog("OnUnknownCallback1");
    }

    private void OnUnknownCallback2(double value)
    {
        WriteLog($"OnUnknownCallback2: {value}");
    }

    private void WritePersist(string name, string value)
    {
        string path = GetPersistPath(name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, value);
    }

    private string LoadPersist(string name)
    {
        string path = GetPersistPath(name);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    private string GetPersistPath(string name)
    {
        string safeName = string.Join(
            "_",
            name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
        );
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "default";
        }

        return Path.Combine(_instance.InstanceDirectory, "misc", "renderer_persist", safeName);
    }

    private void UpdateStatus(string status)
    {
        WriteLog(status);
        StatusChanged?.Invoke(this, status);
    }

    private void WriteLog(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            lock (_logLock)
            {
                File.AppendAllText(
                    Path.Combine(LogDirectory, "untamed-renderer-bridge.log"),
                    $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}{Environment.NewLine}",
                    Encoding.UTF8
                );
            }
        }
        catch
        {
            // Renderer callbacks can arrive on native worker threads; logging must not break them.
        }
    }
}
