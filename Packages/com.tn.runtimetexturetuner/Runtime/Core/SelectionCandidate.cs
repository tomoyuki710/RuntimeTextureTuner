using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// レイで指した方向にある Renderer の候補。
    /// </summary>
    public readonly struct SelectionCandidate
    {
        public SelectionCandidate(Renderer renderer, float distance, bool isOriginInside)
        {
            Renderer = renderer;
            Distance = distance;
            IsOriginInside = isOriginInside;
        }

        public Renderer Renderer { get; }

        /// <summary>
        /// レイの始点から Bounds の表面までの距離。始点が Bounds の内側にある場合は、レイが外へ出る点までの距離。
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// レイの始点が Bounds の内側にあるかどうか。部屋全体が1つのメッシュの場合などに起きる。
        /// </summary>
        public bool IsOriginInside { get; }
    }
}
