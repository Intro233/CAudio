using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAudio.Samples
{
    /// <summary>CAudio 功能展示场景控制器，使用 uGUI 构建运行时面板。</summary>
    public sealed class CAudioFeatureShowcase : MonoBehaviour
    {
        [SerializeField] private AudioDatabase database;
        [SerializeField] private AudioClip directClip;
        [SerializeField] private Transform followTarget;

        private readonly List<AudioPlaybackHandle> stressHandles = new List<AudioPlaybackHandle>();
        private AudioPlaybackHandle ambienceHandle;
        private AudioPlaybackHandle followHandle;
        private Text logText;
        private Text statusText;
        private Transform generatedUiRoot;
        private float followAngle;

        private void Awake()
        {
            AudioManager.Initialize(database);
        }

        private void Start()
        {
            EnsureFollowTarget();
            BuildUi();
            Log("CAudio 功能展示已就绪。");
        }

        private void Update()
        {
            if (followTarget != null)
            {
                followAngle += Time.deltaTime * 70f;
                float radians = followAngle * Mathf.Deg2Rad;
                followTarget.position = new Vector3(Mathf.Cos(radians) * 3f, 1.1f, Mathf.Sin(radians) * 3f);
            }

            if (statusText != null)
            {
                statusText.text = $"已初始化：{AudioManager.IsInitialized}  |  环境音：{DescribeHandle(ambienceHandle)}  |  跟随音：{DescribeHandle(followHandle)}  |  压力句柄：{stressHandles.Count}";
            }
        }

        public void PlayUi()
        {
            AudioManager.PlayUi("ui_click");
            Log("播放 UI 音效：ui_click");
        }

        public void PlaySfxRandom()
        {
            AudioManager.PlaySfx("sfx_random");
            Log("播放 SFX：sfx_random，加权随机并带音量/音调浮动。");
        }

        public void PlaySfxSequential()
        {
            AudioManager.PlaySfx("sfx_sequence");
            Log("播放 SFX：sfx_sequence，顺序选择。");
        }

        public void TryLimitedCue()
        {
            AudioPlayResult result = AudioManager.TryPlay("sfx_limited");
            Log(result.Success ? "限制型 Cue 播放成功。" : $"限制型 Cue 播放失败：{result.FailureReason}");
        }

        public void TryMissingCue()
        {
            AudioPlayResult result = AudioManager.TryPlay("missing_key");
            Log(result.Success ? "意外播放了不存在的 Cue。" : $"不存在 Cue 的失败结果：{result.FailureReason}");
        }

        public void PlayAsyncCue()
        {
            AudioManager.PlayAsync("sfx_random", null, result =>
            {
                Log(result.Success ? "异步播放完成：sfx_random。" : $"异步播放失败：{result.FailureReason}");
            });
            Log("已发起异步播放：sfx_random。");
        }

        public void PlayDirectClip()
        {
            AudioManager.Play(directClip, new AudioPlayOptions { Channel = AudioChannel.Sfx, Volume = 0.9f });
            Log("播放直连 AudioClip。");
        }

        public void PlayAtPosition()
        {
            Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(-3f, 3f), 1f, UnityEngine.Random.Range(-2f, 2f));
            AudioManager.PlayAt("sfx_3d", position);
            Log($"在世界坐标播放 3D 音效：{position:F1}");
        }

        public void PlayFollowTarget()
        {
            followHandle = AudioManager.PlayFollow("sfx_3d", followTarget, new AudioPlayOptions
            {
                Loop = true,
                FadeIn = 0.15f,
                FadeOut = 0.3f
            });
            Log("开始跟随目标循环播放 3D 音效。");
        }

        public void StopFollowTarget()
        {
            followHandle?.Stop(0.3f);
            Log("跟随音效淡出停止。");
        }

        public void ToggleAmbience()
        {
            if (ambienceHandle != null && !ambienceHandle.IsStopped)
            {
                ambienceHandle.Stop(0.75f);
                Log("环境循环淡出停止。");
                return;
            }

            ambienceHandle = AudioManager.PlayAmbience("ambience_loop");
            Log("环境循环淡入开始。");
        }

        public void PlayMusic()
        {
            AudioManager.PlayMusic("music_theme");
            Log("播放音乐：music_theme。");
        }

        public void CrossfadeMusic()
        {
            AudioManager.CrossfadeMusic("music_alt", 1f);
            Log("音乐交叉淡入淡出到：music_alt，1 秒。");
        }

        public void QueueMusic()
        {
            AudioManager.QueueMusic("music_theme", 0.5f);
            AudioManager.QueueMusic("music_alt", 0.5f);
            Log("已排队播放 music_theme，然后 music_alt。");
        }

        public void PlayVoice()
        {
            AudioManager.PlayVoice("voice_line");
            Log("播放语音：voice_line，数据库已启用语音压低音乐。");
        }

        public void StartPoolStress()
        {
            ClearStressHandles();
            for (int i = 0; i < 10; i++)
            {
                AudioPlayResult result = AudioManager.TryPlay("pool_loop");
                if (result.Success)
                {
                    stressHandles.Add(result.Handle);
                }
                else
                {
                    Log($"音源池压力项 {i + 1} 播放失败：{result.FailureReason}");
                }
            }

            Log($"已请求 10 个循环音源，成功句柄数：{stressHandles.Count}。");
        }

        public void ClearStressHandles()
        {
            for (int i = 0; i < stressHandles.Count; i++)
            {
                stressHandles[i]?.Stop(0.2f);
            }

            stressHandles.Clear();
            Log("已停止压力测试句柄。");
        }

        public void PauseAll()
        {
            AudioManager.PauseAll();
            Log("暂停全部音频。");
        }

        public void ResumeAll()
        {
            AudioManager.ResumeAll();
            Log("恢复全部音频。");
        }

        public void StopAll()
        {
            AudioManager.StopAll(0.4f);
            ClearStressHandles();
            Log("停止全部音频，淡出 0.4 秒。");
        }

        public void StopGroupCombat()
        {
            AudioManager.StopGroup("combat", 0.2f);
            Log("停止 combat 分组。");
        }

        public void StopKeyPrefix()
        {
            AudioManager.StopByKeyPrefix("sfx_", 0.2f);
            Log("停止 Key 前缀为 sfx_ 的音频。");
        }

        public void StopSfxChannel()
        {
            AudioManager.StopChannel(AudioChannel.Sfx, 0.2f);
            Log("停止 Sfx 通道。");
        }

        public void SoloMusic()
        {
            AudioManager.SetSoloChannel(AudioChannel.Music);
            Log("独奏 Music 通道。");
        }

        public void ClearSolo()
        {
            AudioManager.ClearSoloChannel();
            Log("清除通道独奏。");
        }

        public void MuteSfx(bool mute)
        {
            AudioManager.SetChannelMute(AudioChannel.Sfx, mute);
            Log(mute ? "Sfx 已静音。" : "Sfx 已取消静音。");
        }

        public void SetMasterVolume(float value)
        {
            AudioManager.SetMasterVolume(value);
        }

        public void SetMusicVolume(float value)
        {
            AudioManager.SetChannelVolume(AudioChannel.Music, value);
        }

        public void SetSfxVolume(float value)
        {
            AudioManager.SetChannelVolume(AudioChannel.Sfx, value);
        }

        private void BuildUi()
        {
            if (generatedUiRoot != null)
            {
                return;
            }

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("CAudio Sample Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            generatedUiRoot = canvasObject.transform;

            GameObject scrollObject = CreateUiObject("Scroll View", generatedUiRoot);
            Image scrollImage = scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            Stretch(scrollObject.GetComponent<RectTransform>(), new Vector2(18f, 18f), new Vector2(-18f, -18f));

            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            GameObject viewportObject = CreateUiObject("Viewport", scrollObject.transform);
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = Color.white;
            Stretch(viewportObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            GameObject panelObject = CreateUiObject("Content", viewportObject.transform);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.11f, 0f);
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
            scrollRect.content = panel;

            VerticalLayoutGroup rootLayout = panelObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(18, 18, 16, 16);
            rootLayout.spacing = 10f;
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            Text title = CreateText("Title", panelObject.transform, "CAudio 功能展示", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.color = Color.white;

            statusText = CreateText("Status", panelObject.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            statusText.color = new Color(0.75f, 0.82f, 0.9f, 1f);

            CreateButtonGrid(panelObject.transform, "播放能力", new[]
            {
                ButtonSpec("UI 点击", PlayUi),
                ButtonSpec("随机音效", PlaySfxRandom),
                ButtonSpec("顺序音效", PlaySfxSequential),
                ButtonSpec("冷却/上限", TryLimitedCue),
                ButtonSpec("缺失 Cue", TryMissingCue),
                ButtonSpec("异步播放", PlayAsyncCue),
                ButtonSpec("直连 Clip", PlayDirectClip),
                ButtonSpec("语音压低", PlayVoice)
            });

            CreateButtonGrid(panelObject.transform, "音乐与循环", new[]
            {
                ButtonSpec("播放音乐", PlayMusic),
                ButtonSpec("交叉淡入淡出", CrossfadeMusic),
                ButtonSpec("音乐队列", QueueMusic),
                ButtonSpec("环境音开关", ToggleAmbience),
                ButtonSpec("3D 坐标播放", PlayAtPosition),
                ButtonSpec("跟随循环", PlayFollowTarget),
                ButtonSpec("停止跟随", StopFollowTarget),
                ButtonSpec("音源池压力", StartPoolStress)
            });

            CreateButtonGrid(panelObject.transform, "控制能力", new[]
            {
                ButtonSpec("暂停全部", PauseAll),
                ButtonSpec("恢复全部", ResumeAll),
                ButtonSpec("停止全部", StopAll),
                ButtonSpec("停止 combat", StopGroupCombat),
                ButtonSpec("停止 sfx_*", StopKeyPrefix),
                ButtonSpec("停止 Sfx 通道", StopSfxChannel),
                ButtonSpec("独奏 Music", SoloMusic),
                ButtonSpec("清除独奏", ClearSolo)
            });

            CreateSliderRow(panelObject.transform, "主音量", SetMasterVolume, 1f);
            CreateSliderRow(panelObject.transform, "音乐音量", SetMusicVolume, 0.8f);
            CreateSliderRow(panelObject.transform, "音效音量", SetSfxVolume, 1f);
            CreateToggle(panelObject.transform, "静音 Sfx", MuteSfx);

            logText = CreateText("Log", panelObject.transform, "日志：", 14, FontStyle.Normal, TextAnchor.UpperLeft);
            logText.color = new Color(0.86f, 0.88f, 0.9f, 1f);
            LayoutElement logLayout = logText.gameObject.AddComponent<LayoutElement>();
            logLayout.minHeight = 120f;
            logLayout.flexibleHeight = 1f;
        }

        private void CreateButtonGrid(Transform parent, string heading, IReadOnlyList<ButtonEntry> buttons)
        {
            Text label = CreateText($"{heading} Label", parent, heading, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = new Color(0.96f, 0.92f, 0.78f, 1f);

            GameObject gridObject = CreateUiObject($"{heading} Buttons", parent);
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(148f, 34f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            LayoutElement layout = gridObject.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Ceil(buttons.Count / 4f) * 42f;
            layout.preferredHeight = layout.minHeight;

            for (int i = 0; i < buttons.Count; i++)
            {
                CreateButton(gridObject.transform, buttons[i].Label, buttons[i].Action);
            }
        }

        private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateUiObject(label, parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.25f, 0.31f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text text = CreateText("Text", buttonObject.transform, label, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = Color.white;
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        }

        private void CreateSliderRow(Transform parent, string label, Action<float> onChanged, float value)
        {
            GameObject row = CreateUiObject($"{label} Slider Row", parent);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 28f;

            Text text = CreateText($"{label} Label", row.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = Color.white;
            LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 78f;

            Slider slider = CreateUiObject($"{label} Slider", row.transform).AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChanged(v));
            BuildSliderGraphics(slider);
        }

        private void CreateToggle(Transform parent, string label, Action<bool> onChanged)
        {
            Toggle toggle = CreateUiObject(label, parent).AddComponent<Toggle>();
            LayoutElement layout = toggle.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 28f;

            Image background = CreateUiObject("Background", toggle.transform).AddComponent<Image>();
            background.color = new Color(0.15f, 0.18f, 0.22f, 1f);
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(22f, 22f);
            backgroundRect.anchoredPosition = new Vector2(11f, 0f);

            Image checkmark = CreateUiObject("Checkmark", background.transform).AddComponent<Image>();
            checkmark.color = new Color(0.45f, 0.78f, 1f, 1f);
            Stretch(checkmark.rectTransform, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            Text text = CreateText("Label", toggle.transform, label, 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = Color.white;
            RectTransform labelRect = text.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(34f, 0f);
            labelRect.offsetMax = Vector2.zero;

            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.onValueChanged.AddListener(v => onChanged(v));
        }

        private void BuildSliderGraphics(Slider slider)
        {
            RectTransform sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0f, 22f);

            Image background = CreateUiObject("Background", slider.transform).AddComponent<Image>();
            background.color = new Color(0.14f, 0.16f, 0.2f, 1f);
            RectTransform backgroundRect = background.rectTransform;
            Stretch(backgroundRect, new Vector2(0f, 7f), new Vector2(0f, -7f));

            RectTransform fillArea = CreateUiObject("Fill Area", slider.transform).GetComponent<RectTransform>();
            Stretch(fillArea, new Vector2(0f, 7f), new Vector2(0f, -7f));

            Image fill = CreateUiObject("Fill", fillArea).AddComponent<Image>();
            fill.color = new Color(0.45f, 0.78f, 1f, 1f);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

            Image handle = CreateUiObject("Handle", slider.transform).AddComponent<Image>();
            handle.color = Color.white;
            RectTransform handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(18f, 18f);

            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
        {
            Text text = CreateUiObject(name, parent).AddComponent<Text>();
            text.font = ResolveFont();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void Stretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minOffset;
            rect.offsetMax = maxOffset;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void EnsureFollowTarget()
        {
            if (followTarget != null)
            {
                return;
            }

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "CAudio Follow Target";
            target.transform.position = new Vector3(3f, 1f, 0f);
            target.transform.localScale = Vector3.one * 0.35f;
            followTarget = target.transform;
        }

        private Font ResolveFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private ButtonEntry ButtonSpec(string label, UnityEngine.Events.UnityAction action)
        {
            return new ButtonEntry(label, action);
        }

        private string DescribeHandle(AudioPlaybackHandle handle)
        {
            if (handle == null)
            {
                return "无";
            }

            if (handle.IsStopped)
            {
                return "已停止";
            }

            return handle.Paused ? "已暂停" : "播放中";
        }

        private void Log(string message)
        {
            string line = $"[{Time.time:0.00}] {message}";
            Debug.Log(line);
            if (logText == null)
            {
                return;
            }

            logText.text = $"{line}\n{logText.text}";
        }

        private readonly struct ButtonEntry
        {
            public readonly string Label;
            public readonly UnityEngine.Events.UnityAction Action;

            public ButtonEntry(string label, UnityEngine.Events.UnityAction action)
            {
                Label = label;
                Action = action;
            }
        }
    }
}

