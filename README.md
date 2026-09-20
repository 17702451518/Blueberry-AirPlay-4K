# 蓝莓 AirPlay 4K

> 将 iPhone 或 iPad 的屏幕无线显示到 Windows 电脑。提供中文界面、4K60 预设、低延迟模式和一键启动，无需手动填写底层参数。

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Release](https://img.shields.io/github/v/release/17702451518/Blueberry-AirPlay-4K)](https://github.com/17702451518/Blueberry-AirPlay-4K/releases/latest)
[![License](https://img.shields.io/badge/license-GPL--3.0--only-blue)](LICENSE)

<img width="1624" height="1320" alt="image" src="https://github.com/user-attachments/assets/b5452e94-cf21-40e1-978d-d36755272a59" />


## 这个软件是做什么的？

蓝莓 AirPlay 4K 是一款安装在 **Windows 电脑端的苹果设备投屏工具**。你在手机上打开应用、翻看照片或进行操作时，电脑上的投屏窗口会同步显示手机画面。这就是“屏幕镜像”：操作仍在手机上完成，电脑负责接收和显示。

例如，你可以在讲解手机应用时，让旁边的人通过电脑大屏看清操作；也可以把手机里的照片、演示内容或游戏画面显示到电脑上。

使用时，手机和电脑连接同一个路由器的网络，打开电脑端软件，再从 iPhone/iPad 控制中心的“屏幕镜像”中选择接收器即可。**不需要连接手机数据线，也不需要在手机上安装本项目的配套应用。** 电脑可以连接 Wi-Fi，也可以通过网线连接同一路由器。

## 它能做什么，不能做什么？

- **无线投屏：** 把 iPhone/iPad 的屏幕画面显示在 Windows 10/11 电脑上，适合操作演示、内容分享和大屏查看。
- **选择清晰度和流畅度：** 提供 1080P、2K、4K 等预设，以及最高 60 帧/秒的请求选项。分辨率影响画面细节，帧率影响运动是否流畅。
- **减少操作到画面显示的等待：** 提供低延迟模式，但无线投屏仍会有延迟，不承诺与手机屏幕完全同时显示。
- **中文操作界面：** 用按钮切换模式、启动和停止投屏，不需要了解底层命令。

它**不能用电脑鼠标或键盘控制 iPhone，也不提供录屏功能**；不是把电脑画面传到手机的工具，也不面向 Android 手机。

名称中的“4K”表示提供了相应的画质请求选项，**不代表任何手机、任何内容都能达到原生 4K60**。实际效果取决于手机、网络和电脑性能；把低清内容投到大屏上也不会自动增加原本不存在的细节。

## 本项目与上游软件的关系

实际接收手机画面的功能来自开源项目 `uxplay-windows` / `UxPlay`。本项目在它们的基础上增加中文操作界面、画质预设、启动与停止管理，并整理成便于下载使用的完整包；没有自行重写投屏协议，也没有修改上游接收程序及其依赖。

你不需要了解这些技术就能使用。开发者可以在下文查看工作原理、源码构建和第三方许可证。本项目不是 Apple 官方产品，与 Apple Inc. 无隶属、授权或赞助关系。

## 下载与使用

只想使用软件，请下载 Release 中的完整 ZIP 包，不要下载页面自动生成的 `Source code` 源码包。无需安装编程工具：

1. 打开 [Releases 页面](https://github.com/17702451518/Blueberry-AirPlay-4K/releases)，在 **Assets（附件）** 下载名称以 `Blueberry-AirPlay-4K-` 开头、以 `-win64.zip` 结尾的完整包，以及同版本的 `SHA256SUMS.txt`。请勿下载自动生成的 `Source code` 源码包。
2. 校验 ZIP 的 SHA-256，然后完整解压到一个可写目录；不要从压缩包预览界面直接运行，也不要只移动其中某一个 `.exe` 或 DLL。
3. 确认系统已安装 [.NET 8 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/8.0)。这是电脑运行本软件需要的微软组件；请选择 **Desktop Runtime**，不是 SDK 或 ASP.NET Core Runtime。
4. 在新版目录根部运行 `蓝莓投屏.exe`；它会启动 `app` 目录中的中文控制台。选择模式后点击“应用并启动”。旧版平铺包仍可直接运行 `蓝莓AirPlay4K控制台.exe`。
5. 让 iPhone/iPad 与电脑处于同一局域网，在控制中心打开“屏幕镜像”，选择以“蓝莓投屏”开头的接收器。

首次启动时，Windows 防火墙可能询问是否允许网络访问。项目没有代码签名证书，SmartScreen/安全软件也可能显示未知发布者；请先核对 Release 校验值，不要为运行本项目关闭 Windows Defender。

## 技术功能概览

- 实验性 2K120 / 4K120 帧率请求。该选项只请求最高 120 FPS，实际帧率由发送设备、iOS、网络和电脑决定，详见 [高帧率与任务栏说明](docs/EXPERIMENTAL-120FPS.md)
- 控制台运行时，将同一程序包中的投屏窗口归为同一任务栏应用组；是否合并由 Windows 任务栏设置决定，程序不会修改全局系统设置
- HEVC 4K60 标准同步与低延迟互动模式
- 4K30、2K60、1080P60 H.264 兼容回退
- 固定 D3D11 渲染路径，规避部分设备上的 D3D12 初始化或黑屏问题
- 中文运行状态、模式说明、启动/重启、停止和打开目录
- 可选的后台投屏核心登录启动（默认关闭，需明确确认）
- 原子写入一行 UxPlay 参数，不生成历史配置副本
- 只管理当前发布目录中的接收器进程，不结束其他目录的实例
- 不读取或保存 Token、Cookie、账号密码、浏览器数据、投屏内容或运行历史

## 模式说明

| 模式 | 请求参数 | 适用场景 | 取舍 |
| --- | --- | --- | --- |
| 4K60 标准同步 | HEVC、3840×2160、60 FPS、时间戳同步 | 日常投屏、视频观看 | 音画同步优先 |
| 4K60 低延迟互动 | HEVC、3840×2160、60 FPS、`-vsync no` | 操作演示、游戏、实时交互 | 延迟更低，音画同步可能变弱 |
| 4K30 稳定兼容 | HEVC、3840×2160、30 FPS 上限 | 4K60 不稳定时 | 降低帧率负载 |
| 2K60 中间档 | HEVC、2560×1440、60 FPS | 网络或显卡余量有限时 | 清晰度与流畅度折中 |
| 1080P60 H.264 回退 | H.264、1920×1080、60 FPS | HEVC 兼容性排查 | 兼容性优先 |
| 2K120 实验性 | HEVC、2560×1440、最高 120 FPS 请求 | 高刷新率设备验证 | 实际帧率不保证，低延迟优先 |
| 4K120 实验性 | HEVC、3840×2160、最高 120 FPS 请求 | 高性能设备与网络验证 | 负载高，实际帧率不保证 |

这些选项是接收端向发送端提出的能力与输出上限，不保证 iPhone 在每个场景都产生原生 4K60 流。实际分辨率、帧率和码率仍由 iOS、内容类型、无线网络、解码器与显示链路动态决定；本项目不提供强制固定手机编码码率的功能。

## 新版目录结构

从 v3.2.0-preview.2 起，发行包把启动入口、界面程序和底层依赖分开存放，根目录不再堆满 DLL。请完整保留目录结构：

```text
蓝莓 AirPlay 4K/
├─ 蓝莓投屏.exe        启动入口
├─ README.md           快速使用说明
├─ LICENSE             控制台许可证
├─ app/                中文控制台及 .NET 运行文件
├─ runtime/            UxPlay、Qt、GStreamer 与编解码依赖
└─ docs/               更新说明、来源与第三方许可证
```

`runtime` 里的 DLL、子目录和可执行文件是投屏核心运行所需文件，并非多余缓存；不要为了“精简”而删除或拆开移动。此调整降低了根目录杂乱度，不代表删减了经验证的解码依赖。

## 推荐环境

- Windows 10/11 x64
- .NET 8 Desktop Runtime x64
- 支持 D3D11 的显卡与较新的显卡驱动
- iPhone/iPad 与电脑在同一局域网，网络允许 mDNS/DNS-SD
- 电脑优先使用有线网络；移动设备优先连接信号良好的 5 GHz 或 6 GHz Wi-Fi

4K60 对网络稳定性和硬件解码能力要求明显高于 1080P。路由器的客户端隔离、访客网络、VPN、第三方防火墙和跨 VLAN 配置都可能导致“能看到设备但无画面”或无法发现设备。

## 工作方式

```text
iPhone / iPad
    │ AirPlay 镜像流 + DNS-SD 发现
    ▼
uxplay-windows 2.0.0.1736 / UxPlay 1.73.6
    │ GStreamer 解码与 D3D11 输出
    ▼
Windows 显示窗口

蓝莓 AirPlay 4K 控制台
    ├─ 写入当前模式参数
    ├─ 设置 D3D11 与 BLE 发现选项
    └─ 启动、停止并观察同目录接收核心
```

控制台写入：

- `%APPDATA%\leapbtw\uxplay-windows\arguments.txt`
- `HKCU\Software\leapbtw\uxplay-windows`
- 可选：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\BlueberryAirPlay4K`

开机启动默认关闭。启用后 Windows 直接启动后台 `uxplay-windows` 核心，并不是中文控制台。
v3.1.1 起，启用前需再次确认；“关闭本程序所有目录的开机启动”按钮只删除本程序拥有的 `BlueberryAirPlay4K` 登记，不影响其他软件、画质设置或当前投屏。
界面也会显示旧目录或异常登记，不因搬动文件夹而误报未登记。Windows 任务管理器的禁用状态与登记是否存在是两回事。
使用旧版时，可在 Windows“设置 → 应用 → 启动”或任务管理器中关闭 `BlueberryAirPlay4K`。不要删除整个 `Run` 注册表键。详细行为和迁移说明见 [开机启动说明](docs/AUTOSTART.md)。
卸载前建议先点击关闭按钮，再退出接收器、删除解压目录，并按需删除上述当前用户配置。

## 常见问题

### 手机能看到接收器，但连接后没有画面

先切换到“1080P60 H.264 回退”排查 HEVC/D3D11 能力；确认防火墙允许接收器、系统显卡驱动正常，且没有其他目录中的 `uxplay-windows.exe` 占用端口。访客 Wi-Fi、AP 隔离或 VPN 也可能允许发现却阻断后续媒体流。

### 4K 或自定义分辨率没有画面

并非所有 iPhone、iOS 版本、内容和网络都接受任意接收端能力组合。优先使用内置预设，不要把任意宽高、刷新率和编码器组合等同于底层一定支持。

### 第一帧明显慢，后续正常

新会话需要完成 AirPlay 协商、手机编码器启动、HEVC/H.264 解码器创建和第一张纹理上传。预启动接收核心能减少程序冷启动，但不能消除每次连接的全部首帧初始化时间。

### 白色、色彩或清晰度不一致

先关闭 Windows/iPhone 的 HDR、夜览、色彩增强或显卡后处理作对比。色差不只由码率决定，也可能来自色域、传递函数、有限/全范围和显示器配置。当前预设不强制注入未经验证的 BT.709、sRGB 或 HDR 参数。

更多细节见 [性能与画质说明](docs/PERFORMANCE.md)。

## 从源码构建

要求 Windows 和 .NET 8 SDK：

```powershell
dotnet restore .\src\BlueberryAirPlay4K\BlueberryAirPlay4K.csproj
dotnet build .\src\BlueberryAirPlay4K\BlueberryAirPlay4K.csproj -c Release --no-restore
```

控制台运行时需要和 `uxplay-windows.exe` 及其完整依赖位于同一目录。大型第三方二进制不进入 Git 历史，而是仅在 GitHub Release 的便携包中分发。

## 项目结构

```text
Blueberry-AirPlay-4K/
├─ .github/                   CI、Issue 与 PR 模板
├─ docs/                      架构、性能和发布文档
├─ src/BlueberryAirPlay4K/    WPF 控制台源码
├─ CHANGELOG.md
├─ ASSET_NOTICE.md
├─ CODE_OF_CONDUCT.md
├─ CONTRIBUTING.md
├─ LICENSE                    GPL-3.0-only
├─ RELEASE_NOTES.md
├─ SECURITY.md
└─ THIRD_PARTY_NOTICES.md
```

## 来源、修改与可复现校验

发布包以 [`leapbtw/uxplay-windows` 2.0.0.1736](https://github.com/leapbtw/uxplay-windows/releases/tag/2.0.0.1736) 为接收核心，其中包含 [`FDH2/UxPlay` 1.73.6](https://github.com/FDH2/UxPlay/releases/tag/v1.73.6)。本项目没有修改上游 `uxplay-windows.exe` 或其依赖，只新增独立的中文 WPF 控制台、参数预设和说明文档。

用于制作 v3.1.0 的上游官方 `uxplay-windows.zip`：

```text
文件大小：113,529,789 字节
SHA-256：9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18
```

SHA-256 是公开的文件指纹，用来检查下载是否完整，不是 Token、Cookie、密码或密钥。发布包自身的校验值放在同一 Release 的 `SHA256SUMS.txt`，并记录于该版本发布说明。

更完整的来源与许可证边界见 [第三方组件说明](THIRD_PARTY_NOTICES.md) 和 [源码获取说明](SOURCE_CODE.md)。

## 隐私与安全

控制台不包含遥测、账号登录、广告、远程更新器或录屏功能，也不会扫描浏览器或账号数据。安全边界、公开报告方式和发布前凭据检查见 [SECURITY.md](SECURITY.md)。

## 许可证与声明

本项目控制台源码和原创屏幕共享图标按 [GNU GPL v3.0 only](LICENSE) 发布。Release 中的 UxPlay、GStreamer、mDNSResponder、Qt、FFmpeg 等第三方组件分别受其自身许可证约束，发布包保留上游 `LICENSE.rtf`；不要把根目录 GPL 误解为对第三方商标或每个独立库重新授权。美术素材说明见 [ASSET_NOTICE.md](ASSET_NOTICE.md)。

本软件按“原样”提供，不承诺适合特定用途。使用者应遵守所在地法律、网络政策、内容版权和设备条款。项目名称不是 Apple 官方产品，也不暗示与上游项目作者存在背书关系。

## 参与贡献

欢迎提交可复现的 Bug、兼容性结果、文档修正和小范围 PR。提交前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md) 与 [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)；安全漏洞请按 [SECURITY.md](SECURITY.md) 私下报告，不要在公开 Issue 中粘贴真实凭据或私人网络信息。

## 致谢

感谢 [`leapbtw/uxplay-windows`](https://github.com/leapbtw/uxplay-windows)、[`FDH2/UxPlay`](https://github.com/FDH2/UxPlay)、[GStreamer](https://gstreamer.freedesktop.org/) 与相关自由软件贡献者。本项目是在这些工作之上提供的 Windows 中文控制与打包层。
