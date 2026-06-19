using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAudio.Samples
{
    /// <summary>Feature showcase scene controller built with uGUI.</summary>
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
            Log("CAudio Feature Showcase ready.");
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
                statusText.text = $"Initialized: {AudioManager.IsInitialized}  |  Ambience: {DescribeHandle(ambienceHandle)}  |  Follow: {DescribeHandle(followHandle)}  |  Stress: {stressHandles.Count}";
            }
        }

        public void PlayUi()
        {
            AudioManager.PlayUi("ui_click");
            Log("PlayUi(ui_click)");
        }

        public void PlaySfxRandom()
        {
            AudioManager.PlaySfx("sfx_random");
            Log("PlaySfx(sfx_random) with weighted random volume/pitch.");
        }

        public void PlaySfxSequential()
        {
            AudioManager.PlaySfx("sfx_sequence");
            Log("PlaySfx(sfx_sequence) with sequential selection.");
        }

        public void TryLimitedCue()
        {
            AudioPlayResult result = AudioManager.TryPlay("sfx_limited");
            Log(result.Success ? "TryPlay(sfx_limited) success." : $"TryPlay(sfx_limited) failed: {result.FailureReason}");
        }

        public void TryMissingCue()
        {
            AudioPlayResult result = AudioManager.TryPlay("missing_key");
            Log(result.Success ? "Unexpected missing cue success." : $"Missing cue result: {result.FailureReason}");
        }

        public void PlayAsyncCue()
        {
            AudioManager.PlayAsync("sfx_random", null, result =>
            {
                Log(result.Success ? "PlayAsync(sfx_random) completed." : $"PlayAsync failed: {result.FailureReason}");
            });
            Log("PlayAsync(sfx_random) requested.");
        }

        public void PlayDirectClip()
        {
            AudioManager.Play(directClip, new AudioPlayOptions { Channel = AudioChannel.Sfx, Volume = 0.9f });
            Log("Play(directClip)");
        }

        public void PlayAtPosition()
        {
            Vector3 position = transform.position + new Vector3(UnityEngine.Random.Range(-3f, 3f), 1f, UnityEngine.Random.Range(-2f, 2f));
            AudioManager.PlayAt("sfx_3d", position);
            Log($"PlayAt(sfx_3d, {position:F1})");
        }

        public void PlayFollowTarget()
        {
            followHandle = AudioManager.PlayFollow("sfx_3d", followTarget, new AudioPlayOptions
            {
                Loop = true,
                FadeIn = 0.15f,
                FadeOut = 0.3f
            });
            Log("PlayFollow(sfx_3d) loop started.");
        }

        public void StopFollowTarget()
        {
            followHandle?.Stop(0.3f);
            Log("Follow handle fade-out stop.");
        }

        public void ToggleAmbience()
        {
            if (ambienceHandle != null && !ambienceHandle.IsStopped)
            {
                ambienceHandle.Stop(0.75f);
                Log("Ambience loop fade-out stop.");
                return;
            }

            ambienceHandle = AudioManager.PlayAmbience("ambience_loop");
            Log("Ambience loop fade-in start.");
        }

        public void PlayMusic()
        {
            AudioManager.PlayMusic("music_theme");
            Log("PlayMusic(music_theme)");
        }

        public void CrossfadeMusic()
        {
            AudioManager.CrossfadeMusic("music_alt", 1f);
            Log("CrossfadeMusic(music_alt, 1s)");
        }

        public void QueueMusic()
        {
            AudioManager.QueueMusic("music_theme", 0.5f);
            AudioManager.QueueMusic("music_alt", 0.5f);
            Log("Queued music_theme then music_alt.");
        }

        public void PlayVoice()
        {
            AudioManager.PlayVoice("voice_line");
            Log("PlayVoice(voice_line), voice ducking enabled in database.");
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
                    Log($"Pool stress item {i + 1} failed: {result.FailureReason}");
                }
            }

            Log($"Pool stress requested 10 loops, active handles: {stressHandles.Count}.");
        }

        public void ClearStressHandles()
        {
            for (int i = 0; i < stressHandles.Count; i++)
            {
                stressHandles[i]?.Stop(0.2f);
            }

            stressHandles.Clear();
            Log("Stopped stress handles.");
        }

        public void PauseAll()
        {
            AudioManager.PauseAll();
            Log("PauseAll()");
        }

        public void ResumeAll()
        {
            AudioManager.ResumeAll();
            Log("ResumeAll()");
        }

        public void StopAll()
        {
            AudioManager.StopAll(0.4f);
            ClearStressHandles();
            Log("StopAll(0.4s)");
        }

        public void StopGroupCombat()
        {
            AudioManager.StopGroup("combat", 0.2f);
            Log("StopGroup(combat)");
        }

        public void StopKeyPrefix()
        {
            AudioManager.StopByKeyPrefix("sfx_", 0.2f);
            Log("StopByKeyPrefix(sfx_)");
        }

        public void StopSfxChannel()
        {
            AudioManager.StopChannel(AudioChannel.Sfx, 0.2f);
            Log("StopChannel(Sfx)");
        }

        public void SoloMusic()
        {
            AudioManager.SetSoloChannel(AudioChannel.Music);
            Log("Solo Music channel.");
        }

        public void ClearSolo()
        {
            AudioManager.ClearSoloChannel();
            Log("Clear solo channel.");
        }

        public void MuteSfx(bool mute)
        {
            AudioManager.SetChannelMute(AudioChannel.Sfx, mute);
            Log(mute ? "Sfx muted." : "Sfx unmuted.");
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

            Text title = CreateText("Title", panelObject.transform, "CAudio Feature Showcase", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.color = Color.white;

            statusText = CreateText("Status", panelObject.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            statusText.color = new Color(0.75f, 0.82f, 0.9f, 1f);

            CreateButtonGrid(panelObject.transform, "Playback", new[]
            {
                ButtonSpec("UI Click", PlayUi),
                ButtonSpec("SFX Random", PlaySfxRandom),
                ButtonSpec("SFX Sequential", PlaySfxSequential),
                ButtonSpec("Cooldown/Limit", TryLimitedCue),
                ButtonSpec("Missing Cue", TryMissingCue),
                ButtonSpec("Async Play", PlayAsyncCue),
                ButtonSpec("Direct Clip", PlayDirectClip),
                ButtonSpec("Voice Ducking", PlayVoice)
            });

            CreateButtonGrid(panelObject.transform, "Music And Loops", new[]
            {
                ButtonSpec("Music", PlayMusic),
                ButtonSpec("Crossfade", CrossfadeMusic),
                ButtonSpec("Queue Music", QueueMusic),
                ButtonSpec("Ambience Toggle", ToggleAmbience),
                ButtonSpec("3D At Position", PlayAtPosition),
                ButtonSpec("Follow Loop", PlayFollowTarget),
                ButtonSpec("Stop Follow", StopFollowTarget),
                ButtonSpec("Pool Stress", StartPoolStress)
            });

            CreateButtonGrid(panelObject.transform, "Control", new[]
            {
                ButtonSpec("Pause All", PauseAll),
                ButtonSpec("Resume All", ResumeAll),
                ButtonSpec("Stop All", StopAll),
                ButtonSpec("Stop Combat Group", StopGroupCombat),
                ButtonSpec("Stop sfx_*", StopKeyPrefix),
                ButtonSpec("Stop Sfx Channel", StopSfxChannel),
                ButtonSpec("Solo Music", SoloMusic),
                ButtonSpec("Clear Solo", ClearSolo)
            });

            CreateSliderRow(panelObject.transform, "Master", SetMasterVolume, 1f);
            CreateSliderRow(panelObject.transform, "Music", SetMusicVolume, 0.8f);
            CreateSliderRow(panelObject.transform, "Sfx", SetSfxVolume, 1f);
            CreateToggle(panelObject.transform, "Mute Sfx", MuteSfx);

            logText = CreateText("Log", panelObject.transform, "Log:", 14, FontStyle.Normal, TextAnchor.UpperLeft);
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
                return "None";
            }

            if (handle.IsStopped)
            {
                return "Stopped";
            }

            return handle.Paused ? "Paused" : "Playing";
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

