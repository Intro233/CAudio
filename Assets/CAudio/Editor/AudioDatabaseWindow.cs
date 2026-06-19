#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace CAudio.EditorTools
{
    /// <summary>音频数据库管理窗口。</summary>
    public sealed class AudioDatabaseWindow : EditorWindow
    {
        private const string DefaultFolder = "Assets/CAudio/Database";
        private const string PresetMixerTemplateGuid = "ca0d10a0000000000000000000001002";

        private AudioDatabase database;
        private SerializedObject serializedDatabase;
        private Vector2 scrollPosition;
        private string searchText;
        private bool filterByChannel;
        private bool filterIssuesOnly;
        private bool compactCueCards = true;
        private AudioChannel channelFilter = AudioChannel.Sfx;
        private List<AudioDatabaseValidationIssue> validationIssues;

        private bool showCues = true;
        private bool showCueAssets = true;
        private bool showBuses = true;
        private bool showMixer = true;
        private bool showPool = true;
        private bool showDebug = true;
        private bool showValidation = true;

        private string statusMessage;
        private MessageType statusType = MessageType.Info;

        private GUIStyle sectionStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle mutedStyle;
        private GUIStyle badgeStyle;

        /// <summary>打开窗口。</summary>
        [MenuItem("CAudio/Audio Database")]
        public static void Open()
        {
            GetWindow<AudioDatabaseWindow>("Audio Database");
        }

        private void OnEnable()
        {
            minSize = new Vector2(720f, 520f);
            if (database == null && Selection.activeObject is AudioDatabase selectedDatabase)
            {
                SetDatabase(selectedDatabase);
            }
        }

        private void OnSelectionChange()
        {
            if (database == null && Selection.activeObject is AudioDatabase selectedDatabase)
            {
                SetDatabase(selectedDatabase);
                Repaint();
            }
        }

        /// <summary>绘制窗口界面。</summary>
        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (database == null)
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.HelpBox("请选择或创建一个 AudioDatabase。也可以在 Project 窗口选中数据库资产后重新打开本窗口。", MessageType.Info);
                return;
            }

            if (serializedDatabase == null || serializedDatabase.targetObject != database)
            {
                serializedDatabase = new SerializedObject(database);
            }

            serializedDatabase.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawDatabaseHeader();
            DrawCueList();
            DrawCueAssetList();
            DrawBusList();
            DrawMixerSettings();
            DrawPoolSettings();
            DrawDebugSettings();
            DrawValidationIssues();
            EditorGUILayout.EndScrollView();
            serializedDatabase.ApplyModifiedProperties();

            if (GUI.changed && database != null)
            {
                database.RebuildCache();
                EditorUtility.SetDirty(database);
            }
        }

        /// <summary>绘制顶部工具栏。</summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            AudioDatabase selected = (AudioDatabase)EditorGUILayout.ObjectField(database, typeof(AudioDatabase), false, GUILayout.MinWidth(220f));
            if (selected != database)
            {
                SetDatabase(selected);
            }

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(58f)))
            {
                CreateDatabaseAsset();
            }

            using (new EditorGUI.DisabledScope(database == null))
            {
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                {
                    Selection.activeObject = database;
                    EditorGUIUtility.PingObject(database);
                }

                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                {
                    SaveDatabase();
                }

                if (GUILayout.Button("校验", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                {
                    RunValidation();
                }
            }

            GUILayout.FlexibleSpace();
            DrawValidationToolbarStatus();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            using (new EditorGUI.DisabledScope(database == null))
            {
                if (GUILayout.Button("添加条目", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    AddCue();
                }

                if (GUILayout.Button("新建Cue", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    CreateCueAsset();
                }

                if (GUILayout.Button("导入选中Clip", EditorStyles.toolbarButton, GUILayout.Width(102f)))
                {
                    ImportSelectedClips();
                }

                if (GUILayout.Button("添加总线", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    AddBus();
                }

                GUILayout.Space(8f);
                filterByChannel = GUILayout.Toggle(filterByChannel, "按通道", EditorStyles.toolbarButton, GUILayout.Width(62f));
                using (new EditorGUI.DisabledScope(!filterByChannel))
                {
                    channelFilter = (AudioChannel)EditorGUILayout.EnumPopup(channelFilter, GUILayout.Width(94f));
                }

                filterIssuesOnly = GUILayout.Toggle(filterIssuesOnly, "仅问题", EditorStyles.toolbarButton, GUILayout.Width(62f));
                if (filterIssuesOnly && validationIssues == null)
                {
                    RunValidation();
                }

                compactCueCards = GUILayout.Toggle(compactCueCards, "紧凑", EditorStyles.toolbarButton, GUILayout.Width(52f));
                searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));

                if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    ClearFilters();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>绘制数据库总览。</summary>
        private void DrawDatabaseHeader()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("音频数据库", headerTitleStyle);
                GUILayout.FlexibleSpace();
                DrawAssetPath();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4f);
                EditorGUILayout.BeginHorizontal();
                DrawStatBadge("内嵌", GetArraySize("cues").ToString());
                DrawStatBadge("独立Cue", GetArraySize("cueAssets").ToString());
                DrawStatBadge("总线", GetArraySize("buses").ToString());
                DrawStatBadge("筛选显示", $"{CountVisibleCues()}/{GetArraySize("cues")}");
                DrawStatBadge("校验", validationIssues == null ? "未运行" : validationIssues.Count == 0 ? "通过" : $"{validationIssues.Count} 个问题");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrWhiteSpace(statusMessage))
                {
                    EditorGUILayout.HelpBox(statusMessage, statusType);
                }
            }
        }

        /// <summary>绘制音频条目列表。</summary>
        private void DrawCueList()
        {
            SerializedProperty cuesProp = serializedDatabase.FindProperty("cues");
            if (cuesProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.BeginHorizontal();
                showCues = EditorGUILayout.Foldout(showCues, $"音频条目 ({CountVisibleCues()}/{cuesProp.arraySize})", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部展开", EditorStyles.miniButtonLeft, GUILayout.Width(74f)))
                {
                    SetAllCueExpanded(cuesProp, true);
                }

                if (GUILayout.Button("全部折叠", EditorStyles.miniButtonMid, GUILayout.Width(74f)))
                {
                    SetAllCueExpanded(cuesProp, false);
                }

                if (GUILayout.Button("规范化Key", EditorStyles.miniButtonRight, GUILayout.Width(82f)))
                {
                    NormalizeAllCueKeys(cuesProp);
                }

                EditorGUILayout.EndHorizontal();

                if (!showCues)
                {
                    return;
                }

                DrawClipDropZone();
                int visibleCount = DrawCueElements(cuesProp);
                if (visibleCount == 0)
                {
                    EditorGUILayout.HelpBox("当前筛选条件下没有可显示的音频条目。", MessageType.Info);
                }
            }
        }

        /// <summary>绘制独立Cue资产列表。</summary>
        private void DrawCueAssetList()
        {
            SerializedProperty cueAssetsProp = serializedDatabase.FindProperty("cueAssets");
            if (cueAssetsProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.BeginHorizontal();
                showCueAssets = EditorGUILayout.Foldout(showCueAssets, $"独立Cue资产 ({cueAssetsProp.arraySize})", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("移除空引用", EditorStyles.miniButton, GUILayout.Width(86f)))
                {
                    RemoveNullReferences(cueAssetsProp, "独立Cue资产");
                }

                EditorGUILayout.EndHorizontal();

                if (showCueAssets)
                {
                    EditorGUILayout.PropertyField(cueAssetsProp, true);
                }
            }
        }

        /// <summary>绘制总线列表。</summary>
        private void DrawBusList()
        {
            SerializedProperty busesProp = serializedDatabase.FindProperty("buses");
            if (busesProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.BeginHorizontal();
                showBuses = EditorGUILayout.Foldout(showBuses, $"总线配置 ({busesProp.arraySize})", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("CAudio预设", EditorStyles.miniButtonLeft, GUILayout.Width(86f)))
                {
                    CreatePresetBusConfiguration();
                }

                if (GUILayout.Button("添加总线", EditorStyles.miniButtonRight, GUILayout.Width(74f)))
                {
                    AddBus();
                }

                EditorGUILayout.EndHorizontal();

                if (showBuses)
                {
                    DrawBusElements(busesProp);
                }
            }
        }

        /// <summary>绘制混音器配置。</summary>
        private void DrawMixerSettings()
        {
            SerializedProperty mixerProp = serializedDatabase.FindProperty("mixerSettings");
            if (mixerProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                showMixer = EditorGUILayout.Foldout(showMixer, "混音器与 Ducking", true, EditorStyles.foldoutHeader);
                if (showMixer)
                {
                    EditorGUILayout.PropertyField(mixerProp, true);
                }
            }
        }

        /// <summary>绘制音源池配置。</summary>
        private void DrawPoolSettings()
        {
            SerializedProperty poolProp = serializedDatabase.FindProperty("poolSettings");
            if (poolProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                showPool = EditorGUILayout.Foldout(showPool, "音源池", true, EditorStyles.foldoutHeader);
                if (showPool)
                {
                    EditorGUILayout.HelpBox("Prewarm 会在初始化时提前创建音源；Max Source Count 为 0 时表示不限制。", MessageType.None);
                    EditorGUILayout.PropertyField(poolProp, true);
                }
            }
        }

        /// <summary>绘制调试配置。</summary>
        private void DrawDebugSettings()
        {
            SerializedProperty debugProp = serializedDatabase.FindProperty("debugSettings");
            if (debugProp == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                showDebug = EditorGUILayout.Foldout(showDebug, "调试配置", true, EditorStyles.foldoutHeader);
                if (showDebug)
                {
                    EditorGUILayout.PropertyField(debugProp, true);
                }
            }
        }

        /// <summary>绘制校验结果。</summary>
        private void DrawValidationIssues()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.BeginHorizontal();
                showValidation = EditorGUILayout.Foldout(showValidation, "校验结果", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("重新校验", EditorStyles.miniButtonLeft, GUILayout.Width(78f)))
                {
                    RunValidation();
                }

                if (GUILayout.Button("清空结果", EditorStyles.miniButtonRight, GUILayout.Width(78f)))
                {
                    validationIssues = null;
                    SetStatus("已清空校验结果。", MessageType.Info);
                }

                EditorGUILayout.EndHorizontal();

                if (!showValidation)
                {
                    return;
                }

                if (validationIssues == null)
                {
                    EditorGUILayout.HelpBox("尚未运行校验。点击“重新校验”可检查空 Key、重复 Key、缺失 Clip、空引用和重复总线。", MessageType.Info);
                    return;
                }

                if (validationIssues.Count == 0)
                {
                    EditorGUILayout.HelpBox("未发现问题。", MessageType.Info);
                    return;
                }

                for (int i = 0; i < validationIssues.Count; i++)
                {
                    AudioDatabaseValidationIssue issue = validationIssues[i];
                    MessageType type = issue.Level == AudioLogLevel.Error ? MessageType.Error : MessageType.Warning;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(issue.Message, type);
                    using (new EditorGUI.DisabledScope(issue.Context == null && string.IsNullOrWhiteSpace(issue.CueKey)))
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(52f), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f)))
                        {
                            LocateIssue(issue);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        /// <summary>绘制全部条目。</summary>
        private int DrawCueElements(SerializedProperty cuesProp)
        {
            int visibleCount = 0;
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                SerializedProperty element = cuesProp.GetArrayElementAtIndex(i);
                if (!ShouldShowCue(element))
                {
                    continue;
                }

                visibleCount++;
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    if (!compactCueCards || element.isExpanded)
                    {
                        DrawCueSummary(element, i);
                        EditorGUILayout.Space(2f);
                    }

                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    if ((!compactCueCards || element.isExpanded) && DrawCueActions(cuesProp, i))
                    {
                        break;
                    }
                }
            }

            return visibleCount;
        }

        /// <summary>绘制单个条目摘要。</summary>
        private void DrawCueSummary(SerializedProperty element, int index)
        {
            SerializedProperty keyProp = element.FindPropertyRelative("key");
            SerializedProperty nameProp = element.FindPropertyRelative("displayName");
            SerializedProperty groupProp = element.FindPropertyRelative("group");
            SerializedProperty channelProp = element.FindPropertyRelative("channel");
            SerializedProperty clipsProp = element.FindPropertyRelative("clips");
            SerializedProperty loopProp = element.FindPropertyRelative("loop");

            string key = keyProp != null && !string.IsNullOrWhiteSpace(keyProp.stringValue) ? keyProp.stringValue : "<缺少 Key>";
            string display = nameProp != null ? nameProp.stringValue : string.Empty;
            string group = groupProp != null ? groupProp.stringValue : string.Empty;
            string channel = channelProp != null ? channelProp.enumDisplayNames[channelProp.enumValueIndex] : "Unknown";
            int clipCount = clipsProp != null ? clipsProp.arraySize : 0;
            bool hasIssue = HasCueIssue(element);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{index + 1}", mutedStyle, GUILayout.Width(40f));
            GUILayout.Label(key, hasIssue ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MinWidth(130f));
            if (!string.IsNullOrWhiteSpace(display))
            {
                GUILayout.Label(display, mutedStyle, GUILayout.MinWidth(100f));
            }

            if (!string.IsNullOrWhiteSpace(group))
            {
                DrawBadge(group);
            }

            GUILayout.FlexibleSpace();
            DrawBadge(channel);
                DrawBadge($"{clipCount} 个 Clip");
            if (loopProp != null && loopProp.boolValue)
            {
                DrawBadge("循环");
            }

            if (hasIssue)
            {
                DrawBadge("问题");
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>绘制条目操作。</summary>
        private bool DrawCueActions(SerializedProperty cuesProp, int index)
        {
            bool duplicate = false;
            bool moveUp = false;
            bool moveDown = false;
            bool delete = false;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制", EditorStyles.miniButtonLeft, GUILayout.Width(52f)))
            {
                duplicate = true;
            }

            using (new EditorGUI.DisabledScope(index <= 0))
            {
                if (GUILayout.Button("上移", EditorStyles.miniButtonMid, GUILayout.Width(52f)))
                {
                    moveUp = true;
                }
            }

            using (new EditorGUI.DisabledScope(index >= cuesProp.arraySize - 1))
            {
                if (GUILayout.Button("下移", EditorStyles.miniButtonMid, GUILayout.Width(52f)))
                {
                    moveDown = true;
                }
            }

            if (GUILayout.Button("删除", EditorStyles.miniButtonRight, GUILayout.Width(52f)) &&
                ConfirmDelete("删除音频条目", "确定要删除这个音频条目吗？"))
            {
                delete = true;
            }

            EditorGUILayout.EndHorizontal();

            if (duplicate)
            {
                DuplicateCue(cuesProp, index);
                return true;
            }

            if (moveUp)
            {
                cuesProp.MoveArrayElement(index, index - 1);
                return true;
            }

            if (moveDown)
            {
                cuesProp.MoveArrayElement(index, index + 1);
                return true;
            }

            if (delete)
            {
                cuesProp.DeleteArrayElementAtIndex(index);
                return true;
            }

            return false;
        }

        /// <summary>绘制全部总线。</summary>
        private void DrawBusElements(SerializedProperty busesProp)
        {
            if (busesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("尚未配置总线。总线可统一设置通道输出组、音量和静音状态。", MessageType.Info);
            }

            for (int i = 0; i < busesProp.arraySize; i++)
            {
                SerializedProperty element = busesProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    bool moveUp = false;
                    bool moveDown = false;
                    bool delete = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    using (new EditorGUI.DisabledScope(i <= 0))
                    {
                        if (GUILayout.Button("上移", GUILayout.Width(52f)))
                        {
                            moveUp = true;
                        }
                    }

                    using (new EditorGUI.DisabledScope(i >= busesProp.arraySize - 1))
                    {
                        if (GUILayout.Button("下移", GUILayout.Width(52f)))
                        {
                            moveDown = true;
                        }
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(52f)) && ConfirmDelete("删除总线", "确定要删除这个总线配置吗？"))
                    {
                        delete = true;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (moveUp)
                    {
                        busesProp.MoveArrayElement(i, i - 1);
                        break;
                    }

                    if (moveDown)
                    {
                        busesProp.MoveArrayElement(i, i + 1);
                        break;
                    }

                    if (delete)
                    {
                        busesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        /// <summary>绘制拖拽导入区域。</summary>
        private void DrawClipDropZone()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 42f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "拖拽 AudioClip 到这里批量创建内嵌条目", EditorStyles.helpBox);

            Event current = Event.current;
            if (!rect.Contains(current.mousePosition))
            {
                return;
            }

            List<AudioClip> clips = GetDraggedAudioClips();
            if (clips.Count == 0)
            {
                return;
            }

            if (current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                current.Use();
            }
            else if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                ImportClips(clips);
                current.Use();
            }
        }

        /// <summary>判断条目是否符合当前筛选。</summary>
        private bool ShouldShowCue(SerializedProperty element)
        {
            if (filterByChannel)
            {
                SerializedProperty channelProp = element.FindPropertyRelative("channel");
                if (channelProp == null || channelProp.enumValueIndex != (int)channelFilter)
                {
                    return false;
                }
            }

            if (filterIssuesOnly && !HasCueIssue(element))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            SerializedProperty keyProp = element.FindPropertyRelative("key");
            SerializedProperty nameProp = element.FindPropertyRelative("displayName");
            SerializedProperty groupProp = element.FindPropertyRelative("group");
            string key = keyProp != null ? keyProp.stringValue : string.Empty;
            string display = nameProp != null ? nameProp.stringValue : string.Empty;
            string group = groupProp != null ? groupProp.stringValue : string.Empty;
            return ContainsIgnoreCase(key, searchText) ||
                   ContainsIgnoreCase(display, searchText) ||
                   ContainsIgnoreCase(group, searchText);
        }

        /// <summary>判断条目是否存在常见配置问题。</summary>
        private bool HasCueIssue(SerializedProperty element)
        {
            SerializedProperty keyProp = element.FindPropertyRelative("key");
            SerializedProperty clipsProp = element.FindPropertyRelative("clips");
            string key = keyProp != null ? keyProp.stringValue : string.Empty;
            if (validationIssues != null && validationIssues.Count > 0 && !string.IsNullOrWhiteSpace(key))
            {
                for (int i = 0; i < validationIssues.Count; i++)
                {
                    if (string.Equals(validationIssues[i].CueKey, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            if (clipsProp == null || clipsProp.arraySize == 0)
            {
                return true;
            }

            return CountCueKey(key) > 1;
        }

        /// <summary>统计指定Key在内嵌条目中的出现次数。</summary>
        private int CountCueKey(string key)
        {
            SerializedProperty cuesProp = serializedDatabase != null ? serializedDatabase.FindProperty("cues") : null;
            if (cuesProp == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                SerializedProperty item = cuesProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = item.FindPropertyRelative("key");
                if (keyProp != null && string.Equals(keyProp.stringValue, key, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>复制一个音频条目。</summary>
        private void DuplicateCue(SerializedProperty cuesProp, int index)
        {
            cuesProp.InsertArrayElementAtIndex(index);
            cuesProp.MoveArrayElement(index, index + 1);
            SerializedProperty duplicate = cuesProp.GetArrayElementAtIndex(index + 1);
            SerializedProperty keyProp = duplicate.FindPropertyRelative("key");
            SerializedProperty nameProp = duplicate.FindPropertyRelative("displayName");
            duplicate.isExpanded = true;
            if (keyProp != null)
            {
                keyProp.stringValue = GenerateUniqueCueKey(string.IsNullOrWhiteSpace(keyProp.stringValue) ? "cue" : keyProp.stringValue + "_copy");
            }

            if (nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue))
            {
                nameProp.stringValue += " Copy";
            }

            SetStatus("已复制条目，并生成了新的 Key。", MessageType.Info);
        }

        /// <summary>确认删除操作。</summary>
        private bool ConfirmDelete(string title, string message)
        {
            return EditorUtility.DisplayDialog(title, message, "删除", "取消");
        }

        /// <summary>创建数据库资产。</summary>
        private void CreateDatabaseAsset()
        {
            if (!AssetDatabase.IsValidFolder(DefaultFolder))
            {
                CreateFolders(DefaultFolder);
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(DefaultFolder, "AudioDatabase.asset"));
            AudioDatabase asset = CreateInstance<AudioDatabase>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetDatabase(asset);
            Selection.activeObject = database;
            SetStatus($"已创建数据库：{path}", MessageType.Info);
        }

        /// <summary>创建独立Cue资产。</summary>
        private void CreateCueAsset()
        {
            if (!AssetDatabase.IsValidFolder(DefaultFolder))
            {
                CreateFolders(DefaultFolder);
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(DefaultFolder, "AudioCue.asset"));
            AudioCueAsset asset = CreateInstance<AudioCueAsset>();
            string assetName = Path.GetFileNameWithoutExtension(path);
            asset.Data.SetIdentity(GenerateUniqueCueKey(assetName), assetName);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddCueAssetReference(asset);
            Selection.activeObject = asset;
            SetStatus($"已创建独立 Cue：{path}", MessageType.Info);
        }

        /// <summary>从当前选中的音频剪辑批量创建条目。</summary>
        private void ImportSelectedClips()
        {
            List<AudioClip> clips = new List<AudioClip>();
            UnityEngine.Object[] selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                if (selectedObjects[i] is AudioClip clip)
                {
                    clips.Add(clip);
                }
            }

            if (clips.Count == 0)
            {
                EditorUtility.DisplayDialog("导入选中Clip", "请先在 Project 窗口中选择一个或多个 AudioClip。", "知道了");
                return;
            }

            ImportClips(clips);
        }

        /// <summary>批量导入剪辑。</summary>
        private void ImportClips(IReadOnlyList<AudioClip> clips)
        {
            int importedCount = 0;
            for (int i = 0; i < clips.Count; i++)
            {
                AudioClip clip = clips[i];
                if (clip == null)
                {
                    continue;
                }

                string key = GenerateUniqueCueKey(NormalizeKey(clip.name));
                AudioCueData cue = new AudioCueData();
                cue.SetIdentity(key, clip.name);
                cue.AddClip(clip);
                database.AddCue(cue);
                importedCount++;
            }

            serializedDatabase = new SerializedObject(database);
            EditorUtility.SetDirty(database);
            RunValidation();
            SetStatus($"已导入 {importedCount} 个 AudioClip。", MessageType.Info);
        }

        /// <summary>添加独立Cue引用。</summary>
        private void AddCueAssetReference(AudioCueAsset asset)
        {
            SerializedProperty cueAssetsProp = serializedDatabase.FindProperty("cueAssets");
            if (cueAssetsProp == null)
            {
                return;
            }

            int index = cueAssetsProp.arraySize;
            cueAssetsProp.InsertArrayElementAtIndex(index);
            cueAssetsProp.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            database.RebuildCache();
        }

        /// <summary>添加一个默认条目。</summary>
        private void AddCue()
        {
            SerializedProperty cuesProp = serializedDatabase.FindProperty("cues");
            if (cuesProp == null)
            {
                return;
            }

            int index = cuesProp.arraySize;
            cuesProp.InsertArrayElementAtIndex(index);
            SerializedProperty element = cuesProp.GetArrayElementAtIndex(index);
            SerializedProperty keyProp = element.FindPropertyRelative("key");
            SerializedProperty nameProp = element.FindPropertyRelative("displayName");
            element.isExpanded = true;
            if (keyProp != null)
            {
                keyProp.stringValue = GenerateUniqueCueKey($"cue_{index + 1}");
            }

            if (nameProp != null)
            {
                nameProp.stringValue = $"Cue {index + 1}";
            }

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            database.RebuildCache();
            SetStatus("已添加默认音频条目。", MessageType.Info);
        }

        /// <summary>添加一个默认总线。</summary>
        private void AddBus()
        {
            SerializedProperty busesProp = serializedDatabase.FindProperty("buses");
            if (busesProp == null)
            {
                return;
            }

            int index = busesProp.arraySize;
            busesProp.InsertArrayElementAtIndex(index);
            SerializedProperty element = busesProp.GetArrayElementAtIndex(index);
            SerializedProperty channelProp = element.FindPropertyRelative("channel");
            if (channelProp != null)
            {
                channelProp.enumValueIndex = Mathf.Clamp(index, 0, Enum.GetValues(typeof(AudioChannel)).Length - 1);
            }

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            database.RebuildCache();
            SetStatus("已添加默认总线配置。", MessageType.Info);
        }

        /// <summary>创建 CAudio 推荐总线和 Mixer。</summary>
        private void CreatePresetBusConfiguration()
        {
            string message = "将创建一个新的 CAudio 预设 AudioMixer，并用 Master、Music、Sfx、Voice、Ambience、Ui、Custom 重建当前数据库的总线配置。\n\n当前 Mixer 设置和总线列表会被替换。创建前请确认已经保存需要保留的配置，是否继续？";
            if (!EditorUtility.DisplayDialog("创建 CAudio 预设总线", message, "创建", "取消"))
            {
                return;
            }

            string templatePath = AssetDatabase.GUIDToAssetPath(PresetMixerTemplateGuid);
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                EditorUtility.DisplayDialog("创建 CAudio 预设总线", "找不到 CAudio 预设 Mixer 模板，请确认包内 Editor/Templates/CAudioPresetMixer.mixer 存在。", "知道了");
                return;
            }

            string targetFolder = ResolveDatabaseFolder();
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                CreateFolders(targetFolder);
            }

            string mixerPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, "CAudioPresetMixer.mixer").Replace('\\', '/'));
            if (!AssetDatabase.CopyAsset(templatePath, mixerPath))
            {
                EditorUtility.DisplayDialog("创建 CAudio 预设总线", $"复制 Mixer 模板失败：{mixerPath}", "知道了");
                return;
            }

            AssetDatabase.ImportAsset(mixerPath);
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if (mixer == null)
            {
                EditorUtility.DisplayDialog("创建 CAudio 预设总线", $"Mixer 创建后无法加载：{mixerPath}", "知道了");
                return;
            }

            Dictionary<string, AudioMixerGroup> groups = LoadMixerGroups(mixerPath);
            ApplyPresetMixerSettings(mixer);
            ApplyPresetBuses(groups);
            serializedDatabase.ApplyModifiedProperties();
            database.RebuildCache();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            RunValidation();
            Selection.activeObject = mixer;
            EditorGUIUtility.PingObject(mixer);
            SetStatus($"已创建 CAudio 预设 Mixer 并重建总线：{mixerPath}", MessageType.Info);
        }

        /// <summary>读取 Mixer 中的所有分组。</summary>
        private Dictionary<string, AudioMixerGroup> LoadMixerGroups(string mixerPath)
        {
            Dictionary<string, AudioMixerGroup> groups = new Dictionary<string, AudioMixerGroup>(StringComparer.OrdinalIgnoreCase);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(mixerPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AudioMixerGroup group && !groups.ContainsKey(group.name))
                {
                    groups.Add(group.name, group);
                }
            }

            return groups;
        }

        /// <summary>写入预设 Mixer 设置。</summary>
        private void ApplyPresetMixerSettings(AudioMixer mixer)
        {
            SerializedProperty mixerSettingsProp = serializedDatabase.FindProperty("mixerSettings");
            if (mixerSettingsProp == null)
            {
                return;
            }

            SerializedProperty mixerProp = mixerSettingsProp.FindPropertyRelative("mixer");
            if (mixerProp != null)
            {
                mixerProp.objectReferenceValue = mixer;
            }

            SerializedProperty duckingProp = mixerSettingsProp.FindPropertyRelative("enableVoiceDucking");
            if (duckingProp != null)
            {
                duckingProp.boolValue = true;
            }

            SerializedProperty duckVolumeProp = mixerSettingsProp.FindPropertyRelative("duckMusicVolume");
            if (duckVolumeProp != null)
            {
                duckVolumeProp.floatValue = 0.35f;
            }

            SerializedProperty duckSpeedProp = mixerSettingsProp.FindPropertyRelative("duckFadeSpeed");
            if (duckSpeedProp != null)
            {
                duckSpeedProp.floatValue = 8f;
            }
        }

        /// <summary>写入 CAudio 推荐总线。</summary>
        private void ApplyPresetBuses(Dictionary<string, AudioMixerGroup> groups)
        {
            SerializedProperty busesProp = serializedDatabase.FindProperty("buses");
            if (busesProp == null)
            {
                return;
            }

            AudioChannel[] channels =
            {
                AudioChannel.Master,
                AudioChannel.Music,
                AudioChannel.Sfx,
                AudioChannel.Voice,
                AudioChannel.Ambience,
                AudioChannel.Ui,
                AudioChannel.Custom
            };

            busesProp.arraySize = channels.Length;
            for (int i = 0; i < channels.Length; i++)
            {
                AudioChannel channel = channels[i];
                SerializedProperty element = busesProp.GetArrayElementAtIndex(i);
                SerializedProperty channelProp = element.FindPropertyRelative("channel");
                SerializedProperty outputGroupProp = element.FindPropertyRelative("outputGroup");
                SerializedProperty volumeProp = element.FindPropertyRelative("volume");
                SerializedProperty muteProp = element.FindPropertyRelative("mute");

                if (channelProp != null)
                {
                    channelProp.enumValueIndex = (int)channel;
                }

                if (outputGroupProp != null)
                {
                    outputGroupProp.objectReferenceValue = ResolvePresetGroup(groups, channel);
                }

                if (volumeProp != null)
                {
                    volumeProp.floatValue = ResolvePresetVolume(channel);
                }

                if (muteProp != null)
                {
                    muteProp.boolValue = false;
                }
            }
        }

        /// <summary>按通道读取预设 Mixer 分组。</summary>
        private AudioMixerGroup ResolvePresetGroup(Dictionary<string, AudioMixerGroup> groups, AudioChannel channel)
        {
            string groupName = channel == AudioChannel.Ui ? "UI" : channel.ToString();
            if (groups.TryGetValue(groupName, out AudioMixerGroup group))
            {
                return group;
            }

            groups.TryGetValue("Master", out AudioMixerGroup master);
            return master;
        }

        /// <summary>读取预设通道音量。</summary>
        private float ResolvePresetVolume(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music:
                    return 0.8f;
                case AudioChannel.Ambience:
                    return 0.65f;
                case AudioChannel.Ui:
                    return 0.85f;
                default:
                    return 1f;
            }
        }

        /// <summary>读取数据库所在目录。</summary>
        private string ResolveDatabaseFolder()
        {
            string databasePath = database != null ? AssetDatabase.GetAssetPath(database) : null;
            if (!string.IsNullOrWhiteSpace(databasePath))
            {
                if (databasePath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                {
                    return DefaultFolder;
                }

                string folder = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    return folder.Replace('\\', '/');
                }
            }

            return DefaultFolder;
        }

        /// <summary>生成不重复的条目键。</summary>
        private string GenerateUniqueCueKey(string baseKey)
        {
            string normalized = NormalizeKey(baseKey);
            string candidate = normalized;
            int suffix = 1;
            database?.RebuildCache();
            while (database != null && database.TryGetCue(candidate, out _))
            {
                candidate = $"{normalized}_{suffix}";
                suffix++;
            }

            return candidate;
        }

        /// <summary>规范化条目键。</summary>
        private string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "cue";
            }

            char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        /// <summary>定位校验问题来源。</summary>
        private void LocateIssue(AudioDatabaseValidationIssue issue)
        {
            if (!string.IsNullOrWhiteSpace(issue.CueKey))
            {
                searchText = issue.CueKey;
                filterIssuesOnly = false;
            }

            if (issue.Context != null)
            {
                Selection.activeObject = issue.Context;
                EditorGUIUtility.PingObject(issue.Context);
            }

            Repaint();
        }

        /// <summary>递归创建文件夹。</summary>
        private void CreateFolders(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private void SetDatabase(AudioDatabase value)
        {
            database = value;
            serializedDatabase = database != null ? new SerializedObject(database) : null;
            validationIssues = null;
            statusMessage = null;
        }

        private void SaveDatabase()
        {
            serializedDatabase?.ApplyModifiedProperties();
            database.RebuildCache();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            SetStatus("数据库已保存。", MessageType.Info);
        }

        private void RunValidation()
        {
            if (database == null)
            {
                return;
            }

            serializedDatabase?.ApplyModifiedProperties();
            database.RebuildCache();
            validationIssues = database.Validate();
            SetStatus(validationIssues.Count == 0 ? "校验通过，未发现问题。" : $"校验发现 {validationIssues.Count} 个问题。", validationIssues.Count == 0 ? MessageType.Info : MessageType.Warning);
        }

        private void ClearFilters()
        {
            searchText = string.Empty;
            filterByChannel = false;
            filterIssuesOnly = false;
            SetStatus("已清除筛选条件。", MessageType.Info);
        }

        private void SetAllCueExpanded(SerializedProperty cuesProp, bool expanded)
        {
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                cuesProp.GetArrayElementAtIndex(i).isExpanded = expanded;
            }

            SetStatus(expanded ? "已展开全部条目。" : "已折叠全部条目。", MessageType.Info);
        }

        private void NormalizeAllCueKeys(SerializedProperty cuesProp)
        {
            HashSet<string> usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int changed = 0;
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                SerializedProperty element = cuesProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("key");
                if (keyProp == null)
                {
                    continue;
                }

                string normalized = NormalizeKey(keyProp.stringValue);
                string candidate = normalized;
                int suffix = 1;
                while (!usedKeys.Add(candidate))
                {
                    candidate = $"{normalized}_{suffix}";
                    suffix++;
                }

                if (!string.Equals(keyProp.stringValue, candidate, StringComparison.Ordinal))
                {
                    keyProp.stringValue = candidate;
                    changed++;
                }
            }

            serializedDatabase.ApplyModifiedProperties();
            database.RebuildCache();
            EditorUtility.SetDirty(database);
            RunValidation();
            SetStatus($"已规范化 {changed} 个 Key。", MessageType.Info);
        }

        private void RemoveNullReferences(SerializedProperty arrayProp, string label)
        {
            int removed = 0;
            for (int i = arrayProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty item = arrayProp.GetArrayElementAtIndex(i);
                if (item.objectReferenceValue != null)
                {
                    continue;
                }

                arrayProp.DeleteArrayElementAtIndex(i);
                removed++;
            }

            serializedDatabase.ApplyModifiedProperties();
            database.RebuildCache();
            EditorUtility.SetDirty(database);
            RunValidation();
            SetStatus($"已移除 {removed} 个空{label}引用。", MessageType.Info);
        }

        private int CountVisibleCues()
        {
            SerializedProperty cuesProp = serializedDatabase != null ? serializedDatabase.FindProperty("cues") : null;
            if (cuesProp == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                if (ShouldShowCue(cuesProp.GetArrayElementAtIndex(i)))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetArraySize(string propertyName)
        {
            SerializedProperty property = serializedDatabase != null ? serializedDatabase.FindProperty(propertyName) : null;
            return property != null && property.isArray ? property.arraySize : 0;
        }

        private List<AudioClip> GetDraggedAudioClips()
        {
            List<AudioClip> clips = new List<AudioClip>();
            UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences;
            for (int i = 0; i < draggedObjects.Length; i++)
            {
                if (draggedObjects[i] is AudioClip clip)
                {
                    clips.Add(clip);
                }
            }

            return clips;
        }

        private bool ContainsIgnoreCase(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(query) &&
                   value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawAssetPath()
        {
            string path = AssetDatabase.GetAssetPath(database);
            GUILayout.Label(string.IsNullOrWhiteSpace(path) ? "Unsaved Asset" : path, mutedStyle);
        }

        private void DrawValidationToolbarStatus()
        {
            if (validationIssues == null)
            {
                GUILayout.Label("校验：未运行", mutedStyle, GUILayout.Width(84f));
                return;
            }

            GUILayout.Label(validationIssues.Count == 0 ? "校验：通过" : $"校验：{validationIssues.Count} 个问题", mutedStyle, GUILayout.Width(110f));
        }

        private void DrawStatBadge(string label, string value)
        {
            GUILayout.Label($"{label}: {value}", badgeStyle, GUILayout.Height(22f));
        }

        private void DrawBadge(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            GUILayout.Label(value, badgeStyle, GUILayout.Height(20f));
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private void EnsureStyles()
        {
            if (sectionStyle != null)
            {
                return;
            }

            sectionStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(6, 6, 6, 6)
            };

            headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };

            mutedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };

            badgeStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 1, 1),
                margin = new RectOffset(2, 2, 1, 1)
            };
        }
    }
}
#endif
