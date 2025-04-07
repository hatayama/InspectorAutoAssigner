using UnityEngine;
using System;

namespace InspectorLinkBinder
{
    /// <summary>
    /// Inspectorの値を他のコンポーネントにバインドするためのコンポーネント
    /// </summary>
    [AddComponentMenu("Inspector Link Binder/Inspector Link Binder")]
    public class InspectorLinkBinder : MonoBehaviour
    {
        [Serializable]
        public class Binding
        {
            public Component sourceComponent;
            public string sourcePropertyName;
            public Component targetComponent;
            public string targetPropertyName;
        }

        [SerializeField]
        private Binding[] bindings;

        private void Update()
        {
            if (bindings == null) return;

            foreach (var binding in bindings)
            {
                if (binding.sourceComponent == null || binding.targetComponent == null) continue;

                var sourceType = binding.sourceComponent.GetType();
                var targetType = binding.targetComponent.GetType();

                var sourceProperty = sourceType.GetProperty(binding.sourcePropertyName);
                var targetProperty = targetType.GetProperty(binding.targetPropertyName);

                if (sourceProperty == null || targetProperty == null) continue;

                var value = sourceProperty.GetValue(binding.sourceComponent);
                targetProperty.SetValue(binding.targetComponent, value);
            }
        }
    }
} 