# CAudio 快速开始

这份文档用于在 Unity 项目中完成一次最小可用接入。

## 1. 创建音频数据库

1. 在 Unity 菜单选择 `CAudio/Audio Database`。
2. 点击 `新建`，默认会在 `Assets/CAudio/Database` 下创建 `AudioDatabase.asset`。
3. 点击 `添加条目`，创建一个内嵌 Cue。
4. 设置 Cue 的 `Key`，例如 `ui_click`。
5. 在 Clip 列表里添加一个 `AudioClip`。

也可以点击 `新建Cue` 创建独立的 `AudioCueAsset`，再由数据库引用。

## 2. 初始化系统

在启动场景中创建一个空物体，挂载 `AudioSystemBootstrap`，并把 `AudioDatabase.asset` 拖到 `Database` 字段。

也可以在代码中手动初始化：

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

## 3. 播放音频

按 Key 播放数据库中的 Cue：

```csharp
AudioManager.Play("ui_click");
```

播放不同通道：

```csharp
AudioManager.PlaySfx("explosion");
AudioManager.PlayUi("ui_click");
AudioManager.PlayVoice("npc_hello");
AudioManager.PlayAmbience("wind_loop");
AudioManager.PlayMusic("battle_theme");
```

音乐切换时可使用交叉淡入淡出：

```csharp
AudioManager.CrossfadeMusic("battle_theme", 1.5f);
```

也可以把音乐加入队列，当前音乐结束后自动播放下一首：

```csharp
AudioManager.QueueMusic("intro_theme", 0.5f);
AudioManager.QueueMusic("loop_theme", 1.0f, new AudioPlayOptions { Loop = true });
```

在世界坐标播放 3D 音效：

```csharp
AudioManager.PlayAt("explosion", transform.position);
```

跟随目标播放：

```csharp
AudioManager.PlayFollow("engine_loop", vehicleTransform);
```

## 4. 使用播放句柄

`Play` 会返回 `AudioPlaybackHandle`，可以用于停止、暂停、调整音量或音调：

```csharp
AudioPlaybackHandle handle = AudioManager.Play("charge_loop", new AudioPlayOptions
{
    Loop = true,
    FadeIn = 0.2f,
    FadeOut = 0.3f
});

handle.SetVolume(0.7f);
handle.Stop();
```

需要明确失败原因时使用 `TryPlay`：

```csharp
AudioPlayResult result = AudioManager.TryPlay("missing_key");
if (!result.Success)
{
    Debug.LogWarning(result.Message);
}
```

异步资源加载时使用 `PlayAsync`：

```csharp
AudioAsyncPlayRequest request = AudioManager.PlayAsync("voice_line", onComplete: result =>
{
    if (!result.Success)
    {
        Debug.LogWarning(result.Message);
    }
});

// 需要取消加载中的请求时：
request.Cancel();
```

## 5. 停止与音量

停止指定 Key：

```csharp
AudioManager.Stop("ui_click");
```

停止指定通道：

```csharp
AudioManager.StopChannel(AudioChannel.Sfx, 0.2f);
```

停止一组音频：

```csharp
AudioManager.StopByKeyPrefix("enemy_", 0.1f);
AudioManager.StopGroup("combat", 0.2f);
```

停止全部音频：

```csharp
AudioManager.StopAll(0.5f);
```

设置主音量和通道音量：

```csharp
AudioManager.SetMasterVolume(0.8f);
AudioManager.SetChannelVolume(AudioChannel.Music, 0.5f);
```

暂停、恢复、静音和独奏通道：

```csharp
AudioManager.PauseChannel(AudioChannel.Sfx);
AudioManager.ResumeChannel(AudioChannel.Sfx);
AudioManager.SetChannelMute(AudioChannel.Ui, true);
AudioManager.SetSoloChannel(AudioChannel.Music);
AudioManager.ClearSoloChannel();
```

Mixer 参数和 Snapshot 过渡：

```csharp
AudioManager.TransitionMixerParameter("LowpassCutoff", 800f, 0.5f);
AudioManager.TransitionToSnapshot(combatSnapshot, 1.0f);
```

## 6. 配置校验

在 `CAudio/Audio Database` 窗口中点击 `校验`，可以检查：

- 空 Cue。
- 空 Key。
- 重复 Key。
- 缺少 Clip。
- 空总线或重复总线。

## 7. 高频音效限制

Cue 可以配置冷却时间和最大同时播放数，用于避免高频音效瞬间堆叠。代码中也可以直接设置：

```csharp
AudioCueData cue = new AudioCueData();
cue.SetIdentity("hit", "Hit");
cue.SetCooldown(0.05f);
cue.SetMaxSimultaneous(4);
```

Cue 的 Clip 选择方式支持：

- `Random`：随机。
- `WeightedRandom`：按权重随机。
- `Sequential`：顺序播放。
- `Shuffle`：洗牌袋播放。
- `NoImmediateRepeat`：不立即重复上一次 Clip。

## 8. Addressables 扩展

核心包不强依赖 Addressables。安装 `com.unity.addressables` 后，`AddressablesAudioClipProvider` 会通过 `CAUDIO_ADDRESSABLES` 版本定义自动参与编译。

```csharp
AudioManager.SetClipProvider(new AddressablesAudioClipProvider());
AudioManager.PlayAsync("addressable_voice");
```
