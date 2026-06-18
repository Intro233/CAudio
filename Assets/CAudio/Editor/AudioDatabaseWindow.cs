#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CAudio.EditorTools
{
    /// <summary>音频数据库管理窗口。</summary>
    public sealed class AudioDatabaseWindow : EditorWindow
    {
        private const string DefaultFolder = "Assets/CAudio/Database";

        private AudioDatabase database;
        private SerializedObject serializedDatabase;
        private Vector2 scrollPosition;
        private string searchText;
        private bool filterByChannel;
        private bool filterIssuesOnly;
        private AudioChannel channelFilter = AudioChannel.Sfx;
        private System.Collections.Generic.List<AudioDatabaseValidationIssue> validationIssues;

        /// <summary>打开窗口。</summary>
        [MenuItem("CAudio/Audio Database")]
        public static void Open()
        {
            GetWindow<AudioDatabaseWindow>("Audio Database");
        }

        /// <summary>绘制窗口界面。</summary>
        private void OnGUI()
        {
            DrawToolbar();

            if (database == null)
            {
                EditorGUILayout.HelpBox("请选择或创建一个 AudioDatabase。", MessageType.Info);
                return;
            }

            if (serializedDatabase == null || serializedDatabase.targetObject != database)
            {
                serializedDatabase = new SerializedObject(database);
            }

            serializedDatabase.Update();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawDatabaseStats();
            DrawCueList();
            DrawCueAssetList();
            DrawBusList();
            DrawMixerSettings();
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

            AudioDatabase selected = (AudioDatabase)EditorGUILayout.ObjectField(database, typeof(AudioDatabase), false, GUILayout.MinWidth(180f));
            if (selected != database)
            {
                database = selected;
                serializedDatabase = database != null ? new SerializedObject(database) : null;
            }

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                CreateDatabaseAsset();
            }

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60f)) && database != null)
            {
                database.RebuildCache();
                EditorUtility.SetDirty(database);
            }

            if (GUILayout.Button("添加条目", EditorStyles.toolbarButton, GUILayout.Width(80f)) && database != null)
            {
                AddCue();
            }

            if (GUILayout.Button("新建Cue", EditorStyles.toolbarButton, GUILayout.Width(80f)) && database != null)
            {
                CreateCueAsset();
            }

            if (GUILayout.Button("导入选中Clip", EditorStyles.toolbarButton, GUILayout.Width(100f)) && database != null)
            {
                ImportSelectedClips();
            }

            if (GUILayout.Button("添加总线", EditorStyles.toolbarButton, GUILayout.Width(80f)) && database != null)
            {
                AddBus();
            }

            if (GUILayout.Button("校验", EditorStyles.toolbarButton, GUILayout.Width(60f)) && database != null)
            {
                validationIssues = database.Validate();
            }

            filterByChannel = GUILayout.Toggle(filterByChannel, "按通道", EditorStyles.toolbarButton, GUILayout.Width(60f));
            using (new EditorGUI.DisabledScope(!filterByChannel))
            {
                channelFilter = (AudioChannel)EditorGUILayout.EnumPopup(channelFilter, GUILayout.Width(90f));
            }

            filterIssuesOnly = GUILayout.Toggle(filterIssuesOnly, "仅问题", EditorStyles.toolbarButton, GUILayout.Width(60f));

            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160f));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>绘制数据库统计。</summary>
        private void DrawDatabaseStats()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("数据库信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("内嵌条目数量", database.Cues != null ? database.Cues.Count.ToString() : "0");
                EditorGUILayout.LabelField("独立Cue数量", database.CueAssets != null ? database.CueAssets.Count.ToString() : "0");
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("音频条目", EditorStyles.boldLabel);
                DrawCueElements(cuesProp);
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("总线配置", EditorStyles.boldLabel);
                DrawBusElements(busesProp);
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("独立Cue资产", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(cueAssetsProp, true);
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("混音器与Ducking", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(mixerProp, true);
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

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("调试配置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(debugProp, true);
            }
        }

        /// <summary>绘制校验结果。</summary>
        private void DrawValidationIssues()
        {
            if (validationIssues == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("校验结果", EditorStyles.boldLabel);
                if (validationIssues.Count == 0)
                {
                    EditorGUILayout.HelpBox("未发现问题。", MessageType.Info);
                    return;
                }

                for (int i = 0; i < validationIssues.Count; i++)
                {
                    AudioDatabaseValidationIssue issue = validationIssues[i];
                    MessageType type = issue.Level == AudioLogLevel.Error ? MessageType.Error : MessageType.Warning;
                    EditorGUILayout.HelpBox(issue.Message, type);
                }
            }
        }

        /// <summary>绘制全部条目。</summary>
        private void DrawCueElements(SerializedProperty cuesProp)
        {
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                SerializedProperty element = cuesProp.GetArrayElementAtIndex(i);
                if (!ShouldShowCue(element))
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    bool duplicate = false;
                    bool moveUp = false;
                    bool moveDown = false;
                    bool delete = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    if (GUILayout.Button("复制", GUILayout.Width(40f)))
                    {
                        duplicate = true;
                    }

                    if (GUILayout.Button("上移", GUILayout.Width(50f)) && i > 0)
                    {
                        moveUp = true;
                    }

                    if (GUILayout.Button("下移", GUILayout.Width(50f)) && i < cuesProp.arraySize - 1)
                    {
                        moveDown = true;
                    }

                    if (GUILayout.Button("删", GUILayout.Width(30f)) && ConfirmDelete("删除音频条目", "确定要删除这个音频条目吗？"))
                    {
                        delete = true;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (duplicate)
                    {
                        DuplicateCue(cuesProp, i);
                        break;
                    }

                    if (moveUp)
                    {
                        cuesProp.MoveArrayElement(i, i - 1);
                        break;
                    }

                    if (moveDown)
                    {
                        cuesProp.MoveArrayElement(i, i + 1);
                        break;
                    }

                    if (delete)
                    {
                        cuesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }

        /// <summary>绘制全部总线。</summary>
        private void DrawBusElements(SerializedProperty busesProp)
        {
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
                    if (GUILayout.Button("上移", GUILayout.Width(50f)) && i > 0)
                    {
                        moveUp = true;
                    }

                    if (GUILayout.Button("下移", GUILayout.Width(50f)) && i < busesProp.arraySize - 1)
                    {
                        moveDown = true;
                    }

                    if (GUILayout.Button("删", GUILayout.Width(30f)) && ConfirmDelete("删除总线", "确定要删除这个总线配置吗？"))
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

        /// <summary>绘制筛选后的条目。</summary>
        private void DrawFilteredCues(SerializedProperty cuesProp)
        {
            EditorGUILayout.LabelField("筛选结果");
            for (int i = 0; i < cuesProp.arraySize; i++)
            {
                SerializedProperty element = cuesProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("key");
                SerializedProperty nameProp = element.FindPropertyRelative("displayName");
                string key = keyProp != null ? keyProp.stringValue : string.Empty;
                string display = nameProp != null ? nameProp.stringValue : string.Empty;
                if (key.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    display.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(element, true);
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
            string key = keyProp != null ? keyProp.stringValue : string.Empty;
            string display = nameProp != null ? nameProp.stringValue : string.Empty;
            return key.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   display.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>判断条目是否存在常见配置问题。</summary>
        private bool HasCueIssue(SerializedProperty element)
        {
            SerializedProperty keyProp = element.FindPropertyRelative("key");
            SerializedProperty clipsProp = element.FindPropertyRelative("clips");
            string key = keyProp != null ? keyProp.stringValue : string.Empty;
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
                if (keyProp != null && string.Equals(keyProp.stringValue, key, System.StringComparison.OrdinalIgnoreCase))
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
            if (keyProp != null)
            {
                keyProp.stringValue = GenerateUniqueCueKey(string.IsNullOrWhiteSpace(keyProp.stringValue) ? "cue" : keyProp.stringValue + "_copy");
            }

            if (nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue))
            {
                nameProp.stringValue += " Copy";
            }
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
            database = asset;
            serializedDatabase = new SerializedObject(database);
            Selection.activeObject = database;
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
            asset.Data.SetIdentity(Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), Path.GetFileNameWithoutExtension(path));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddCueAssetReference(asset);
            Selection.activeObject = asset;
        }

        /// <summary>从当前选中的音频剪辑批量创建条目。</summary>
        private void ImportSelectedClips()
        {
            int importedCount = 0;
            Object[] selectedObjects = Selection.objects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                AudioClip clip = selectedObjects[i] as AudioClip;
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

            if (importedCount == 0)
            {
                EditorUtility.DisplayDialog("导入选中Clip", "请先在 Project 窗口中选择一个或多个 AudioClip。", "知道了");
                return;
            }

            serializedDatabase = new SerializedObject(database);
            EditorUtility.SetDirty(database);
            validationIssues = database.Validate();
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
        }

        /// <summary>生成不重复的条目键。</summary>
        private string GenerateUniqueCueKey(string baseKey)
        {
            string normalized = NormalizeKey(baseKey);
            string candidate = normalized;
            int suffix = 1;
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
                channelProp.enumValueIndex = Mathf.Clamp(index, 0, System.Enum.GetValues(typeof(AudioChannel)).Length - 1);
            }

            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            database.RebuildCache();
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
    }
}
#endif
