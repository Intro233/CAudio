# CAudio

CAudio 是一个面向 Unity 的轻量音频系统，用数据驱动的 `AudioDatabase` 管理 SFX、UI、Voice、Ambience、Music、3D 音效、Mixer 音量控制、播放限制和配置校验。

当前版本：`0.3.0`

## 安装

### Package Manager Git URL

在 Unity 的 `Window/Package Manager` 中点击 `+`，选择 `Add package from git URL...`，输入：

```text
https://github.com/<user>/<repo>.git?path=/Assets/CAudio#master
```

`?path=/Assets/CAudio` 很重要，因为当前仓库仍然是一个完整 Unity 工程，真正的 UPM 包根目录在 `Assets/CAudio`。

### 直接复制

也可以把 `Assets/CAudio` 复制到目标项目的 `Assets` 目录。

## 功能

- 统一播放入口：`AudioManager.Play`、`PlaySfx`、`PlayUi`、`PlayVoice`、`PlayAmbience`、`PlayMusic`。
- 数据驱动配置：支持内嵌 `AudioCueData` 和独立 `AudioCueAsset`。
- Clip 选择策略：随机、加权随机、顺序播放、洗牌播放、避免连续重复。
- 播放变化：音量范围、音调范围、淡入淡出、延迟、循环、优先级、同通道替换。
- 空间音频：支持世界坐标播放和跟随目标播放。
- 批量控制：按 Key、Key 前缀、Channel、Group、Handle 或 All 停止。
- 音乐能力：音乐播放、Crossfade、播放队列。
- Mixer 集成：通道到 `AudioMixerGroup` 映射、暴露音量参数、Snapshot 过渡、Voice Ducking。
- 稳定性策略：Cue Cooldown、最大同时播放数、播放失败结果、音源池上限、可取消异步请求。
- 资源扩展：`IAudioClipProvider` 支持自定义资源系统，内置直连 Clip Provider，并提供可选 Addressables Provider。
- 编辑器工具：数据库窗口、校验、筛选、拖拽导入、批量编辑、自定义 Cue Drawer。

## 目录

```text
Runtime/                 运行时代码与 CAudio.Runtime.asmdef
Runtime/Providers/       可选 Provider，例如 Addressables
Editor/                  编辑器窗口、Drawer 与 CAudio.Editor.asmdef
Database/                示例或本地数据库资产
Samples/                 示例场景、示例脚本与说明
Tests/EditMode/          EditMode 测试与 CAudio.EditModeTests.asmdef
```

## 快速接入

1. 打开 Unity 菜单 `CAudio/Audio Database`。
2. 点击 `新建` 创建 `AudioDatabase.asset`。
3. 点击 `添加条目`，设置 `Key`，例如 `ui_click`。
4. 在条目的 Clip 列表里添加一个 `AudioClip`。
5. 在启动场景放一个空物体，挂载 `AudioSystemBootstrap`，把数据库拖到 `Database` 字段。
6. 在代码里播放：

需要快速建立通道总线时，可以在 `CAudio/Audio Database` 窗口的 `总线配置` 区域点击 `CAudio预设`。窗口会先弹出确认框，确认后自动创建预设 `AudioMixer`，并重建 Master、Music、Sfx、Voice、Ambience、Ui、Custom 总线。

```csharp
using CAudio;
using UnityEngine;

public sealed class AudioExample : MonoBehaviour
{
    public void PlayClick()
    {
        AudioManager.PlayUi("ui_click");
    }
}
```

## 可选依赖

- Addressables Provider 需要安装 `com.unity.addressables`。安装后 `CAudio.Runtime.asmdef` 会通过 `versionDefines` 自动启用 `CAUDIO_ADDRESSABLES`。
- EditMode 测试需要 `com.unity.test-framework`。

## 示例

包内提供 `功能展示` 示例。通过 Package Manager 导入包后，在 Samples 区域点击该示例的 `Import`，然后打开导入出的 `CAudioFeatureShowcase` 场景运行即可。

## 许可

MIT License
