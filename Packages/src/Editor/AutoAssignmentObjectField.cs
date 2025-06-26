using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace io.github.hatayama.InspectorAutoAssigner
{
    /// <summary>
    /// Custom property drawer that adds an automatic assignment button to the right of property fields of type GameObject or Component.
    /// Depending on the Unity editor's Inspector display mode, the following methods are called:
    /// - If UI Toolkit is enabled: CreatePropertyGUI() is called (OnGUI() is ignored).
    /// - If the conventional IMGUI is enabled: OnGUI() is called (CreatePropertyGUI() is ignored).
    /// Two processes are prepared for compatibility with other inspector extension OSS.
    /// </summary>
    [CustomPropertyDrawer(typeof(GameObject), true)]
    [CustomPropertyDrawer(typeof(Component), true)]
    public class AutoAssignmentObjectField : PropertyDrawer
    {
        // Variable to cache the button style for IMGUI.
        private GUIStyle _searchButtonStyle;

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
            searchButton.style.alignItems = Align.Center;

            Image iconImage = new Image
            {
                image = EditorGUIUtility.FindTexture("Search Icon"),
                scaleMode = ScaleMode.ScaleToFit
            };
            // Removed icon size adjustment to match the original code.
            iconImage.style.width = buttonSize * 0.8f;
            iconImage.style.height = buttonSize * 0.8f;

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

            // Draw the label part.
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float buttonSize = EditorGUIUtility.singleLineHeight;
            float buttonWidth = buttonSize; // Button width is the same as height
            float objectFieldWidth = position.width - buttonWidth; // Calculate the object field width

            // Calculate the drawing range for each control.
            Rect objectFieldRect = new Rect(position.x, position.y, objectFieldWidth, position.height);
            Rect buttonRect = new Rect(position.x + objectFieldWidth, position.y, buttonWidth, position.height);

            // Draw the object field.
            EditorGUI.PropertyField(objectFieldRect, property, GUIContent.none);

            if (_searchButtonStyle == null)
            {
                int padding = 2;
                _searchButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(padding, padding, padding, padding), // Reduce padding to make the icon appear larger.
                    alignment = TextAnchor.MiddleCenter, // Center the icon.
                    imagePosition = ImagePosition.ImageOnly // Display only the icon.
                };
            }

            // Get the icon.
            Texture2D searchIcon = EditorGUIUtility.FindTexture("Search Icon");
            GUIContent searchButtonContent = new GUIContent(searchIcon); // Create GUIContent with only the icon.

            // Draw the button with a custom style.
            if (GUI.Button(buttonRect, searchButtonContent, _searchButtonStyle))
            {
                // Call the popup display process when the button is pressed.
                HandleButtonPress(property, fieldType, buttonRect);
            }

            // ---- End of icon button drawing process ----

            EditorGUI.indentLevel = indent; // Restore the indent level.

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

            Component[] foundComponents = targetGameObject.GetComponentsInChildren(fieldType, true);
            List<Component> allComponents = new List<Component>(foundComponents);

            if (allComponents.Count == 0)
            {
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{fieldType.Name} component not found."));
                return;
            }

            if (allComponents.Count == 1)
            {
                AssignValue(property, allComponents[0]);
                return;
            }

            ProcessAssignmentCandidates<Component>(
                property,
                buttonRect,
                allComponents,
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
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentMessagePopup($"{typeName} not found."));
                return;
            }

            string cleanName = GetCleanPropertyName(property.name);

            T matchingCandidate = candidateList.FirstOrDefault(c => nameMatcher(c, cleanName));

            if (matchingCandidate != null)
            {
                AssignValue(property, matchingCandidate);
                return;
            }

            if (candidateList.Count > 1)
            {
                UnityEditor.PopupWindow.Show(buttonRect, new AutoAssignmentObjectSelectorPopup(property, candidateList.ToArray()));
            }
        }

        private string GetCleanPropertyName(string propertyName)
        {
            // Handle Unity naming convention: single character + underscore (e.g., m_, s_, k_, etc.)
            if (propertyName.Length >= 3 && propertyName[1] == '_')
            {
                return propertyName.Substring(2);
            }
            
            // Handle leading underscore (e.g., _fieldName)
            if (propertyName.StartsWith("_"))
            {
                return propertyName.Substring(1);
            }
            
            return propertyName;
        }

        private void AssignValue(SerializedProperty property, UnityEngine.Object value)
        {
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Adds the GameObject itself and all its descendant GameObjects to the list (recursive process).
        /// </summary>
        /// <param name="targetTransform">The Transform to start searching from.</param>
        /// <param name="list">The list to add GameObjects to.</param>
        private void GetSelfAndChildGameObjects(Transform targetTransform, List<GameObject> list)
        {
            list.Add(targetTransform.gameObject);
            foreach (Transform child in targetTransform)
            {
                GetSelfAndChildGameObjects(child, list);
            }
        }

        private bool IsListOrArray(Type type)
        {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }
    }
}