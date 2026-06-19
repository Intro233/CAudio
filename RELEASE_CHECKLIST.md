# CAudio 发布检查清单

这份清单用于记录发布前需要确认的证据和步骤。

## 必需验证

- Unity Console 没有 C# 编译错误。
- `git diff --check` 通过。
- README、快速开始、变更日志、版本文件、示例、Runtime/Editor/Test 程序集定义均存在。
- `Assets/CAudio/package.json`、包内 README、包内变更日志和包内 License 均存在。
- 编辑器窗口可以打开，没有 GUILayout 状态错误。

## 验证命令

静态检查：

```powershell
git diff --check
```

UPM 包元数据检查：

```powershell
Get-Content "Assets\CAudio\package.json" -Raw | ConvertFrom-Json
```

Package Manager Git URL 格式：

```text
https://github.com/<user>/<repo>.git?path=/Assets/CAudio#master
```

说明：后续不再默认启动 Unity EditMode 测试。如需测试，应先确认 Unity 未打开当前项目，再由使用者明确要求执行。

## 包发布证据

- `Assets/CAudio/package.json` 定义 `com.caudio.core`，版本为 `0.3.0`。
- `Assets/CAudio/README.md`、`CHANGELOG.md`、`LICENSE.md` 和 `Documentation~/index.md` 均存在。
- 包可通过 `?path=/Assets/CAudio#master` 的 Git URL 安装。
- `Assets/CAudio/package.json` 注册了可导入示例 `功能展示`。
- Addressables 仍通过 `CAudio.Runtime.asmdef` 的 `versionDefines` 保持可选。

## 当前已验证

- `git diff --check` 通过。
- 早前临时项目副本 `C:\Users\Nagisa\AppData\Local\Temp\CAudio_FourBatchTest_20260619_081744` 曾成功编译 Runtime、Editor、Samples 和 EditMode Tests 程序集。
- 早前临时项目 EditMode 测试曾通过：28 total / 28 passed / 0 failed / 0 skipped。

## 当前注意事项

- 当前示例目录临时使用 `Assets/CAudio/Samples`，方便在本项目内直接测试。
- 发布为标准 UPM 包前，应按需要将示例目录改回 `Samples~`，并把 `package.json` 中的 sample path 改回 `Samples~/Feature Showcase`。
