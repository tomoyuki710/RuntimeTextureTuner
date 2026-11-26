using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// レイと Renderer.bounds の交差で、指している対象の候補を求める。Collider や Layer を使わない。
    /// Bounds は軸に平行な箱による近似なので、遮蔽、メッシュの穴、重なった別の物を区別できない。
    /// そのため1つに決めず、距離順の候補を返して利用者に確定させる。
    /// </summary>
    public static class SelectionService
    {
        /// <summary>
        /// レイが Bounds を通る Renderer を、近い順に results へ入れる。
        /// </summary>
        public static void FindCandidates(Ray ray, IReadOnlyList<Renderer> renderers, float maxDistance, List<SelectionCandidate> results)
        {
            results.Clear();
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!TryIntersect(ray, candidate.bounds, out float nearDistance, out float farDistance))
                {
                    continue;
                }

                bool isOriginInside = nearDistance < 0f;
                float distance = isOriginInside ? farDistance : nearDistance;
                if (distance <= maxDistance)
                {
                    results.Add(new SelectionCandidate(candidate, distance, isOriginInside));
                }
            }

            results.Sort(CompareByDistance);
        }

        /// <summary>
        /// レイと箱の交差区間を求める（スラブ法）。nearDistance が負のとき、始点は箱の内側にある。
        /// </summary>
        public static bool TryIntersect(Ray ray, Bounds bounds, out float nearDistance, out float farDistance)
        {
            nearDistance = float.NegativeInfinity;
            farDistance = float.PositiveInfinity;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int axis = 0; axis < 3; axis++)
            {
                float origin = ray.origin[axis];
                float direction = ray.direction[axis];
                if (Mathf.Abs(direction) < 1e-8f)
                {
                    // この軸に平行なレイは、始点が範囲外なら交差しない。
                    if (origin < min[axis] || origin > max[axis])
                    {
                        return false;
                    }

                    continue;
                }

                float first = (min[axis] - origin) / direction;
                float second = (max[axis] - origin) / direction;
                nearDistance = Mathf.Max(nearDistance, Mathf.Min(first, second));
                farDistance = Mathf.Min(farDistance, Mathf.Max(first, second));
            }

            return farDistance >= Mathf.Max(nearDistance, 0f);
        }

        private static int CompareByDistance(SelectionCandidate left, SelectionCandidate right)
        {
            return left.Distance.CompareTo(right.Distance);
        }
    }
}
