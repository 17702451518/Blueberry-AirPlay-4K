# 第三方组件与来源

蓝莓 AirPlay 4K 的源码仓库主要包含独立 WPF 控制台；完整 Windows Release 另外聚合并分发上游开源二进制。每个第三方组件继续适用其自身许可证、版权声明和免责声明。

## 接收核心

| 组件 | 本发布使用版本 | 来源 | 许可证 |
| --- | --- | --- | --- |
| uxplay-windows | 2.0.0.1736（Release commit `43cf903`） | <https://github.com/leapbtw/uxplay-windows/releases/tag/2.0.0.1736> | GPL-3.0 |
| UxPlay | 1.73.6（tag commit `21eef8d`） | <https://github.com/FDH2/UxPlay/releases/tag/v1.73.6> | GPL-3.0 |

上游官方 `uxplay-windows.zip` 大小为 113,529,789 字节，SHA-256 为：

```text
9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18
```

上述原样二进制说明适用于 v3.1.x 及早期预览包。v3.2.0-preview.5 接收核心以 uxplay-windows 提交 `f76fe48400916449fd601b1c0444021aaf517082` 及其固定 libuxplay 子模块重新构建，加入本仓库 `native/` 下的窗口管理补丁；Qt、GStreamer 与关联依赖由 MSYS2 UCRT64 获取，具体包版本保存在 `runtime/resources/build-manifest.json`。Bonjour 与 BLE 可执行文件沿用原运行包，不更改字节。不能将新版本描述为上游未经修改的官方二进制。

## 运行依赖

- GStreamer 及插件：LGPL 或各插件声明的许可证，<https://gstreamer.freedesktop.org/documentation/frequently-asked-questions/licensing.html>
- Apple mDNSResponder：Apache-2.0 与 BSD-3-Clause，<https://github.com/apple-oss-distributions/mDNSResponder/blob/main/LICENSE>
- Qt：不同模块适用 LGPL/GPL 或商业许可，<https://www.qt.io/licensing/open-source-obligations>
- FFmpeg：LGPL/GPL 取决于具体构建选项，<https://ffmpeg.org/legal.html>

完整包还包含编解码器、图像、TLS、字体和压缩库等传递依赖。发布包保留上游提供的 `LICENSE.rtf`，该文件是当前二进制集合最接近的上游组件清单；上表不是穷尽性法律意见。

## 源码与再分发

对应源码入口见 `SOURCE_CODE.md`。重新分发 Release 时，不得删除 `LICENSE`、`LICENSE.rtf`、本文件或源码获取说明，并应确认对应源码在与二进制等价可访问的位置持续可用。

## 商标与素材

Apple、AirPlay、iPhone、iPad 及其他名称可能是其各自所有人的商标。项目使用这些名称仅为描述兼容性，不表示官方授权、隶属或背书。项目图标是重新绘制的原创屏幕共享符号，详见 `ASSET_NOTICE.md`。
