namespace UntamedAndroidSubsystem.Core.Configuration;

public sealed class EmulatorPaths
{
    private const string MuMuVmsRoot = @"D:\code\WinUI3\mumu\MuMuPlayer\vms";
    private const string MuMuRoot = @"D:\code\WinUI3\mumu\MuMuPlayer";

    public EmulatorPaths(
        string vmsRoot,
        string baseVmDirectory,
        string deviceSharedDirectory,
        string globalSharedDirectory,
        string shellDirectory,
        string logsRoot
    )
    {
        VmsRoot = vmsRoot;
        BaseVmDirectory = baseVmDirectory;
        DeviceSharedDirectory = deviceSharedDirectory;
        GlobalSharedDirectory = globalSharedDirectory;
        ShellDirectory = shellDirectory;
        LogsRoot = logsRoot;
    }

    public string VmsRoot { get; }

    public string BaseVmDirectory { get; }

    public string DeviceSharedDirectory { get; }

    public string GlobalSharedDirectory { get; }

    public string ShellDirectory { get; }

    public string LogsRoot { get; }

    public static EmulatorPaths CreateDefault()
    {
        if (Directory.Exists(MuMuVmsRoot))
        {
            return new EmulatorPaths(
                MuMuVmsRoot,
                Path.Combine(MuMuVmsRoot, "base.madoa"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "MuMuShared"
                ),
                Path.Combine(MuMuRoot, "misc", "global_shared"),
                Path.Combine(MuMuRoot, "shell"),
                Path.Combine(MuMuRoot, "logs")
            );
        }

        string appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UntamedAndroidSubsystem"
        );
        string vmsRoot = Path.Combine(appDataRoot, "vms");
        return new EmulatorPaths(
            vmsRoot,
            Path.Combine(vmsRoot, "base.madoa"),
            Path.Combine(appDataRoot, "shared"),
            Path.Combine(appDataRoot, "global_shared"),
            Path.Combine(appDataRoot, "shell"),
            Path.Combine(appDataRoot, "logs")
        );
    }
}
