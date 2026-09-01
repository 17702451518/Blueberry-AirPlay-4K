# 蓝莓 AirPlay 4K

> 面向 Windows 10/11 的中文 AirPlay 接收控制台，基于 `uxplay-windows` / `UxPlay`，提供 HEVC 4K60、低延迟模式与 D3D11 渲染。

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Release](https://img.shields.io/badge/release-v3.1.0-0891B2)](RELEASE_NOTES.md)
[![License](https://img.shields.io/badge/license-GPL--3.0--only-blue)](LICENSE)

蓝莓 AirPlay 4K 是一个独立的 WPF 控制台。它不重新实现 AirPlay 协议，而是为开源接收核心提供中文界面、经过实机验证的画质预设、D3D11 渲染配置、进程管理和当前用户登录预启动。

本项目与 Apple Inc. 无隶属、授权或赞助关系；“AirPlay”“iPhone”“iPad”“Apple”是其各自权利人的商标。

## 下载与使用

普通用户无需克隆源码或安装 Visual Studio：

1. 在 GitHub 的 **Releases** 页面下载 `Blueberry-AirPlay-4K-v3.1.0-win64.zip` 和 `SHA256SUMS.txt`。
2. 校验 ZIP 的 SHA-256，然后完整解压到一个可写目录；不要只从压缩包预览界面直接运行。
3. 确认系统已安装 [.NET 8 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/8.0)。
4. 运行 `蓝莓AirPlay4K控制台.exe`，选择模式并点击“应用并启动”。
5. 让 iPhone/iPad 与电脑处于同一局域网，在控制中心打开“屏幕镜像”，选择以“蓝莓投屏”开头的接收器。

首次启动时，Windows 防火墙可能询问是否允许网络访问。项目没有代码签名证书，SmartScreen/安全软件也可能显示未知发布者；请先核对 Release 校验值，不要为运行本项目关闭 Windows Defender。

## 功能

- HEVC 4K60 标准同步与低延迟互动模式
- 4K30、2K60、1080P60 H.264 兼容回退
- 固定 D3D11 渲染路径，规避部分设备上的 D3D12 初始化或黑屏问题
- 中文运行状态、模式说明、启动/重启、停止和打开目录
- 当前 Windows 用户登录后预启动
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

这些选项是接收端向发送端提出的能力与输出上限，不保证 iPhone 在每个场景都产生原生 4K60 流。实际分辨率、帧率和码率仍由 iOS、内容类型、无线网络、解码器与显示链路动态决定；项目没有提供一个不能约束 iPhone 编码器的“固定码率”伪开关。

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

关闭“登录后预启动”会删除最后一项。卸载时可在接收器退出后删除解压目录，并按需删除上述当前用户配置。

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
