using UnityEngine;

namespace Cubes.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BuildAreaView : MonoBehaviour
    {
        [SerializeField] private Color _outlineColor = new Color(1f, 0.6f, 0f, 1f);

        public RectTransform RectTransform => (RectTransform)transform;

        public bool TryGetLogicalPosition(Cubes.Application.Gameplay.TowerState state, Vector2 prefabSize,
            Vector2 screenPosition, Camera eventCamera, out Vector2 logicalPosition)
        {
            logicalPosition = Vector2.zero;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, screenPosition, eventCamera, out Vector2 local))
            {
                return false;
            }

            Rect field = RectTransform.rect;

            if (local.x < field.xMin || local.x > field.xMax || local.y < field.yMin || local.y > field.yMax)
            {
                return false;
            }

            logicalPosition = new TowerLayout(state, field, prefabSize).ToLogical(local);

            return true;
        }

        private void OnDrawGizmos()
        {
            Rect bounds = RectTransform.rect;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _outlineColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}
