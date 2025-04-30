using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace io.github.hatayama
{
    /// <summary>
    /// GameObject または Component 型のプロパティフィールドの右側に自動割り当てボタンを追加するカスタムプロパティドロワーです。
    /// UnityエディタのInspector表示モードに応じて、以下のメソッドが呼び出されます。
    /// - UI Toolkit が有効な場合: CreatePropertyGUI() が呼び出されます (OnGUI() は無視されます)。
    /// - 従来の IMGUI が有効な場合: OnGUI() が呼び出されます (CreatePropertyGUI() は無視されます)。
    /// 他のinspector拡張系のOSSとの互換性のため、2つの処理を用意しています。
    /// </summary>
    [CustomPropertyDrawer(typeof(GameObject), true)]
    [CustomPropertyDrawer(typeof(Component), true)]
    public class AutoAssignmentObjectField : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Type fieldType = fieldInfo.FieldType;
            if (IsListOrArray(fieldType))
            {
                return new PropertyField(property);
            }

            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            PropertyField propertyField = new PropertyField(property);
            propertyField.style.flexGrow = 1;
            container.Add(propertyField);

            Button searchButton = new Button();
            float buttonSize = EditorGUIUtility.singleLineHeight;
            searchButton.style.width = buttonSize;
            searchButton.style.height = buttonSize;
            searchButton.style.marginRight = 0;
            searchButton.style.alignItems = Align.Center;
            Image iconImage = new Image
            {
                image = EditorGUIUtility.FindTexture("Search Icon"),
                scaleMode = ScaleMode.ScaleToFit
            };
            iconImage.style.width = buttonSize * 0.7f;
            iconImage.style.height = buttonSize * 0.7f;

            searchButton.Clear();
            searchButton.Add(iconImage);

            searchButton.clicked += () => 
            { 
                HandleButtonPress(property, fieldInfo.FieldType, searchButton.worldBound);
            };
            container.Add(searchButton);
            
            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type fieldType = fieldInfo.FieldType;
            if (IsListOrArray(fieldType))
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
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float buttonSize = 24f;
            float buttonWidth = buttonSize;
            float objectFieldWidth = position.width - buttonWidth;

            Rect objectFieldRect = new Rect(position.x, position.y, objectFieldWidth, position.height);
            Rect buttonRect = new Rect(position.x + objectFieldWidth, position.y, buttonWidth, position.height);

            EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);

            GUIContent searchIconContent = EditorGUIUtility.IconContent("Search Icon");
            if (GUI.Button(buttonRect, searchIconContent))
            {
                HandleButtonPress(property, fieldType, buttonRect);
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        private void HandleButtonPress(SerializedProperty property, Type fieldType, Rect buttonWorldBound)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            if (!(targetObject is Component component)) return;

            if (fieldType == typeof(GameObject) || fieldType.IsSubclassOf(typeof(GameObject)))
            {
                HandleGameObjectAssignment(property, buttonWorldBound, component);
                return;
            }
            
            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                HandleComponentAssignment(property, fieldType, buttonWorldBound, component);
                return;
            }
        }

        private void HandleGameObjectAssignment(SerializedProperty property, Rect buttonRect, Component component)
        {
            GameObject targetGameObject = component.gameObject;

            List<GameObject> gameObjects = new List<GameObject>();
            GetSelfAndChildGameObjects(targetGameObject.transform, gameObjects);

            ProcessAssignmentCandidates<GameObject>(
                property,
                buttonRect,
                gameObjects,
                "GameObject",
                (go, name) => string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase)
            );
        }

        private void HandleComponentAssignment(SerializedProperty property, Type fieldType, Rect buttonRect, Component component)
        {
            GameObject targetGameObject = component.gameObject;

            List<Component> allComponents = new List<Component>();
            targetGameObject.GetComponentsInChildren(true, allComponents);

            List<Component> filteredComponents = allComponents.Where(c => fieldType.IsAssignableFrom(c.GetType())).ToList();

            if (filteredComponents.Count == 0)
            {
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{fieldType.Name} コンポーネントが見つかりませんでした."));
                return;
            }

            if (filteredComponents.Count == 1)
            {
                AssignValue(property, filteredComponents[0]);
                return;
            }

            ProcessAssignmentCandidates<Component>(
                property,
                buttonRect,
                filteredComponents,
                fieldType.Name,
                (comp, name) => string.Equals(comp.gameObject.name, name, StringComparison.OrdinalIgnoreCase)
            );
        }

        private void ProcessAssignmentCandidates<T>(
            SerializedProperty property,
            Rect buttonRect,
            IEnumerable<T> candidates,
            string typeName,
            Func<T, string, bool> nameMatcher
        ) where T : UnityEngine.Object
        {
            List<T> candidateList = candidates.ToList();

            if (candidateList.Count == 0)
            {
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{typeName} が見つかりませんでした."));
                return;
            }

            string cleanName = GetCleanPropertyName(property.name);

            T matchingCandidate = candidateList.FirstOrDefault(c => nameMatcher(c, cleanName));

            if (matchingCandidate != null)
            {
                AssignValue(property, matchingCandidate);
                return;
            }

            if (candidateList.Count > 0)
            {
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, candidateList.ToArray()));
            }
        }

        private string GetCleanPropertyName(string propertyName)
        {
            int underscoreIndex = propertyName.IndexOf('_');
            return underscoreIndex >= 0 ? propertyName.Substring(underscoreIndex + 1) : propertyName;
        }

        private void AssignValue(SerializedProperty property, UnityEngine.Object value)
        {
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 指定されたTransformを持つGameObject自身と、その全ての子孫GameObjectをリストに追加する（再帰処理）。
        /// </summary>
        /// <param name="targetTransform">検索を開始するTransform</param>
        /// <param name="list">GameObjectを追加するリスト</param>
        private void GetSelfAndChildGameObjects(Transform targetTransform, List<GameObject> list)
        {
            // まず自分自身を追加
            list.Add(targetTransform.gameObject);

            // 次に、それぞれの子に対して再帰的にこのメソッドを呼び出す
            foreach (Transform child in targetTransform)
            {
                // 再帰呼び出しで子とその子孫を追加
                GetSelfAndChildGameObjects(child, list);
            }
        }

        private bool IsListOrArray(Type type)
        {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }
    }
}