using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using UntamedAndroidSubsystem.Core.HyperV;
using UntamedAndroidSubsystem.Core.Models;
using UntamedAndroidSubsystem.Core.Serialization;

namespace UntamedAndroidSubsystem.Core.Services;

public sealed class HcsEmulatorRuntimeService : IEmulatorRuntimeService, IDisposable
{
    private const string EmptyHcnQuery = "{}";
    private const string EndpointNamePrefix = "UntamedAndroid-";
    private const uint OperationTimeoutMs = 60_000;
    private const uint GenericAllAccess = 0x10000000;
    private const int VmComputeSystemNotFoundHResult = unchecked((int)0x8037010E);
    private const int VmComputeSystemNotFoundNdisResult = unchecked((int)0xC037010E);
    private const int VmComputeSystemAlreadyStoppedHResult = unchecked((int)0x80370110);
    private const int VmComputeSystemAlreadyStoppedNdisResult = unchecked((int)0xC0370110);
    private const int ErrorNotSupportedHResult = unchecked((int)0x80070032);

    private readonly HcsConfigurationBuilder _configurationBuilder;
    private readonly ConcurrentDictionary<int, RunningComputeSystem> _runningSystems = [];

    public HcsEmulatorRuntimeService(HcsConfigurationBuilder configurationBuilder)
    {
        _configurationBuilder = configurationBuilder;
    }

    public bool IsRunning(AndroidEmulatorInstance instance)
    {
        return _runningSystems.ContainsKey(instance.Id);
    }

    public Task StartAsync(
        AndroidEmulatorInstance instance,
        CancellationToken cancellationToken = default
    )
    {
        return Task.Run(() => StartCore(instance, cancellationToken), cancellationToken);
    }

    public Task StopAsync(AndroidEmulatorInstance instance, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => StopCore(instance, cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        foreach (RunningComputeSystem runningSystem in _runningSystems.Values)
        {
            TerminateAndCloseComputeSystemBestEffort(runningSystem.ComputeSystem);
            DeleteEndpointIfExists(runningSystem.EndpointId);
        }
        _runningSystems.Clear();
    }

    private void StartCore(AndroidEmulatorInstance instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureHostComputeApiAvailable();
        EnsureDirectories(instance);
        EnsureInstanceDisks(instance);

        string systemId = GetSystemId(instance);
        CleanupStaleComputeSystem(systemId);
        DeleteEndpointIfExists(CreateLegacyStableEndpointId(instance.Id));

        HyperVLaunchPlan plan = _configurationBuilder.Build(instance);
        WriteLaunchPlan(instance, plan);
        EnsureNetwork(plan);
        CleanupStaleEndpoints(plan, CreateProtectedEndpointSet(plan.HcnEndpoint.ID));
        EnsureEndpoint(plan);

        string hcsJson = JsonSerializer.Serialize(
            plan.HcsSystem,
            EmulatorJsonSerializerContext.Default.HcsSystemConfiguration
        );
        nint computeSystem = nint.Zero;

        try
        {
            using var createOperation = HcsOperation.Create();
            ThrowIfFailed(
                "HcsCreateComputeSystem",
                HcsNativeMethods.HcsCreateComputeSystem(
                    systemId,
                    hcsJson,
                    createOperation.Handle,
                    nint.Zero,
                    out computeSystem
                )
            );
            createOperation.Wait("HcsCreateComputeSystem");

            using var startOperation = HcsOperation.Create();
            ThrowIfFailed(
                "HcsStartComputeSystem",
                HcsNativeMethods.HcsStartComputeSystem(computeSystem, startOperation.Handle, null)
            );
            startOperation.Wait("HcsStartComputeSystem");
        }
        catch
        {
            if (computeSystem != nint.Zero)
            {
                TerminateAndCloseComputeSystemBestEffort(computeSystem);
            }

            DeleteEndpointIfExists(plan.HcnEndpoint.ID);
            throw;
        }

        Guid endpointId = Guid.Parse(plan.HcnEndpoint.ID);
        if (!_runningSystems.TryAdd(instance.Id, new RunningComputeSystem(computeSystem, endpointId)))
        {
            TerminateAndCloseComputeSystemBestEffort(computeSystem);
            DeleteEndpointIfExists(endpointId);
            throw new InvalidOperationException($"Instance {instance.Id} is already running.");
        }
    }

    private IReadOnlySet<Guid> CreateProtectedEndpointSet(string currentEndpointId)
    {
        HashSet<Guid> protectedEndpointIds = _runningSystems.Values
            .Select(runningSystem => runningSystem.EndpointId)
            .ToHashSet();

        if (Guid.TryParse(currentEndpointId, out Guid currentEndpointGuid))
        {
            protectedEndpointIds.Add(currentEndpointGuid);
        }

        return protectedEndpointIds;
    }

    private void StopCore(AndroidEmulatorInstance instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_runningSystems.TryRemove(instance.Id, out RunningComputeSystem runningSystem))
        {
            return;
        }

        nint computeSystem = runningSystem.ComputeSystem;
        try
        {
            ShutDownOrTerminateComputeSystem(computeSystem);
        }
        finally
        {
            HcsNativeMethods.HcsCloseComputeSystem(computeSystem);
            DeleteEndpointIfExists(runningSystem.EndpointId);
        }
    }

