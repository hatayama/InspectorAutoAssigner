using UnityEngine;
using UnityEditor;

namespace io.github.hatayama
{
    // カスタムポップアップウィンドウクラス
    public class AutoAssignmentObjectSelectorPopup : PopupWindowContent
    {
        private const int MaxVisibleItems = 10; // 最大表示アイテム数
        private const float ItemHeight = 20f; // 各アイテムの高さ
        private const float AssignBtnWidth = 50f;
        private const float ScrollMargin = 25f;

        private SerializedProperty _property;
        private Object[] _objects;
        private Vector2 _scrollPos;
        private float _windowWidth; // ウィンドウの幅
        private bool _needsScroll;

        public AutoAssignmentObjectSelectorPopup(SerializedProperty property, Object[] objects)
        {
            _property = property;
            _objects = objects;

            // スクロールが必要か判定
            _needsScroll = _objects.Length > MaxVisibleItems;
            // ウィンドウの幅を計算
            CalculateWindowWidth();
        }

        public override Vector2 GetWindowSize()
        {
            int itemCount = Mathf.Min(_objects.Length, MaxVisibleItems);
            float windowHeight = itemCount * ItemHeight + 10f; // 余白を追加
            return new Vector2(_windowWidth, windowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            // メニュー外をクリックしたらウィンドウを閉じる
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

                float margin = _needsScroll ? ScrollMargin : 10f;
                var width = _windowWidth - AssignBtnWidth - margin; // ウィンドウ幅 - 代入ボタン幅 - 余白
                                                                    // オブジェクト名のボタン（クリックで Ping）
                if (GUILayout.Button(_objects[i].name, GUILayout.Width(width)))
                {
                    EditorGUIUtility.PingObject(_objects[i]);
                }

                if (GUILayout.Button("代入", GUILayout.Width(AssignBtnWidth)))
                {
                    AssignValue(_property, _objects[i]);
                    editorWindow.Close();
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
            // オブジェクト名の最大幅を計算
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

            // 総ウィンドウ幅を計算（オブジェクト名ボタン + 代入ボタン + マージン）
            float margin = _needsScroll ? ScrollMargin : 30f;
            _windowWidth = maxNameWidth + AssignBtnWidth + margin; // 代入ボタン幅 + 余白
        }

        private void AssignValue(SerializedProperty property, Object value)
        {
            // 値を代入
            property.objectReferenceValue = value;
            // 変更を適用
            property.serializedObject.ApplyModifiedProperties();
        }
    }

    // 見つからなかった場合のポップアップウィンドウクラス
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
            return new Vector2(size.x + 20f, size.y + 20f); // 余裕を持たせる
        }

        public override void OnGUI(Rect rect)
        {
            // メッセージを中央揃えで表示
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.FlexibleSpace();
            GUILayout.Label(_message, style);
            GUILayout.FlexibleSpace();
        }
    }
}