namespace UntamedAndroidSubsystem.Core.Models;

public sealed class EmulatorDiskLayout
{
    public required string SystemDiskPath { get; init; }

    public required string DataDiskPath { get; init; }

    public required string SwapDiskPath { get; init; }

    public required string BootDiskPath { get; init; }

    public bool SystemDiskReadOnly { get; init; } = true;
}