    private static void EnsureHostComputeApiAvailable()
    {
        string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (
            !File.Exists(Path.Combine(system32, "computecore.dll"))
            || !File.Exists(Path.Combine(system32, "computenetwork.dll"))
        )
        {
            throw new PlatformNotSupportedException(
                "Windows Host Compute Service/Network API is not available on this system."
            );
        }
    }

    private static void EnsureDirectories(AndroidEmulatorInstance instance)
    {
        Directory.CreateDirectory(Path.Combine(instance.InstanceDirectory, "misc"));
        Directory.CreateDirectory(instance.SharedFolders.DeviceSharedPath);
        Directory.CreateDirectory(instance.SharedFolders.GlobalSharedPath);
        Directory.CreateDirectory(instance.SharedFolders.PrivateSharedPath);
    }

    private static void EnsureInstanceDisks(AndroidEmulatorInstance instance)
    {
        CopyBaseDiskIfMissing(instance, "data.vhdx");
        CopyBaseDiskIfMissing(instance, "swap.vhdx");

        if (instance.IsSystemWritable)
        {
            CopyBaseDiskIfMissing(instance, "system.vhdx");
        }
    }

    private static void CleanupStaleComputeSystem(string systemId)
    {
        int hresult = HcsNativeMethods.HcsOpenComputeSystem(
            systemId,
            GenericAllAccess,
            out nint computeSystem
        );
        if (IsVmComputeSystemNotFound(hresult))
        {
            return;
        }

        ThrowIfFailed("HcsOpenComputeSystem", hresult);

        try
        {
            TerminateComputeSystem(computeSystem);
        }
        finally
        {
            HcsNativeMethods.HcsCloseComputeSystem(computeSystem);
        }
    }

    private static void ShutDownOrTerminateComputeSystem(nint computeSystem)
    {
        using var operation = HcsOperation.Create();
        int hresult = HcsNativeMethods.HcsShutDownComputeSystem(
            computeSystem,
            operation.Handle,
            null
        );
        if (IsVmComputeSystemAlreadyStopped(hresult))
        {
            return;
        }

        if (hresult < 0)
        {
            TerminateComputeSystem(computeSystem);
            return;
        }

        try
        {
            operation.Wait("HcsShutDownComputeSystem");
        }
        catch (HcsException ex) when (IsVmComputeSystemAlreadyStopped(ex.HResultCode))
        {
        }
        catch (HcsException ex) when (IsGracefulShutdownUnsupported(ex.HResultCode))
        {
            TerminateComputeSystem(computeSystem);
        }
    }

    private static void TerminateAndCloseComputeSystemBestEffort(nint computeSystem)
    {
        try
        {
            TerminateComputeSystem(computeSystem);
        }
        catch
        {
            // Preserve the original HCS error; cleanup failures are secondary here.
        }
        finally
        {
            HcsNativeMethods.HcsCloseComputeSystem(computeSystem);
        }
    }

    private static void TerminateComputeSystem(nint computeSystem)
    {
        using var operation = HcsOperation.Create();
        int hresult = HcsNativeMethods.HcsTerminateComputeSystem(
            computeSystem,
            operation.Handle,
            null
        );
        if (IsVmComputeSystemAlreadyStopped(hresult))
        {
            return;
        }

        ThrowIfFailed("HcsTerminateComputeSystem", hresult);

        try
        {
            operation.Wait("HcsTerminateComputeSystem");
        }
        catch (HcsException ex) when (IsVmComputeSystemAlreadyStopped(ex.HResultCode))
        {
        }
    }

    private static void CopyBaseDiskIfMissing(AndroidEmulatorInstance instance, string diskName)
    {
        string instanceDisk = Path.Combine(instance.InstanceDirectory, diskName);
        string baseDisk = Path.Combine(instance.BaseDirectory, diskName);
        if (!File.Exists(instanceDisk) && File.Exists(baseDisk))
        {
            File.Copy(baseDisk, instanceDisk);
        }
    }

