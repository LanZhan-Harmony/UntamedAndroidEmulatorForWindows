using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UntamedAndroidSubsystem.Core.Configuration;
using UntamedAndroidSubsystem.Core.Models;

namespace UntamedAndroidSubsystem.Rendering;

internal static class RendererConfigBuilder
{
    private const string AppVersion = "1.6.16";
    private const string PlayerEngine = "WOAPRO";
    private const string PlayerPackage = "mumu";
    private const string PlayerChannel = "gw-arm-beta";
    private const int PlatformHyperV = 12808;
    private const int PackedLowFpsLimits = 5 | (15 << 8) | (30 << 16);

    public static string Build(
        AndroidEmulatorInstance instance,
        EmulatorPaths paths,
        nint canvasParent,
        double screenScale,
        RendererIdentity identity
    )
    {
        int resolutionWidth = Math.Min(instance.FramebufferWidth, instance.FramebufferHeight);
        int resolutionHeight = Math.Max(instance.FramebufferWidth, instance.FramebufferHeight);
        string instanceName = Path.GetFileName(instance.InstanceDirectory);
        string logDirectory = GetLogDirectory(paths, instance);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = true,
            }
        );

        writer.WriteStartObject();
        writer.WriteString("app_token", identity.AppToken);
        writer.WriteString("app_version", AppVersion);
        writer.WriteNumber("audio_out_hardware_off", 0);
        writer.WriteNumber("dpi", instance.FramebufferDpi);
        writer.WriteNumber("force_dedicated_gpu", 0);
        writer.WriteNumber("fps_limit", 60);
        writer.WriteNumber("fps_limit_low", PackedLowFpsLimits);
        writer.WriteNumber("gnss_latitude", 0);
        writer.WriteNumber("gnss_longitude", 0);
        writer.WriteNumber("gnss_metersElevation", 0);
        writer.WriteString("instance", instanceName);
        writer.WriteNumber("main_canvas_parent", canvasParent.ToInt64());
        writer.WriteNumber("opt_flag", 2);
        writer.WriteNumber("platform", PlatformHyperV);
        writer.WriteNumber("present_vsync_on", 0);
        writer.WriteNumber("resolution_height", resolutionHeight);
        writer.WriteNumber("resolution_width", resolutionWidth);
        writer.WriteNumber("rotation", instance.InitialRotation);
        writer.WriteNumber("screen_scale", screenScale);
        writer.WriteString(
            "server_crash_url",
            "http://nemu-api.game163.dev.webapp.163.com:8400/api/crashrpt"
        );
        writer.WriteString("server_host", "mumu.nie.netease.com");
        writer.WriteNumber("server_port", 80);
        writer.WriteString("user_uuid", identity.UserUuid);
        writer.WriteNumber("vulkan_enabled", 1);
        writer.WriteString("data_dir", instance.InstanceDirectory);
        writer.WriteString("log_dir", logDirectory);
        writer.WriteString("gpu_device_name", instance.GpuName);
        writer.WriteString("thumbnail_surface_name", identity.ThumbnailSurfaceName);
        writer.WriteString(
            "system_init_properties",
            BuildSystemInitProperties(instance, resolutionWidth, resolutionHeight, identity)
        );
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string GetLogDirectory(EmulatorPaths paths, AndroidEmulatorInstance instance)
    {
        return Path.Combine(paths.LogsRoot, Path.GetFileName(instance.InstanceDirectory));
    }

    public static RendererIdentity CreateIdentity(AndroidEmulatorInstance instance)
    {
        string identityDirectory = Path.Combine(instance.InstanceDirectory, "misc");
        Directory.CreateDirectory(identityDirectory);

        return new RendererIdentity(
            ReadOrCreateGuid(Path.Combine(identityDirectory, "untamed-renderer-token.txt")),
            ReadOrCreateGuid(Path.Combine(identityDirectory, "untamed-renderer-user.txt")),
            $"TNSM{Environment.ProcessId}{Stopwatch.GetTimestamp()}"
        );
    }

    private static string BuildSystemInitProperties(
        AndroidEmulatorInstance instance,
        int resolutionWidth,
        int resolutionHeight,
        RendererIdentity identity
    )
    {
        string locale = CultureInfo.CurrentUICulture.Name;
        if (string.IsNullOrWhiteSpace(locale))
        {
            locale = "zh-CN";
        }

        string localeUnderscore = locale.Replace('-', '_');
        List<KeyValuePair<string, string>> properties =
        [
            new("nemud.app_keep_alive", "false"),
            new("nemud.system_writable", instance.IsSystemWritable ? "1" : "0"),
            new("nemu.ro.product.locale", locale),
            new("nemud.resolution_x", resolutionWidth.ToString(CultureInfo.InvariantCulture)),
            new("nemud.resolution_y", resolutionHeight.ToString(CultureInfo.InvariantCulture)),
            new("nemu.ro.sf.lcd_density", instance.FramebufferDpi.ToString(CultureInfo.InvariantCulture)),
            new("persist.tab_manager.use_vaddress", "true"),
            new("nemu.ro.product.board", instance.PhoneMiit),
            new("nemu.ro.product.name", instance.PhoneMiit),
            new("nemu.ro.product.device", instance.PhoneMiit),
            new("nemu.ro.boot.baseband", instance.PhoneBrand),
            new("nemu.ro.boot.hardware", instance.PhoneMiit),
            new("nemu.ro.build.product", instance.PhoneMiit),
            new("nemu.ro.product.brand", instance.PhoneBrand),
            new("nemu.ro.product.model", instance.PhoneMiit),
            new("nemu.ro.product.manufacturer", instance.PhoneBrand),
            new("nemu.ro.board.platform", instance.PhoneBrand),
            new("nemu.ro.hardware", instance.PhoneMiit),
            new("nemud.device.id", instance.PhoneImei),
            new("nemud.device.line1num", ""),
            new("nemud.player_uuid", identity.UserUuid),
            new("nemud.player_user_id", ""),
            new("nemud.player_version", AppVersion),
            new("nemud.player_engine", PlayerEngine),
            new("nemud.player_package", PlayerPackage),
            new("nemud.player_channel", PlayerChannel),
            new("nemud.player_fchannel", PlayerChannel),
            new("nemud.player_publish_store", "false"),
            new("nemud.player_architecture", "arm64"),
            new("nemud.player_usage", "0"),
            new("nemud.player_language", localeUnderscore),
            new("nemud.player_country", localeUnderscore),
            new("nemud.player_token", identity.AppToken),
            new("nemud.player_mpid", instance.Id.ToString(CultureInfo.InvariantCulture)),
            new("nemud.display_cutout", instance.DisplayCutout.ToString(CultureInfo.InvariantCulture)),
        ];

        var builder = new StringBuilder();
        foreach ((string name, string value) in properties)
        {
            builder.Append(name.Length.ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(name);
            builder.Append(value.Length.ToString("X2", CultureInfo.InvariantCulture));
            builder.Append(value);
        }

        return builder.ToString();
    }

    private static string ReadOrCreateGuid(string path)
    {
        if (File.Exists(path))
        {
            string existing = File.ReadAllText(path).Trim();
            if (Guid.TryParse(existing, out _))
            {
                return existing;
            }
        }

        string value = Guid.NewGuid().ToString();
        File.WriteAllText(path, value);
        return value;
    }
}

internal readonly record struct RendererIdentity(
    string AppToken,
    string UserUuid,
    string ThumbnailSurfaceName
);
