using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace io.github.hatayama
{
    [CustomPropertyDrawer(typeof(GameObject), true)]
    [CustomPropertyDrawer(typeof(Component), true)]
    public class AutoAssignmentObjectField : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray || (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)))
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
            if (fieldType == typeof(GameObject) || fieldType.IsSubclassOf(typeof(GameObject)))
            {
                HandleGameObjectAssignment(property, buttonWorldBound);
                return;
            }
            
            if (fieldType.IsSubclassOf(typeof(Component)))
            {
                HandleComponentAssignment(property, fieldType, buttonWorldBound);
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
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup("GameObject が見つかりませんでした."));
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

            UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, gameObjects.ToArray()));
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
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{fieldType.Name} コンポーネントが見つかりませんでした."));
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
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, components));
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

        private void GetChildGameObjects(Transform parent, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child.gameObject);
                GetChildGameObjects(child, list);
            }
        }
    }
}