    private static void WriteLaunchPlan(AndroidEmulatorInstance instance, HyperVLaunchPlan plan)
    {
        string planPath = Path.Combine(instance.InstanceDirectory, "misc", "untamed-hcs-plan.json");
        string json = JsonSerializer.Serialize(
            plan,
            EmulatorJsonSerializerContext.Default.HyperVLaunchPlan
        );
        File.WriteAllText(planPath, json);
    }

    private static void EnsureNetwork(HyperVLaunchPlan plan)
    {
        Guid networkId = Guid.Parse(plan.HcnNetwork.ID);
        string networkJson = JsonSerializer.Serialize(
            plan.HcnNetwork,
            EmulatorJsonSerializerContext.Default.HcnNetworkConfiguration
        );

        int hresult = HcsNativeMethods.HcnCreateNetwork(
            ref networkId,
            networkJson,
            out nint network,
            out nint errorRecord
        );
        if (hresult < 0)
        {
            FreeNativeString(errorRecord);
            hresult = HcsNativeMethods.HcnOpenNetwork(ref networkId, out network, out errorRecord);
        }

        ThrowIfFailed("HcnCreate/OpenNetwork", hresult, ReadAndFreeNativeString(errorRecord));
        HcsNativeMethods.HcnCloseNetwork(network);
    }

    private static void EnsureEndpoint(HyperVLaunchPlan plan)
    {
        Guid networkId = Guid.Parse(plan.HcnNetwork.ID);
        Guid endpointId = Guid.Parse(plan.HcnEndpoint.ID);
        string endpointJson = JsonSerializer.Serialize(
            plan.HcnEndpoint,
            EmulatorJsonSerializerContext.Default.HcnEndpointConfiguration
        );

        int hresult = HcsNativeMethods.HcnOpenNetwork(
            ref networkId,
            out nint network,
            out nint errorRecord
        );
        ThrowIfFailed("HcnOpenNetwork", hresult, ReadAndFreeNativeString(errorRecord));

        try
        {
            DeleteEndpointIfExists(endpointId);
            hresult = HcsNativeMethods.HcnCreateEndpoint(
                network,
                ref endpointId,
                endpointJson,
                out nint endpoint,
                out errorRecord
            );

            ThrowIfFailed("HcnCreateEndpoint", hresult, ReadAndFreeNativeString(errorRecord));
            HcsNativeMethods.HcnCloseEndpoint(endpoint);
        }
        finally
        {
            HcsNativeMethods.HcnCloseNetwork(network);
        }
    }

    private static void CleanupStaleEndpoints(
        HyperVLaunchPlan plan,
        IReadOnlySet<Guid> protectedEndpointIds
    )
    {
        try
        {
            foreach (Guid endpointId in EnumerateEndpointIds())
            {
                if (
                    !protectedEndpointIds.Contains(endpointId)
                    && IsOwnedEndpoint(endpointId, plan.HcnNetwork.ID)
                )
                {
                    DeleteEndpointIfExists(endpointId);
                }
            }
        }
        catch
        {
            // Stale endpoint cleanup is best-effort; launch should still use the fresh endpoint.
        }
    }

