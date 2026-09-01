# 蓝莓 AirPlay 4K v3.1.0

首个准备公开发布的完整版本。

## 新增与改进

- 新增 4K60 HEVC 标准同步与低延迟互动预设；
- 新增 4K30、2K60 和 1080P60 H.264 回退；
- 固定 D3D11 渲染路径，改善部分设备的黑屏与冻结；
- 修复已连接但无画面的核心启动/参数链路；
- 重新设计中文 WPF 界面，移除一级界面图标区域，修复小窗口滚动抖动并适配缩放；
- 使用项目原创的蓝色屏幕共享图标，移除旧头像及来源不明的下载素材；
- 原子写入当前参数，不保留旧配置历史；
- 限定进程管理范围，不影响其他目录的软件；
- 清理调试符号、绝对构建路径、临时缓存和旧发布包；
- 完整包加入来源、隐私、安全、性能、源码获取和校验说明。

## 下载

下载 `Blueberry-AirPlay-4K-v3.1.0-win64.zip` 与 `SHA256SUMS.txt`，核对校验值后完整解压。需要 Windows 10/11 x64 和 .NET 8 Desktop Runtime x64。

## 已知边界

- 4K60 是能力请求和接收端上限，实际流参数由 iOS 和网络动态决定；
- 第一次连接仍可能有 AirPlay/编码器/解码器初始化延迟；
- 低延迟模式可能牺牲严格音画同步；
- 程序未签名，Windows 可能显示未知发布者；
- 本版本不提供从 Windows 使用鼠标控制 iPhone 的功能。

## 上游

- uxplay-windows 2.0.0.1736：<https://github.com/leapbtw/uxplay-windows/releases/tag/2.0.0.1736>
- UxPlay 1.73.6：<https://github.com/FDH2/UxPlay/releases/tag/v1.73.6>

发布包 SHA-256 以同一 Release 中的 `SHA256SUMS.txt` 为准。
