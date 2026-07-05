using System.Runtime.InteropServices;

namespace UntamedAndroidSubsystem.Core.HyperV;

internal static class HcsNativeMethods
{
    private const string HcsLibrary = "computecore.dll";
    private const string HcnLibrary = "computenetwork.dll";

    [DllImport(HcsLibrary, ExactSpelling = true)]
    internal static extern nint HcsCreateOperation(nint context, nint callback);

    [DllImport(HcsLibrary, ExactSpelling = true)]
    internal static extern void HcsCloseOperation(nint operation);

    [DllImport(HcsLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcsCreateComputeSystem(
        string id,
        string configuration,
        nint operation,
        nint securityDescriptor,
        out nint computeSystem
    );

    [DllImport(HcsLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcsOpenComputeSystem(
        string id,
        uint requestedAccess,
        out nint computeSystem
    );

    [DllImport(HcsLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcsStartComputeSystem(
        nint computeSystem,
        nint operation,
        string? options
    );

    [DllImport(HcsLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcsShutDownComputeSystem(
        nint computeSystem,
        nint operation,
        string? options
    );

    [DllImport(HcsLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcsTerminateComputeSystem(
        nint computeSystem,
        nint operation,
        string? options
    );

    [DllImport(HcsLibrary, ExactSpelling = true)]
    internal static extern void HcsCloseComputeSystem(nint computeSystem);

    [DllImport(HcsLibrary, ExactSpelling = true)]
    internal static extern int HcsWaitForOperationResult(
        nint operation,
        uint timeoutMs,
        out nint resultDocument
    );

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnCreateNetwork(
        ref Guid id,
        string settings,
        out nint network,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnOpenNetwork(
        ref Guid id,
        out nint network,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true)]
    internal static extern int HcnCloseNetwork(nint network);

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnEnumerateEndpoints(
        string query,
        out nint endpoints,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnCreateEndpoint(
        nint network,
        ref Guid id,
        string settings,
        out nint endpoint,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnOpenEndpoint(
        ref Guid id,
        out nint endpoint,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true, CharSet = CharSet.Unicode)]
    internal static extern int HcnQueryEndpointProperties(
        nint endpoint,
        string query,
        out nint properties,
        out nint errorRecord
    );

    [DllImport(HcnLibrary, ExactSpelling = true)]
    internal static extern int HcnDeleteEndpoint(ref Guid id, out nint errorRecord);

    [DllImport(HcnLibrary, ExactSpelling = true)]
    internal static extern int HcnCloseEndpoint(nint endpoint);
}