    private static List<Guid> EnumerateEndpointIds()
    {
        int hresult = HcsNativeMethods.HcnEnumerateEndpoints(
            EmptyHcnQuery,
            out nint endpoints,
            out nint errorRecord
        );
        ThrowIfFailed("HcnEnumerateEndpoints", hresult, ReadAndFreeNativeString(errorRecord));

        string? endpointsJson = ReadAndFreeNativeString(endpoints);
        if (string.IsNullOrWhiteSpace(endpointsJson))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(endpointsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<Guid> endpointIds = [];
        foreach (JsonElement endpoint in document.RootElement.EnumerateArray())
        {
            if (TryReadEndpointId(endpoint, out Guid endpointId))
            {
                endpointIds.Add(endpointId);
            }
        }

        return endpointIds;
    }

    private static bool IsOwnedEndpoint(Guid endpointId, string networkId)
    {
        int hresult = HcsNativeMethods.HcnOpenEndpoint(
            ref endpointId,
            out nint endpoint,
            out nint errorRecord
        );
        if (hresult < 0)
        {
            FreeNativeString(errorRecord);
            return false;
        }
        FreeNativeString(errorRecord);

        try
        {
            hresult = HcsNativeMethods.HcnQueryEndpointProperties(
                endpoint,
                EmptyHcnQuery,
                out nint properties,
                out errorRecord
            );
            if (hresult < 0)
            {
                FreeNativeString(errorRecord);
                return false;
            }
            FreeNativeString(errorRecord);

            string? propertiesJson = ReadAndFreeNativeString(properties);
            if (string.IsNullOrWhiteSpace(propertiesJson))
            {
                return false;
            }

            using JsonDocument document = JsonDocument.Parse(propertiesJson);
            JsonElement root = document.RootElement;
            return HasEndpointNetwork(root, networkId) || HasEndpointNamePrefix(root);
        }
        catch (JsonException)
        {
            return false;
        }
        finally
        {
            HcsNativeMethods.HcnCloseEndpoint(endpoint);
        }
    }

    private static bool TryReadEndpointId(JsonElement endpoint, out Guid endpointId)
    {
        if (endpoint.ValueKind == JsonValueKind.String)
        {
            return Guid.TryParse(endpoint.GetString(), out endpointId);
        }

        if (
            endpoint.ValueKind == JsonValueKind.Object
            && TryGetStringProperty(endpoint, out string? id, "ID", "Id", "EndpointId")
        )
        {
            return Guid.TryParse(id, out endpointId);
        }

        endpointId = Guid.Empty;
        return false;
    }

    private static bool HasEndpointNetwork(JsonElement endpoint, string networkId)
    {
        return TryGetStringProperty(endpoint, out string? endpointNetworkId, "VirtualNetwork")
            && string.Equals(endpointNetworkId, networkId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasEndpointNamePrefix(JsonElement endpoint)
    {
        return TryGetStringProperty(endpoint, out string? name, "Name")
            && name.StartsWith(EndpointNamePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetStringProperty(
        JsonElement element,
        out string value,
        params string[] names
    )
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (
                    names.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                )
                {
                    value = property.Value.GetString() ?? "";
                    return true;
                }
            }
        }

        value = "";
        return false;
    }

    private static void DeleteEndpointIfExists(string endpointId)
    {
        if (Guid.TryParse(endpointId, out Guid endpointGuid))
        {
            DeleteEndpointIfExists(endpointGuid);
        }
    }

    private static void DeleteEndpointIfExists(Guid endpointId)
    {
        int hresult = HcsNativeMethods.HcnDeleteEndpoint(ref endpointId, out nint errorRecord);
        FreeNativeString(errorRecord);
        _ = hresult;
    }

    private static Guid CreateLegacyStableEndpointId(int instanceId)
    {
        var bytes = HyperVConfigurationDefaults.NetworkId.ToByteArray();
        BitConverter.GetBytes(instanceId + 1).CopyTo(bytes, 12);
        return new Guid(bytes);
    }

    private static bool IsVmComputeSystemNotFound(int hresult)
    {
        return hresult == VmComputeSystemNotFoundHResult
            || hresult == VmComputeSystemNotFoundNdisResult;
    }

    private static bool IsVmComputeSystemAlreadyStopped(int hresult)
    {
        return hresult == VmComputeSystemAlreadyStoppedHResult
            || hresult == VmComputeSystemAlreadyStoppedNdisResult;
    }

    private static bool IsGracefulShutdownUnsupported(int hresult)
    {
        return hresult == ErrorNotSupportedHResult;
    }

    private static string GetSystemId(AndroidEmulatorInstance instance)
    {
        return $"UntamedAndroid-vm{instance.Id}";
    }

    private static void ThrowIfFailed(
        string operation,
        int hresult,
        string? resultDocument = null
    )
    {
        if (hresult < 0)
        {
            throw new HcsException(operation, hresult, resultDocument);
        }
    }

    private static string? ReadAndFreeNativeString(nint pointer)
    {
        if (pointer == nint.Zero)
        {
            return null;
        }

        string? value = Marshal.PtrToStringUni(pointer);
        Marshal.FreeCoTaskMem(pointer);
        return value;
    }

    private static void FreeNativeString(nint pointer)
    {
        if (pointer != nint.Zero)
        {
            Marshal.FreeCoTaskMem(pointer);
        }
    }

    private readonly record struct RunningComputeSystem(nint ComputeSystem, Guid EndpointId);

    private sealed class HcsOperation : IDisposable
    {
        private HcsOperation(nint handle)
        {
            Handle = handle;
        }

        public nint Handle { get; }

        public static HcsOperation Create()
        {
            nint handle = HcsNativeMethods.HcsCreateOperation(nint.Zero, nint.Zero);
            if (handle == nint.Zero)
            {
                throw new InvalidOperationException("HcsCreateOperation returned a null handle.");
            }

            return new HcsOperation(handle);
        }

        public void Wait(string operation)
        {
            int hresult = HcsNativeMethods.HcsWaitForOperationResult(
                Handle,
                OperationTimeoutMs,
                out nint resultDocument
            );
            ThrowIfFailed(operation, hresult, ReadAndFreeNativeString(resultDocument));
        }

        public void Dispose()
        {
            HcsNativeMethods.HcsCloseOperation(Handle);
        }
    }
}
