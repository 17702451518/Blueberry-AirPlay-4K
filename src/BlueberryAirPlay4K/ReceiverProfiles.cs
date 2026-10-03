namespace BlueberryAirPlay4K;

internal sealed record ReceiverProfile(string Id, string Title, string ReceiverName, string Arguments, string Summary);

internal static class ReceiverProfiles
{
internal static readonly IReadOnlyDictionary<string, ReceiverProfile> All =
        new Dictionary<string, ReceiverProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["2k120-experimental"] = new(
                "2k120-experimental", "2K120 实验性高帧率", "蓝莓投屏-2K120-实验",
                "-n 蓝莓投屏-2K120-实验 -nh -h265 -s 2560x1440@120 -fps 120 -vsync no -nofreeze -FPSdata",
                "实验性 HEVC 2K120 请求 · 实际帧率取决于发送设备及链路条件"),
            ["4k120-experimental"] = new(
                "4k120-experimental", "4K120 实验性高帧率", "蓝莓投屏-4K120-实验",
                "-n 蓝莓投屏-4K120-实验 -nh -h265 -s 3840x2160@120 -fps 120 -vsync no -nofreeze -FPSdata",
                "实验性 HEVC 4K120 请求 · 对设备性能及网络带宽要求较高"),
            ["4k60-sync"] = new(
                "4k60-sync",
                "4K60 标准同步",
                "蓝莓投屏-4K60",
                "-n 蓝莓投屏-4K60 -nh -h265 -s 3840x2160@60 -fps 60 -vsync -nofreeze -FPSdata",
                "HEVC 4K60请求 · D3D11 · 时间戳音画同步"),
            ["4k60-low"] = new(
                "4k60-low",
                "4K60 低延迟互动",
                "蓝莓投屏-4K60-低延迟",
                "-n 蓝莓投屏-4K60-低延迟 -nh -h265 -s 3840x2160@60 -fps 60 -vsync no -nofreeze -FPSdata",
                "HEVC 4K60请求 · D3D11 · 到帧尽快显示"),
            ["4k30"] = new(
                "4k30",
                "4K30 稳定兼容",
                "蓝莓投屏-4K30",
                "-n 蓝莓投屏-4K30 -nh -h265 -s 3840x2160@60 -fps 30 -vsync -nofreeze -FPSdata",
                "HEVC 4K30请求 · D3D11 · 稳定优先"),
            ["2k60"] = new(
                "2k60",
                "2K60 中间档",
                "蓝莓投屏-2K60",
                "-n 蓝莓投屏-2K60 -nh -h265 -s 2560x1440@60 -fps 60 -vsync -nofreeze -FPSdata",
                "HEVC 2K60请求 · D3D11 · 中等网络负载"),
            ["1080p60"] = new(
                "1080p60",
                "1080P60 H.264回退",
                "蓝莓投屏-1080P60",
                "-n 蓝莓投屏-1080P60 -nh -s 1920x1080@60 -fps 60 -vsync -nofreeze -FPSdata",
                "H.264 1080P60请求 · D3D11 · 兼容排查"),
        };
}
