using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.InspectorAutoAssigner
{
    // Custom popup window class.
    public class AutoAssignmentObjectSelectorPopup : PopupWindowContent
    {
        private const int MaxVisibleItems = 10; // Maximum number of visible items.
        private const float ItemHeight = 20f; // Height of each item.
        private const float AssignBtnWidth = 30f;
        private const float ScrollMargin = 25f;
        private const float ItemSpacing = 2f; // Item spacing.
        private const float BottomMargin = 5f; // Bottom margin.
        private const float NonScrollMargin = 10f; // Margin for non-scrollable case.
        private const float NonScrollMarginForWidth = 30f; // Margin for width calculation in non-scrollable case.
        private const string ArrowTexturePath = "Packages/io.github.hatayama.inspectorautoassigner/Editor/Images/arrow.png";

        private SerializedProperty _property;
        private Object[] _objects;
        private Vector2 _scrollPos;
        private float _windowWidth; // Window width.
        private bool _needsScroll;
        private Texture2D _arrowTexture;

        public AutoAssignmentObjectSelectorPopup(SerializedProperty property, Object[] objects)
        {
            _property = property;
            _objects = objects;
            
            // Load texture using UPM path format
            _arrowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArrowTexturePath);

            // Determine if scrolling is needed.
            _needsScroll = _objects.Length > MaxVisibleItems;
            // Calculate the window width.
            CalculateWindowWidth();
        }

        public override Vector2 GetWindowSize()
        {
            int itemCount = Mathf.Min(_objects.Length, MaxVisibleItems);
            float windowHeight = (itemCount * ItemHeight) + ((itemCount - 1) * ItemSpacing) + BottomMargin; // Item height + item spacing + bottom margin.
            return new Vector2(_windowWidth, windowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            // Close the window if clicked outside the menu.
            if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
            {
                editorWindow.Close();
            }

            if (_needsScroll)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            }

            for (int i = 0; i < _objects.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Display the green frame (arrow) button first.
                if (GUILayout.Button(new GUIContent(_arrowTexture), GUILayout.Width(AssignBtnWidth), GUILayout.Height(ItemHeight)))
                {
                    AssignValue(_property, _objects[i]);
                    editorWindow.Close();
                }

                // Display the red frame (object name) button later.
                float margin = _needsScroll ? ScrollMargin : NonScrollMargin;
                var width = _windowWidth - AssignBtnWidth - margin; // Window width - assignment button width - margin.
                if (GUILayout.Button(_objects[i].name, GUILayout.Width(width), GUILayout.Height(ItemHeight)))
                {
                    EditorGUIUtility.PingObject(_objects[i]);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_needsScroll)
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void CalculateWindowWidth()
        {
            // Calculate the maximum width of the object name.
            float maxNameWidth = 0f;
            GUIStyle buttonStyle = GUI.skin.button;
            foreach (Object obj in _objects)
            {
                string name = obj.name;
                float nameWidth = buttonStyle.CalcSize(new GUIContent(name)).x;
                if (nameWidth > maxNameWidth)
                {
                    maxNameWidth = nameWidth;
                }
            }

            // Calculate the total window width (object name button + assignment button + margin).
            float margin = _needsScroll ? ScrollMargin : NonScrollMarginForWidth;
            _windowWidth = maxNameWidth + AssignBtnWidth + margin; // Assignment button width + margin.
        }

        private void AssignValue(SerializedProperty property, Object value)
        {
            // Assign the value.
            property.objectReferenceValue = value;
            // Apply the changes.
            property.serializedObject.ApplyModifiedProperties();
        }
    }

    // Popup window class for when nothing is found.
    public class AutoAssignmentMessagePopup : PopupWindowContent
    {
        private string _message;

        public AutoAssignmentMessagePopup(string message)
        {
            _message = message;
        }

        public override Vector2 GetWindowSize()
        {
            GUIStyle style = GUI.skin.label;
            Vector2 size = style.CalcSize(new GUIContent(_message));
            return new Vector2(size.x + 20f, size.y + 20f); // Add some padding.
        }

        public override void OnGUI(Rect rect)
        {
            // Display the message centered.
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.FlexibleSpace();
            GUILayout.Label(_message, style);
            GUILayout.FlexibleSpace();
        }
    }
}