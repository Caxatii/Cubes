using UnityEngine;

namespace Cubes.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DropZoneView : MonoBehaviour
    {
        [SerializeField] private Color _outlineColor = Color.cyan;
        [SerializeField] private Color _hitColor = Color.green;
        [SerializeField] private bool _showDebugInPlayMode;

        private bool _isPointerInside;

        public RectTransform RectTransform => (RectTransform)transform;

        public bool Contains(Vector2 screenPosition, Camera eventCamera)
        {
            Rect bounds = RectTransform.rect;

            if (bounds.width <= 0f || bounds.height <= 0f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPosition, eventCamera, out Vector2 localPosition))
            {
                return false;
            }

            Vector2 relativePosition = localPosition - bounds.center;
            float x = relativePosition.x / (bounds.width * 0.5f);
            float y = relativePosition.y / (bounds.height * 0.5f);

            return x * x + y * y <= 1f;
        }

        public void SetPointerPosition(Vector2 screenPosition, Camera eventCamera)
        {
            _isPointerInside = Contains(screenPosition, eventCamera);
        }

        public void ClearPointer()
        {
            _isPointerInside = false;
        }

        private void OnDrawGizmos()
        {
            if (UnityEngine.Application.isPlaying && !_showDebugInPlayMode)
            {
                return;
            }

            Rect bounds = RectTransform.rect;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _isPointerInside ? _hitColor : _outlineColor;
            Vector3 previousPoint = new Vector3(bounds.xMax, bounds.center.y, 0f);

            for (int index = 1; index <= 64; index++)
            {
                float angle = index * Mathf.PI * 2f / 64f;
                Vector3 point = new Vector3(bounds.center.x + Mathf.Cos(angle) * bounds.width * 0.5f, bounds.center.y + Mathf.Sin(angle) * bounds.height * 0.5f, 0f);
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
