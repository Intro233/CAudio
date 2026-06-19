# CAudio

CAudio 是一个面向 Unity 的轻量音频系统，用数据驱动的 `AudioDatabase` 管理 SFX、UI、Voice、Ambience、Music、3D 音效、Mixer 音量控制、播放限制和配置校验。它适合中小型项目，也适合作为已有音频资源管线前面的统一播放层。

当前版本：`0.3.0`

## 特性

- 统一播放入口：`AudioManager.Play`、`PlaySfx`、`PlayUi`、`PlayVoice`、`PlayAmbience`、`PlayMusic`。
- 数据驱动配置：支持内嵌 `AudioCueData` 和独立 `AudioCueAsset`。
- Clip 选择策略：随机、权重随机、顺序、洗牌、不立即重复。
- 播放变化：音量范围、音调范围、淡入淡出、延迟、循环、优先级、同通道替换。
- 空间音频：支持世界坐标播放和跟随目标播放。
- 播放控制：句柄停止、暂停、恢复、音量、音调、播放状态查询。
- 批量控制：按 Key、Key 前缀、Channel、Group、Handle 或 All 停止。
- 通道控制：Master、Music、Sfx、Voice、Ambience、Ui、Custom，支持音量、暂停/恢复、Mute、Solo。
- 音乐能力：音乐播放、Crossfade、播放队列。
- Mixer 集成：通道到 `AudioMixerGroup` 映射、暴露音量参数、参数平滑过渡、Snapshot 过渡、Voice Ducking。
- 稳定性策略：Cue Cooldown、最大同时播放数、播放失败结果、可取消异步请求。
- 资源扩展：`IAudioClipProvider` 支持自定义资源系统，内置直连 Clip Provider，并提供可选 Addressables Provider。
- 编辑器工具：数据库窗口、校验、筛选、复制、删除确认、从选中 `AudioClip` 批量导入、自定义 Cue Drawer。
- 工程结构：Runtime / Editor / Samples / EditMode Tests 使用 asmdef 分离。

## 目录结构

```text
Assets/CAudio/
  Runtime/                 运行时代码与 CAudio.Runtime.asmdef
  Runtime/Providers/       可选 Provider 示例，例如 Addressables
  Editor/                  编辑器窗口、Drawer 与 CAudio.Editor.asmdef
  Database/                示例或本地数据库资产
  Samples/                 示例场景、示例脚本与说明
  Tests/EditMode/          EditMode 测试与 CAudio.EditModeTests.asmdef
```

辅助文档：

- [QUICK_START.md](QUICK_START.md)
- [CHANGELOG.md](CHANGELOG.md)
- [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)
- [VERSION](VERSION)

## 安装

### Package Manager Git URL

当前仓库仍然是完整 Unity 工程，UPM 包根目录在 `Assets/CAudio`。在 Unity 的 `Window/Package Manager` 中点击 `+`，选择 `Add package from git URL...`，输入：

```text
https://github.com/<user>/<repo>.git?path=/Assets/CAudio#master
```

包内提供可导入示例 `Feature Showcase`。安装包后，可在 Package Manager 的 Samples 区域点击 `Import`。

### 直接复制

把 `Assets/CAudio` 复制到目标 Unity 项目的 `Assets` 目录即可。运行时核心不强依赖 Addressables。

推荐 Unity 版本：`2022.3 LTS`。当前仓库使用 `2022.3.62f3` 验证。

### 可选依赖

- EditMode 测试需要 `com.unity.test-framework`，当前项目使用 `1.1.33`。
- Addressables Provider 需要安装 `com.unity.addressables`。安装后 `CAudio.Runtime.asmdef` 会通过 `versionDefines` 自动启用 `CAUDIO_ADDRESSABLES`。

## 5 分钟接入

1. 打开 Unity 菜单 `CAudio/Audio Database`。
2. 点击 `新建` 创建 `AudioDatabase.asset`。
3. 点击 `添加条目`，设置 `Key`，例如 `ui_click`。
4. 在条目的 Clip 列表里添加一个 `AudioClip`。
5. 在启动场景放一个空物体，挂载 `AudioSystemBootstrap`，把数据库拖到 `Database` 字段。
6. 在代码里播放：

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

也可以完全用代码初始化：

```csharp
using CAudio;
using UnityEngine;

public sealed class GameAudioBootstrap : MonoBehaviour
{
    [SerializeField] private AudioDatabase database;

    private void Awake()
    {
        AudioManager.Initialize(database);
    }
}
```

