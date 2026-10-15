using UnityEngine;
using UnityEngine.UI;

namespace Cubes.Presentation.Gameplay
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HoleMaskGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect bounds = rectTransform.rect;
            RectTransform canvasRect = (RectTransform)canvas.rootCanvas.transform;
            Vector2 minimum = rectTransform.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.min));
            Vector2 maximum = rectTransform.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.max));

            helper.AddVert(new Vector2(minimum.x, bounds.center.y), color, Vector2.zero);
            helper.AddVert(new Vector2(minimum.x, maximum.y), color, Vector2.zero);
            helper.AddVert(new Vector2(maximum.x, maximum.y), color, Vector2.zero);
            helper.AddVert(new Vector2(maximum.x, bounds.center.y), color, Vector2.zero);
            helper.AddTriangle(0, 1, 2);
            helper.AddTriangle(0, 2, 3);

            helper.AddVert(bounds.center, color, new Vector2(0.5f, 0.5f));
            for (int index = 0; index <= 64; index++)
            {
                float angle = Mathf.PI + index * Mathf.PI / 64f;
                Vector2 normalized = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                helper.AddVert(bounds.center + Vector2.Scale(normalized, bounds.size * 0.5f), color, normalized * 0.5f + Vector2.one * 0.5f);
                if (index > 0)
                {
                    helper.AddTriangle(4, index + 4, index + 5);
                }
            }
        }
    }
}

