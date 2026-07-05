using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Core.Services;

public interface IEmulatorInstanceStore
{
    Task<IReadOnlyList<AndroidEmulatorInstance>> GetInstancesAsync(
        CancellationToken cancellationToken = default
    );
}