## 数据配置

### AudioDatabase

`AudioDatabase` 是 CAudio 的主配置资产，包含：

- 内嵌 Cue 列表。
- 独立 `AudioCueAsset` 引用列表。
- 通道总线 `AudioBusData`。
- 调试配置 `AudioDebugSettings`。
- Mixer 与 Voice Ducking 配置 `AudioMixerControlSettings`。

查找 Cue 时 Key 不区分大小写。若存在重复 Key，缓存会保留先出现的配置，校验会报告重复。

### AudioCueData

每个 Cue 描述一次可播放音频配置：

| 字段 | 说明 |
| --- | --- |
| `Key` | 播放用唯一键，例如 `sfx_hit` |
| `DisplayName` | 编辑器显示名 |
| `Group` | 自定义分组，可用于 `StopGroup` |
| `Channel` | 默认通道 |
| `SelectionMode` | Clip 选择方式 |
| `Clips` | Clip 引用列表，支持直连 Clip 或 Address Key |
| `VolumeRange` / `PitchRange` | 每次播放随机倍率 |
| `Loop` | 是否循环 |
| `ReplaceSameChannel` | 播放前是否替换同通道旧声音 |
| `FadeInTime` / `FadeOutTime` | 默认淡入淡出时间 |
| `Cooldown` | 同 Key 冷却时间 |
| `MaxSimultaneous` | 同 Key 最大同时播放数，`0` 表示不限制 |
| `SpatialBlend` | `0` 为 2D，`1` 为 3D |
| `MinDistance` / `MaxDistance` | 3D 衰减距离 |
| `Priority` | Unity `AudioSource.priority` |
| `OutputGroup` | 覆盖输出 Mixer Group |

### Clip 选择方式

- `Random`：普通随机。
- `WeightedRandom`：按 `AudioClipOption.Weight` 加权随机。
- `Sequential`：顺序轮播。
- `Shuffle`：洗牌袋，尽量分散重复。
- `NoImmediateRepeat`：避免连续两次选到同一个 Clip。

## 常用 API

### 播放

```csharp
AudioManager.Play("sfx_hit");
AudioManager.PlaySfx("sfx_hit");
AudioManager.PlayUi("ui_click");
AudioManager.PlayVoice("voice_line");
AudioManager.PlayAmbience("ambience_forest");
AudioManager.PlayMusic("music_theme");
```

### 直连 Clip

```csharp
[SerializeField] private AudioClip clip;

private void PlayDirect()
{
    AudioManager.Play(clip);
}
```

### 临时播放参数

`AudioPlayOptions` 会在播放入口内部复制，`PlayAt`、`PlayMusic` 等 helper 不会污染调用方传入的 options 实例。

```csharp
AudioManager.PlaySfx("sfx_explosion", new AudioPlayOptions
{
    Volume = 0.8f,
    Pitch = 1.05f,
    FadeIn = 0.05f,
    FadeOut = 0.2f,
    Delay = 0.1f,
    Priority = 80
});
```

### 3D 与跟随目标

```csharp
AudioManager.PlayAt("sfx_explosion", transform.position);
AudioManager.PlayFollow("sfx_engine_loop", vehicleTransform, new AudioPlayOptions
{
    Loop = true,
    FadeIn = 0.3f,
    FadeOut = 0.5f
});
```

### 播放结果

如果需要区分失败原因，使用 `TryPlay`：

```csharp
AudioPlayResult result = AudioManager.TryPlay("sfx_hit");
if (!result.Success)
{
    Debug.LogWarning($"{result.FailureReason}: {result.Message}");
}
```

可能的失败原因包括：

- `MissingDatabase`
- `EmptyKey`
- `CueNotFound`
- `MissingCue`
- `MissingClip`
- `ProviderFailed`
- `Cancelled`
- `Cooldown`
- `MaxSimultaneous`
- `PoolLimitReached`

### 播放句柄

```csharp
AudioPlaybackHandle handle = AudioManager.PlayAmbience("ambience_loop", new AudioPlayOptions
{
    Loop = true,
    FadeIn = 0.5f,
    FadeOut = 0.5f
});

handle.SetVolume(0.6f);
handle.SetPitch(0.95f);
handle.Pause();
handle.Resume();
handle.Stop();
```

可读取：

- `IsPlaying`
- `IsStopped`
- `PlaybackKey`
- `PlaybackGroup`
- `PlaybackChannel`

