using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Core.Services;

public interface IEmulatorRuntimeService
{
    bool IsRunning(AndroidEmulatorInstance instance);

    Task StartAsync(AndroidEmulatorInstance instance, CancellationToken cancellationToken = default);

    Task StopAsync(AndroidEmulatorInstance instance, CancellationToken cancellationToken = default);
}
