namespace UntamedAndroidSubsystem.Core.Models;

public sealed class SharedFolderLayout
{
    public required string DeviceSharedPath { get; init; }

    public required string GlobalSharedPath { get; init; }

    public required string PrivateSharedPath { get; init; }
}
