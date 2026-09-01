# 发布维护指南

## GitHub 仓库设置

- 建议仓库名：`Blueberry-AirPlay-4K`
- About：`Windows 上的中文 AirPlay 接收控制台，基于 uxplay-windows/UxPlay，提供 HEVC 4K60、低延迟模式与 D3D11 渲染。`
- Topics：`airplay`、`airplay-receiver`、`screen-mirroring`、`ios`、`windows`、`uxplay`、`wpf`、`dotnet`、`hevc`、`4k`、`d3d11`、`gstreamer`
- 默认分支：`main`
- 建议启用 Issues、Discussions、Secret scanning 和 Dependabot alerts。

## 发布检查清单

1. 确认所有图片、图标、字体和文本均有公开再发布权。
2. `dotnet restore`，随后执行 Release 构建和测试。
3. 确认 4K60 标准、低延迟及 1080P 回退模式可启动。
4. 确认 `_airplay._tcp` / `_raop._tcp` 可正常发现，且没有其他目录的实例冲突。
5. 清理 `bin`、`obj`、PDB、临时包、个人绝对路径和本地缓存。
6. 扫描 Token、Cookie、私钥、证书、`.env`、连接串和构建路径。
7. 完整包必须保留 `LICENSE.rtf`、`README.md`、`RELEASE_NOTES.md`、`SOURCE_CODE.md` 和第三方说明。
8. 从空目录解压 ZIP，运行一次关键路径并检查文件名编码。
9. 生成 SHA-256；不要在 ZIP 内写 ZIP 自身校验值。
10. 创建带注释标签 `vX.Y.Z`，使用 `RELEASE_NOTES.md` 作为 Release 正文，上传 ZIP、发布说明 Markdown 和 `SHA256SUMS.txt`。

不要把大型完整包、上游 DLL 或发布 ZIP 提交到 Git 历史；它们属于 GitHub Release Assets。
