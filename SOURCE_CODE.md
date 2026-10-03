# 对应源码获取说明

GitHub Release 的完整 Windows ZIP 包含项目控制台以及多个自由/开源第三方组件。

## 本项目控制台

与 `v3.1.0` 二进制对应的 WPF 源码位于本仓库同名 Git 标签 `v3.1.0`。GitHub Release 页面也会自动提供该标签的 `Source code (zip)` 与 `Source code (tar.gz)`。

## 接收核心

### v3.2.0-preview.6 显示控制版本

- [uxplay-windows 固定提交](https://github.com/leapbtw/uxplay-windows/tree/f76fe48400916449fd601b1c0444021aaf517082)，执行递归子模块初始化取得对应 libuxplay。
- 本项目新增源码、接入补丁与测试位于 [native/](native/README.md)，构建和部署脚本位于 `scripts/Build-DisplayRuntime.ps1`。
- Qt、GStreamer、FFmpeg 等使用 [MSYS2 MINGW-packages 源码与构建配方](https://github.com/msys2/MINGW-packages)。二进制的具体包版本记录在包内 `runtime/resources/build-manifest.json`，应使用同版本配方及上游源码重建；构建未启用 `-march=native`，不绑定开发电脑 CPU。
- Bonjour / BLE 沿用下述旧包，旧版本入口继续适用于这两部分。新核心与依赖不是原官方 ZIP 的字节副本。

### v3.1.x 原接收核心

- uxplay-windows 2.0.0.1736：<https://github.com/leapbtw/uxplay-windows/tree/2.0.0.1736>
- uxplay-windows 2.0.0.1736 Release：<https://github.com/leapbtw/uxplay-windows/releases/tag/2.0.0.1736>
- UxPlay 1.73.6：<https://github.com/FDH2/UxPlay/tree/v1.73.6>

上游官方二进制包的 SHA-256 为：

```text
9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18
```

## 其他组件

GStreamer、mDNSResponder、Qt、FFmpeg 及其依赖的项目主页与许可证列于 `THIRD_PARTY_NOTICES.md` 和发布包自带的 `LICENSE.rtf`。如果公开链接失效或你无法取得某个受 GPL/LGPL 约束组件的对应源码，请在仓库创建 Issue，维护者会提供等价的无偿获取方式。

本说明旨在让二进制接收者可以从与下载位置相邻的页面找到对应源码；它不替代各上游项目的许可证原文。
