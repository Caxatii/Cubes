using Cubes.Presentation.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Cubes.Presentation.Gameplay
{
    internal sealed class CubeViewFactory
    {
        private readonly GameplayScreen _screen;
        private readonly CubeBarAppearanceAsset _appearance;

        public CubeViewFactory(GameplayScreen screen, CubeBarAppearanceAsset appearance)
        {
            _screen = screen;
            _appearance = appearance;
        }

        public Vector2 PrefabSize => _screen.CubePrefab.Size;

        public CubeView Create(int typeIndex, RectTransform parent, bool fromBar = false)
        {
            CubeView view = Object.Instantiate(_screen.CubePrefab, parent);
            view.SetAppearance(_appearance.GetSprite(typeIndex));
            Bind(view, fromBar, typeIndex);

            return view;
        }

        public void Bind(CubeView cube, bool fromBar, int typeIndex)
            => cube.Bind(_screen.Drag, fromBar, typeIndex);

        public void MatchTowerScale(CubeView cube, Vector2 size)
        {
            Vector3 fieldScale = _screen.BuildArea.RectTransform.lossyScale;
            Vector3 parentScale = cube.RectTransform.parent.lossyScale;

            cube.RectTransform.localScale = new Vector3(size.x / cube.Size.x * fieldScale.x / parentScale.x,
                size.y / cube.Size.y * fieldScale.y / parentScale.y, 1f);
        }

        public static void Reparent(CubeView cube, RectTransform parent)
        {
            cube.RectTransform.SetParent(parent, true);
        }

        public void BuildCubeBar()
        {
            ScrollRect bar = _screen.CubeBar;

            for (int index = bar.content.childCount - 1; index >= 0; index--)
            {
                GameObject child = bar.content.GetChild(index).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }

            for (int typeIndex = 0; typeIndex < _appearance.TypeCount; typeIndex++)
            {
                Create(typeIndex, bar.content, true);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(bar.content);
        }
    }
}
