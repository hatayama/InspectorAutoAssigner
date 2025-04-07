using UnityEngine;

namespace InspectorLinkBinder.Samples
{
    public class InspectorLinkBinderDemo : MonoBehaviour
    {
        [SerializeField]
        private float value;

        [SerializeField]
        private Transform targetTransform;

        private void Update()
        {
            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * value;
            }
        }
    }
} 