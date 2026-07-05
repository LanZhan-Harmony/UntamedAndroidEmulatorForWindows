using System.Text.Json.Serialization;
using UntamedAndroidSubsystem.Core.HyperV;
using UntamedAndroidSubsystem.Core.Services;

namespace UntamedAndroidSubsystem.Core.Serialization;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(MuMuInstanceSettings))]
[JsonSerializable(typeof(HyperVLaunchPlan))]
[JsonSerializable(typeof(HcsSystemConfiguration))]
[JsonSerializable(typeof(HcnNetworkConfiguration))]
[JsonSerializable(typeof(HcnEndpointConfiguration))]
internal sealed partial class EmulatorJsonSerializerContext : JsonSerializerContext;
