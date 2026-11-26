using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 操作用のレイと、選択中の Renderer の Bounds を線で表示する。
    /// </summary>
    internal sealed class TunerPointerVisual
    {
        private const float RayWidth = 0.004f;
        private const float BoundsWidth = 0.006f;
        private const int BoundsPointCount = 16;

        // パネルの板や文字（描画順 0〜12）より後に描き、レイがボタンの下に隠れないようにする。
        private const int SortingOrder = 100;

        private static readonly Color RayColor = new Color(0.3f, 0.8f, 1f, 0.9f);
        private static readonly Color BoundsColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        private readonly LineRenderer _rayLine;
        private readonly LineRenderer _boundsLine;
        private readonly Material _rayMaterial;
        private readonly Material _boundsMaterial;
        private readonly Vector3[] _boundsPoints = new Vector3[BoundsPointCount];

        public TunerPointerVisual(Transform parent)
        {
            _rayMaterial = TunerMaterials.CreateUnlit(RayColor);
            _boundsMaterial = TunerMaterials.CreateUnlit(BoundsColor);
            _rayLine = CreateLine(parent, "PointerRay", _rayMaterial, RayWidth, 2);
            _boundsLine = CreateLine(parent, "SelectionBounds", _boundsMaterial, BoundsWidth, BoundsPointCount);
        }

        public void ShowRay(Vector3 start, Vector3 end)
        {
            _rayLine.SetPosition(0, start);
            _rayLine.SetPosition(1, end);
            _rayLine.enabled = true;
        }

        public void HideRay()
        {
            _rayLine.enabled = false;
        }

        /// <summary>
        /// 選択中の Renderer の Bounds を箱の線で示す。選択の根拠が Bounds であることを見せる意味もある。
        /// </summary>
        public void ShowBounds(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            var bottom0 = new Vector3(min.x, min.y, min.z);
            var bottom1 = new Vector3(max.x, min.y, min.z);
            var bottom2 = new Vector3(max.x, min.y, max.z);
            var bottom3 = new Vector3(min.x, min.y, max.z);
            var top0 = new Vector3(min.x, max.y, min.z);
            var top1 = new Vector3(max.x, max.y, min.z);
            var top2 = new Vector3(max.x, max.y, max.z);
            var top3 = new Vector3(min.x, max.y, max.z);

            // 12本の辺を1本の折れ線でたどる。
            _boundsPoints[0] = bottom0;
            _boundsPoints[1] = bottom1;
            _boundsPoints[2] = bottom2;
            _boundsPoints[3] = bottom3;
            _boundsPoints[4] = bottom0;
            _boundsPoints[5] = top0;
            _boundsPoints[6] = top1;
            _boundsPoints[7] = bottom1;
            _boundsPoints[8] = top1;
            _boundsPoints[9] = top2;
            _boundsPoints[10] = bottom2;
            _boundsPoints[11] = top2;
            _boundsPoints[12] = top3;
            _boundsPoints[13] = bottom3;
            _boundsPoints[14] = top3;
            _boundsPoints[15] = top0;
            _boundsLine.SetPositions(_boundsPoints);
            _boundsLine.enabled = true;
        }

        public void HideBounds()
        {
            _boundsLine.enabled = false;
        }

        public void Destroy()
        {
            Object.Destroy(_rayMaterial);
            Object.Destroy(_boundsMaterial);
        }

        private static LineRenderer CreateLine(Transform parent, string name, Material material, float width, int pointCount)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = pointCount;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = material;
            line.sortingOrder = SortingOrder;
            line.enabled = false;
            TunerMaterials.DisableLighting(line);
            return line;
        }
    }
}