### 停止与暂停

```csharp
AudioManager.Stop("sfx_hit");
AudioManager.StopByKeyPrefix("ui_");
AudioManager.StopGroup("combat");
AudioManager.StopChannel(AudioChannel.Sfx, fadeOutSeconds: 0.2f);
AudioManager.StopAll(fadeOutSeconds: 0.5f);

AudioManager.PauseAll();
AudioManager.ResumeAll();
AudioManager.PauseChannel(AudioChannel.Music);
AudioManager.ResumeChannel(AudioChannel.Music);
```

### 音乐

```csharp
AudioManager.PlayMusic("music_menu");
AudioManager.CrossfadeMusic("music_battle", 1.2f);
AudioManager.QueueMusic("music_victory", fadeSeconds: 0.8f);
AudioManager.ClearMusicQueue();
```

音乐 helper 会默认使用 `AudioChannel.Music` 并开启 `ReplaceSameChannel`。

### 通道音量、Mute、Solo

```csharp
AudioManager.SetMasterVolume(0.9f);
AudioManager.SetChannelVolume(AudioChannel.Sfx, 0.75f);
AudioManager.SetChannelMute(AudioChannel.Ui, true);
AudioManager.SetSoloChannel(AudioChannel.Music);
AudioManager.ClearSoloChannel();
```

如果数据库配置了 Mixer 暴露参数，音量会写入 Mixer 参数；否则会直接作用到播放中的 `AudioSource.volume`。

## Mixer 与 Ducking

在 `AudioDatabase` 的 Mixer 配置里可以设置：

- `Mixer`：目标 `AudioMixer`。
- `VolumeParameters`：通道到暴露音量参数的映射。
- `EnableVoiceDucking`：播放 Voice 时是否压低 Music。
- `DuckMusicVolume`：压低后的 Music 音量倍率。
- `DuckFadeSpeed`：压低/恢复速度。

常用接口：

```csharp
AudioManager.TransitionMixerParameter("MusicVolume", -12f, 0.5f);
AudioManager.TransitionToSnapshot(snapshot, 1.0f);
```

`AudioService.VolumeToDecibel(volume)` 可将线性音量转换为 dB，`0` 会映射为 `-80f`。

## 异步播放与资源 Provider

同步播放会先调用 `IAudioClipProvider.TryResolveClip`。异步播放会调用 `LoadClipAsync`，成功后才租用 `AudioSource`，因此加载失败或取消不会留下不可停止的句柄。

```csharp
AudioAsyncPlayRequest request = AudioManager.PlayAsync("sfx_remote", onComplete: result =>
{
    if (result.Success)
    {
        Debug.Log("Audio started.");
    }
});

request.Cancel();
```

### 自定义 Provider

```csharp
using System;
using CAudio;
using UnityEngine;

public sealed class MyAudioClipProvider : IAudioClipProvider
{
    public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
    {
        clip = null;
        return false;
    }

    public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
    {
        // 接入项目自己的资源系统。
    }
}
```

初始化时注入：

```csharp
AudioManager.SetClipProvider(new MyAudioClipProvider());
AudioManager.Initialize(database);
```

### Addressables

安装 `com.unity.addressables` 后，`AddressablesAudioClipProvider` 会在 `CAUDIO_ADDRESSABLES` define 下编译。Cue 的 Clip 引用可使用 `AddressKey`，然后：

```csharp
AudioManager.SetClipProvider(new AddressablesAudioClipProvider());
AudioManager.Initialize(database);
AudioManager.PlayAsync("sfx_addressable");
```

当前 Addressables 示例 Provider 会在播放生命周期结束时释放加载句柄；正式项目可在自定义 Provider 中进一步加入共享缓存、预加载或更细的引用计数策略。

如资源系统需要释放加载结果，可额外实现 `IAudioClipReleaseProvider`：

```csharp
public sealed class MyAddressProvider : IAudioClipProvider, IAudioClipReleaseProvider
{
    public bool TryResolveClip(AudioClipReference reference, out AudioClip clip)
    {
        clip = null;
        return false;
    }

    public void LoadClipAsync(AudioClipReference reference, Action<AudioClip> onSuccess, Action<string> onFailure)
    {
        // 加载并在成功时回调 onSuccess。
    }

    public void ReleaseClip(AudioClipReference reference, AudioClip clip)
    {
        // 释放对应资源句柄或减少引用计数。
    }
}
```

