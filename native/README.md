# Windows 显示控制接收核心

该目录包含蓝莓项目新增的 GPL-3.0-only 显示窗口，以及上游接入补丁。不是对原软件二进制的注入或修改。

`upstream-main.patch` 同时关闭新版上游默认开启的磁盘会话日志，保持项目不保存运行历史的策略；不会删除其他版本的日志。

`upstream-ui.patch` 汉化接收核心的设置窗口与托盘菜单，并移除默认禁用日志的入口。

固定源码版本：

- uxplay-windows：`f76fe48400916449fd601b1c0444021aaf517082`（https://github.com/leapbtw/uxplay-windows/tree/f76fe48400916449fd601b1c0444021aaf517082）
- libuxplay：`79c620d0e3cd7c503418e1e39bf7b00a1cb16f24`（该上游提交的子模块版本）。
- GStreamer、Qt 等具体构建版本记录在完整包 `runtime/resources/build-manifest.json` 中。

## 构建

1. 克隆上述 uxplay-windows 提交并执行 `git submodule update --init --recursive`。
2. 准备上游 Bonjour SDK。也可从相同版本 `dnssd.dll` 的导出表生成 MinGW 导入库，搭配 Apple mDNSResponder `rel/mDNSResponder-2881` 的 `dns_sd.h`；不要混用其他 API 版本。
3. 安装 MSYS2 UCRT64：GCC、CMake、Qt6 base、libplist、json-glib、GStreamer base/good/bad/ugly/libav 和 python-gobject。
4. 运行 `scripts/Build-DisplayRuntime.ps1`，指定 `-Upstream`、`-OriginalRuntime`、`-OutputDirectory`、`-MsysRoot`。原运行目录只用于读取原版本 Bonjour、BLE 及许可证文件。输出必须是新目录。
5. 将输出作为 `scripts/Package.ps1 -DisplayRuntime` 参数；不能只替换控制台。

## 控制与测试

`display-mode.txt` 是包内的普通显示设置，不含凭据：`缩放方式 窗口状态 命令序号`。编号均为 0—2，顺序对应界面。每次选择更新序号；Esc 的窗口状态不会被旧命令重复覆盖。仅支持已标记 `display-control.version` 的核心。

尺寸从渲染端实际协商的 caps 获取，不使用 4K/2K 预设猜测。窗口线程采用 Per-Monitor-V2 DPI 感知。原始尺寸的渲染子窗口保持视频像素大小，超出父窗口区域时裁切可见区域并允许平移，不改变视频比例或解码分辨率。

`display_smoke.c` 使用本机 D3D11 和测试画面检查 36 个横竖屏组合、窗口区域、渲染矩形、暂停画面的实际像素及八个方向的等比拖动，不启用 AirPlay，不写手机或用户接收设置：

```sh
gcc -DBLUEBERRY_TEST -O2 native/display_window.c native/display_smoke.c \
  -o display-smoke.exe $(pkg-config --cflags --libs gstreamer-video-1.0) -lgdi32
./display-smoke.exe
```

测试不替代 iPhone/iPad 实机投屏、跨显示器 DPI、任务栏视觉行为以及色彩端到端验证。本次改变了接收核心和依赖版本，应作为预览版本发布，不能声称已经完成全部兼容性验证。
