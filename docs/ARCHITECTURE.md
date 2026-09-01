# 架构说明

蓝莓 AirPlay 4K 控制台是独立的 .NET 8 WPF 进程，不承载或转发媒体数据。

```text
WPF 控制台 ──写参数/注册表──► uxplay-windows ──调用──► UxPlay + GStreamer
     │                              │                         │
     └────仅管理同目录进程───────────┘                         └─► D3D11 窗口/音频
```

主要职责：

- `MainWindow.xaml`：中文界面与小窗口滚动布局；
- `MainWindow.xaml.cs`：模式定义、原子配置写入、注册表设置和同目录进程生命周期；
- `uxplay-windows.exe`：上游 AirPlay 接收、发现和媒体管线入口；
- UxPlay/GStreamer/Qt/mDNSResponder/FFmpeg：随 Release 便携包分发的第三方运行依赖。

控制台发现另一个目录的 `uxplay-windows.exe` 时会拒绝启动，并显示路径，避免端口或服务名称冲突。停止操作只针对可执行路径位于当前包目录下的接收器和蓝牙发现进程。
