using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// Renderer が参照しているマテリアルとテクスチャを列挙して保持する。
    /// 毎フレーム作り直すものではなく、初回、シーンの変更時、手動の更新時に作り直す。
    /// </summary>
    public sealed class RendererCatalog
    {
        private static readonly IReadOnlyList<TextureTarget> NoTargets = new List<TextureTarget>();

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<TextureTarget> _targets = new List<TextureTarget>();
        private readonly Dictionary<Texture, TextureTarget> _targetsByTexture = new Dictionary<Texture, TextureTarget>();
        private readonly Dictionary<Renderer, List<TextureTarget>> _targetsByRenderer = new Dictionary<Renderer, List<TextureTarget>>();
        private readonly List<Material> _materialBuffer = new List<Material>();
        private readonly List<string> _propertyNameBuffer = new List<string>();

        /// <summary>
        /// 把握している Renderer。
        /// </summary>
        public IReadOnlyList<Renderer> Renderers => _renderers;

        /// <summary>
        /// 把握しているテクスチャ。共有されているテクスチャは1件にまとまる。
        /// </summary>
        public IReadOnlyList<TextureTarget> Targets => _targets;

        /// <summary>
        /// 読み込み済みのシーンから、表示され得る MeshRenderer と SkinnedMeshRenderer を集める。
        /// 非アクティブなもの、無効なもの、描画を止めているものは含めない。
        /// </summary>
        public static void FindSceneRenderers(List<Renderer> results)
        {
            results.Clear();
            foreach (Renderer candidate in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                bool isSupportedType = candidate is MeshRenderer || candidate is SkinnedMeshRenderer;
                bool isDrawn = candidate.enabled && !candidate.forceRenderingOff;
                if (isSupportedType && isDrawn)
                {
                    results.Add(candidate);
                }
            }
        }

        /// <summary>
        /// 指定した Renderer 群から一覧を作り直す。
        /// </summary>
        public void Rebuild(IEnumerable<Renderer> renderers)
        {
            _renderers.Clear();
            _targets.Clear();
            _targetsByTexture.Clear();
            _targetsByRenderer.Clear();

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || _targetsByRenderer.ContainsKey(targetRenderer))
                {
                    continue;
                }

                _renderers.Add(targetRenderer);
                _targetsByRenderer.Add(targetRenderer, CollectTargets(targetRenderer));
            }
        }

        /// <summary>
        /// Renderer が使っているテクスチャを取得する。把握していない Renderer の場合は空を返す。
        /// </summary>
        public IReadOnlyList<TextureTarget> GetTargets(Renderer targetRenderer)
        {
            if (targetRenderer == null)
            {
                return NoTargets;
            }

            return _targetsByRenderer.TryGetValue(targetRenderer, out List<TextureTarget> targets) ? targets : NoTargets;
        }

        public bool TryGetTarget(Texture texture, out TextureTarget target)
        {
            if (texture == null)
            {
                target = null;
                return false;
            }

            return _targetsByTexture.TryGetValue(texture, out target);
        }

        private List<TextureTarget> CollectTargets(Renderer targetRenderer)
        {
            var targets = new List<TextureTarget>();
            bool hasPropertyBlock = targetRenderer.HasPropertyBlock();

            // sharedMaterials を使い、マテリアルの暗黙の複製を避ける。
            targetRenderer.GetSharedMaterials(_materialBuffer);
            for (int materialIndex = 0; materialIndex < _materialBuffer.Count; materialIndex++)
            {
                Material material = _materialBuffer[materialIndex];
                if (material == null)
                {
                    continue;
                }

                material.GetTexturePropertyNames(_propertyNameBuffer);
                foreach (string propertyName in _propertyNameBuffer)
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture == null)
                    {
                        continue;
                    }

                    TextureTarget target = GetOrCreateTarget(texture);
                    target.AddUsage(new TextureUsage(targetRenderer, materialIndex, material, propertyName, hasPropertyBlock));
                    if (!targets.Contains(target))
                    {
                        targets.Add(target);
                    }
                }
            }

            return targets;
        }

        private TextureTarget GetOrCreateTarget(Texture texture)
        {
            if (_targetsByTexture.TryGetValue(texture, out TextureTarget existing))
            {
                return existing;
            }

            var created = new TextureTarget(texture);
            _targetsByTexture.Add(texture, created);
            _targets.Add(created);
            return created;
        }
    }
}
