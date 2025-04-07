using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace io.github.hatayama
{
    [CustomPropertyDrawer(typeof(GameObject), true)]
    [CustomPropertyDrawer(typeof(Component), true)]
    public class AutoAssignmentObjectField : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type fieldType = fieldInfo.FieldType;
            // フィールドが配列またはリストの場合、デフォルトの描画を行い、カスタムボタンを追加しない
            if (fieldType.IsArray || (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                DrawObjectFieldWithButton(position, property, label, fieldType);
            }
        }

        private void DrawObjectFieldWithButton(Rect position, SerializedProperty property, GUIContent label, Type fieldType)
        {
            // プロパティの開始
            EditorGUI.BeginProperty(position, label, property);

            // ラベルとフィールドの Rect を取得
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // インデントレベルを保存して0に設定（インデントの影響を受けないように）
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // ボタンの幅を定義
            float buttonWidth = 25;

            // フィールド全体の幅
            float fieldWidth = position.width;

            // ObjectField の幅を計算（ボタンの幅を引く）
            float objectFieldWidth = fieldWidth - buttonWidth;

            // ObjectField の描画エリア
            Rect objectFieldRect = new Rect(position.x, position.y, objectFieldWidth, position.height);

            // ボタンの描画エリア
            Rect buttonRect = new Rect(position.x + objectFieldWidth, position.y, buttonWidth, position.height);

            // ObjectField を描画
            EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);

            // ボタンを描画
            if (GUI.Button(buttonRect, EditorGUIUtility.FindTexture("Search Icon")))
            {
                // ボタンが押されたときの処理
                HandleButtonPress(property, fieldType, buttonRect);
            }

            // インデントレベルを元に戻す
            EditorGUI.indentLevel = indent;

            // プロパティの終了
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// ボタンが押されたときの処理
        /// </summary>
        /// <param name="property">対象の SerializedProperty</param>
        /// <param name="fieldType">フィールドの型</param>
        /// <param name="buttonRect">ボタンの Rect</param>
        private void HandleButtonPress(SerializedProperty property, Type fieldType, Rect buttonRect)
        {
            if (fieldType == typeof(GameObject) || fieldType.IsSubclassOf(typeof(GameObject)))
            {
                HandleGameObjectAssignment(property, buttonRect);
                return;
            }
            if (fieldType.IsSubclassOf(typeof(Component)))
            {
                HandleComponentAssignment(property, fieldType, buttonRect);
                return;
            }
        }

        private void HandleGameObjectAssignment(SerializedProperty property, Rect buttonRect)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (!(targetObject is MonoBehaviour)) return;

            MonoBehaviour monoBehaviour = (MonoBehaviour)targetObject;
            GameObject gameObject = monoBehaviour.gameObject;

            List<GameObject> gameObjects = new List<GameObject> { gameObject };
            GetChildGameObjects(gameObject.transform, gameObjects);

            if (gameObjects.Count == 0)
            {
                PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup("GameObject が見つかりませんでした。"));
                return;
            }

            if (gameObjects.Count == 1)
            {
                AssignValue(property, gameObjects[0]);
                return;
            }

            string cleanName = GetCleanPropertyName(property.name);
            GameObject matchingObject = gameObjects.Find((GameObject go) => string.Equals(go.name, cleanName, StringComparison.OrdinalIgnoreCase));
            if (matchingObject != null)
            {
                AssignValue(property, matchingObject);
                return;
            }

            PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, gameObjects.ToArray()));
        }

        private void HandleComponentAssignment(SerializedProperty property, Type fieldType, Rect buttonRect)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (!(targetObject is MonoBehaviour)) return;

            MonoBehaviour monoBehaviour = (MonoBehaviour)targetObject;
            GameObject gameObject = monoBehaviour.gameObject;

            Component[] components = gameObject.GetComponentsInChildren(fieldType, true);
            if (components == null || components.Length == 0)
            {
                PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{fieldType.Name} コンポーネントが見つかりませんでした。"));
                return;
            }

            if (components.Length == 1)
            {
                AssignValue(property, components[0]);
                return;
            }

            string cleanName = GetCleanPropertyName(property.name);
            Component matchingComponent = Array.Find(components, comp => string.Equals(comp.gameObject.name, cleanName, StringComparison.OrdinalIgnoreCase));
            if (matchingComponent != null)
            {
                AssignValue(property, matchingComponent);
            }
            else
            {
                PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, components));
            }
        }

        private string GetCleanPropertyName(string propertyName)
        {
            int underscoreIndex = propertyName.IndexOf('_');
            return underscoreIndex >= 0 ? propertyName.Substring(underscoreIndex + 1) : propertyName;
        }

        /// <summary>
        /// プロパティに値を代入する
        /// </summary>
        private void AssignValue(SerializedProperty property, UnityEngine.Object value)
        {
            // 値を代入
            property.objectReferenceValue = value;
            // 変更を適用
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 子オブジェクトを取得する（再帰）
        /// </summary>
        private void GetChildGameObjects(Transform parent, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child.gameObject);
                // 子孫も取得する場合は再帰呼び出し
                GetChildGameObjects(child, list);
            }
        }
    }
}