CAudio 会在播放结束、加载后取消、加载后被冷却/并发限制拒绝等路径调用释放接口。

## 音源池

`AudioDatabase` 包含 `AudioPoolSettings`：

- `PrewarmCount`：初始化时预热的 `AudioSource` 数量。
- `MaxSourceCount`：最大 `AudioSource` 数量，`0` 表示不限制。

当池已满时，播放会返回 `AudioPlayFailureReason.PoolLimitReached`，并记录警告日志。这样高频音效不会无限创建隐藏的 `AudioSource`。

## 编辑器工作流

打开菜单：

```text
CAudio/Audio Database
```

窗口支持：

- 创建 `AudioDatabase`。
- 添加内嵌 Cue。
- 创建独立 `AudioCueAsset` 并加入数据库引用。
- 从 Project 当前选中的 `AudioClip` 批量创建 Cue。
- 添加总线配置。
- 按通道筛选、按问题筛选、搜索 Key / DisplayName。
- 复制、上移、下移、删除 Cue。
- 校验数据库并显示问题。
- 编辑 Mixer 与 Debug 配置。

校验会报告：

- 空 Cue。
- 缺少 Key。
- 重复 Key。
- 没有配置 Clip。
- Clip 引用既没有直连 Clip，也没有 Address Key。
- 空总线或重复总线通道。

## 样例

示例资源位于 `Assets/CAudio/Samples`。

- `Samples.unity`：示例场景。
- `Scripts/CAudioSampleController.cs`：演示 SFX、UI、Voice、Ambience、Music、Crossfade、3D 播放、跟随目标、直连 Clip 和循环停止。
- `README.md`：样例使用说明。

## 测试

项目使用 Unity Test Framework。测试程序集位于 `Assets/CAudio/Tests/EditMode`，覆盖：

- `AudioPlayOptions` 克隆与默认值。
- Cue 选择策略、音量/音调范围。
- 数据库查找、重复 Key、缺失 Clip、不可解析引用校验。
- 播放失败路径。
- Cooldown 与最大同时播放数。
- 异步播放取消与成功回调。
- Group Stop。

运行命令示例：

```powershell
& "D:\Program\Unity\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "D:\Program\Unity\Projects\CAudio" -runTests -testPlatform EditMode -testResults "D:\Program\Unity\Projects\CAudio\TestResults.xml" -logFile "D:\Program\Unity\Projects\CAudio\UnityTest.log"
```

注意：当前使用的 Unity Test Framework `1.1.33` 不要在该命令后追加 `-quit`，否则测试启动器可能在实际执行前被 Unity 退出流程打断。Test Runner 会在测试结束后自行退出。

最新验证见 [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)。

## 常见问题

### 播放没有声音

检查：

- 是否已调用 `AudioManager.Initialize(database)`，或场景中是否挂了 `AudioSystemBootstrap`。
- `AudioDatabase` 是否包含对应 Key。
- Cue 是否配置了可解析的 Clip。
- 通道是否被 Mute、Solo 排除或音量为 0。
- Mixer 暴露参数名是否正确。

### `TryPlay` 返回 `ProviderFailed`

直连 Clip 模式下通常表示 `AudioClipReference.DirectClip` 为空。Addressables 或自定义 Provider 模式下通常表示地址为空、加载失败或 Provider 没有正确回调。

### `PlayAsync` 取消后会不会留下 AudioSource

不会。异步请求在 Clip 加载成功前不会创建播放句柄；取消后回调会被忽略并返回 `AudioPlayFailureReason.Cancelled`。

### 可以同时用内嵌 Cue 和独立 CueAsset 吗

可以。`AudioDatabase` 会同时收集内嵌 Cue 和 `AudioCueAsset.Data`。Key 查找不区分大小写，重复 Key 会保留先出现的配置。

### 如何限制高频音效堆叠

在 Cue 上设置：

- `Cooldown`：限制同 Key 的最小播放间隔。
- `MaxSimultaneous`：限制同 Key 的活动播放数。

### 如何让语音播放时压低音乐

在 `AudioDatabase.MixerSettings` 启用 `EnableVoiceDucking`，配置 `DuckMusicVolume` 和 `DuckFadeSpeed`。使用 `AudioManager.PlayVoice` 播放语音时，默认会应用 Ducking；也可通过 `AudioPlayOptions.ApplyVoiceDucking = false` 关闭某次播放的影响。

## 版本记录

见 [CHANGELOG.md](CHANGELOG.md)。
