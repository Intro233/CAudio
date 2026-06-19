#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CAudio.EditorTools
{
    /// <summary>音频条目绘制器。</summary>
    [CustomPropertyDrawer(typeof(AudioCueData))]
    public sealed class AudioCueDataDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private static readonly GUIContent[] SelectionModeLabels =
        {
            new GUIContent("随机"),
            new GUIContent("加权随机"),
            new GUIContent("顺序播放"),
            new GUIContent("洗牌播放"),
            new GUIContent("避免连续重复")
        };

        /// <summary>绘制属性。</summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            property.isExpanded = EditorGUI.Foldout(GetLineRect(ref position), property.isExpanded, ResolveTitle(property, label), true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            DrawProperty(ref position, property, "key", "Key");
            DrawProperty(ref position, property, "displayName", "显示名");
            DrawProperty(ref position, property, "group", "分组");
            DrawProperty(ref position, property, "channel", "通道");
            DrawSelectionMode(ref position, property);
            DrawProperty(ref position, property, "clips", "剪辑", true);
            DrawProperty(ref position, property, "volumeRange", "音量范围");
            DrawProperty(ref position, property, "pitchRange", "音调范围");
            DrawProperty(ref position, property, "loop", "循环");
            DrawProperty(ref position, property, "replaceSameChannel", "替换同通道");
            DrawProperty(ref position, property, "cooldown", "冷却时间");
            DrawProperty(ref position, property, "maxSimultaneous", "最大同时播放数");
            DrawProperty(ref position, property, "fadeInTime", "淡入");
            DrawProperty(ref position, property, "fadeOutTime", "淡出");
            DrawProperty(ref position, property, "spatialBlend", "空间化");
            DrawProperty(ref position, property, "minDistance", "最小距离");
            DrawProperty(ref position, property, "maxDistance", "最大距离");
            DrawProperty(ref position, property, "priority", "优先级");
            DrawProperty(ref position, property, "outputGroup", "输出组");
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        /// <summary>获取属性高度。</summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + Spacing;
            if (!property.isExpanded)
            {
                return height;
            }

            height += GetPropertyHeight(property, "key");
            height += GetPropertyHeight(property, "displayName");
            height += GetPropertyHeight(property, "group");
            height += GetPropertyHeight(property, "channel");
            height += GetPropertyHeight(property, "selectionMode");
            height += GetPropertyHeight(property, "clips", true);
            height += GetPropertyHeight(property, "volumeRange");
            height += GetPropertyHeight(property, "pitchRange");
            height += GetPropertyHeight(property, "loop");
            height += GetPropertyHeight(property, "replaceSameChannel");
            height += GetPropertyHeight(property, "cooldown");
            height += GetPropertyHeight(property, "maxSimultaneous");
            height += GetPropertyHeight(property, "fadeInTime");
            height += GetPropertyHeight(property, "fadeOutTime");
            height += GetPropertyHeight(property, "spatialBlend");
            height += GetPropertyHeight(property, "minDistance");
            height += GetPropertyHeight(property, "maxDistance");
            height += GetPropertyHeight(property, "priority");
            height += GetPropertyHeight(property, "outputGroup");
            return height;
        }

        /// <summary>绘制单个子属性。</summary>
        private void DrawProperty(ref Rect position, SerializedProperty property, string relativeName, string displayName, bool includeChildren = false)
        {
            SerializedProperty child = property.FindPropertyRelative(relativeName);
            if (child == null)
            {
                return;
            }

            float height = EditorGUI.GetPropertyHeight(child, includeChildren);
            Rect rect = new Rect(position.x, position.y, position.width, height);
            EditorGUI.PropertyField(rect, child, new GUIContent(displayName), includeChildren);
            position.y += height + Spacing;
        }

        /// <summary>绘制剪辑选择方式。</summary>
        private void DrawSelectionMode(ref Rect position, SerializedProperty property)
        {
            SerializedProperty child = property.FindPropertyRelative("selectionMode");
            if (child == null)
            {
                return;
            }

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int index = Mathf.Clamp(child.enumValueIndex, 0, SelectionModeLabels.Length - 1);
            child.enumValueIndex = EditorGUI.Popup(rect, new GUIContent("选择方式"), index, SelectionModeLabels);
            position.y += EditorGUIUtility.singleLineHeight + Spacing;
        }

        /// <summary>获取单个子属性高度。</summary>
        private float GetPropertyHeight(SerializedProperty property, string relativeName, bool includeChildren = false)
        {
            SerializedProperty child = property.FindPropertyRelative(relativeName);
            return child != null ? EditorGUI.GetPropertyHeight(child, includeChildren) + Spacing : 0f;
        }

        /// <summary>获取一行矩形。</summary>
        private Rect GetLineRect(ref Rect position)
        {
            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            position.y += EditorGUIUtility.singleLineHeight + Spacing;
            return rect;
        }

        /// <summary>读取折叠标题。</summary>
        private GUIContent ResolveTitle(SerializedProperty property, GUIContent fallback)
        {
            SerializedProperty keyProp = property.FindPropertyRelative("key");
            SerializedProperty nameProp = property.FindPropertyRelative("displayName");
            SerializedProperty groupProp = property.FindPropertyRelative("group");
            SerializedProperty channelProp = property.FindPropertyRelative("channel");
            SerializedProperty clipsProp = property.FindPropertyRelative("clips");

            if (keyProp != null && !string.IsNullOrWhiteSpace(keyProp.stringValue))
            {
                string title = keyProp.stringValue;
                if (nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue))
                {
                    title += $"  ·  {nameProp.stringValue}";
                }

                if (groupProp != null && !string.IsNullOrWhiteSpace(groupProp.stringValue))
                {
                    title += $"  ·  {groupProp.stringValue}";
                }

                if (channelProp != null &&
                    channelProp.enumValueIndex >= 0 &&
                    channelProp.enumValueIndex < channelProp.enumDisplayNames.Length)
                {
                    title += $"  ·  {channelProp.enumDisplayNames[channelProp.enumValueIndex]}";
                }

                if (clipsProp != null)
                {
                    title += $"  ·  {clipsProp.arraySize} Clip";
                }

                return new GUIContent(title);
            }

            return fallback;
        }
    }
}
#endif
