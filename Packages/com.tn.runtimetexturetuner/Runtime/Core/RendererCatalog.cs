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
        private readonly MaterialPropertyBlock _rendererBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _materialBlock = new MaterialPropertyBlock();

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

            // MaterialPropertyBlock でテクスチャが差し替えられている場合は、マテリアルの値ではなく、実際に描画される差し替え後のテクスチャを集める。
            // MaterialPropertyBlock の内容は一覧を作った時点のもの。
            targetRenderer.GetPropertyBlock(_rendererBlock);
            int blockTargetCount = 0;

            // sharedMaterials を使い、マテリアルの暗黙の複製を避ける。
            targetRenderer.GetSharedMaterials(_materialBuffer);
            for (int materialIndex = 0; materialIndex < _materialBuffer.Count; materialIndex++)
            {
                Material material = _materialBuffer[materialIndex];
                if (material == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_materialBlock, materialIndex);
                material.GetTexturePropertyNames(_propertyNameBuffer);
                foreach (string propertyName in _propertyNameBuffer)
                {
                    Texture texture = ResolveTexture(material, propertyName, out bool isFromPropertyBlock);
                    if (texture == null)
                    {
                        continue;
                    }

                    TextureTarget target = GetOrCreateTarget(texture);
                    target.AddUsage(new TextureUsage(targetRenderer, materialIndex, material, propertyName, isFromPropertyBlock));
                    AddTarget(targets, target, isFromPropertyBlock, ref blockTargetCount);
                }
            }

            return targets;
        }

        /// <summary>
        /// プロパティで実際に描画されるテクスチャを返す。マテリアル単位の MaterialPropertyBlock、
        /// Renderer 全体の MaterialPropertyBlock、マテリアルの順に探す。
        /// </summary>
        /// <summary>
        /// Renderer のテクスチャ一覧に追加する。MaterialPropertyBlock で設定されたテクスチャは、実際に描画されていることが確かなので先頭側に並べる。
        /// マテリアルのテクスチャには、シェーダーが使っていない互換用のプロパティ（_MainTex など）の値も含まれる。
        /// </summary>
        private static void AddTarget(List<TextureTarget> targets, TextureTarget target, bool isFromPropertyBlock, ref int blockTargetCount)
        {
            int existingIndex = targets.IndexOf(target);
            if (!isFromPropertyBlock)
            {
                if (existingIndex < 0)
                {
                    targets.Add(target);
                }

                return;
            }

            if (existingIndex >= 0 && existingIndex < blockTargetCount)
            {
                return;
            }

            if (existingIndex >= 0)
            {
                targets.RemoveAt(existingIndex);
            }

            targets.Insert(blockTargetCount, target);
            blockTargetCount++;
        }

        private Texture ResolveTexture(Material material, string propertyName, out bool isFromPropertyBlock)
        {
            isFromPropertyBlock = true;
            if (!_materialBlock.isEmpty && _materialBlock.HasTexture(propertyName))
            {
                return _materialBlock.GetTexture(propertyName);
            }

            if (!_rendererBlock.isEmpty && _rendererBlock.HasTexture(propertyName))
            {
                return _rendererBlock.GetTexture(propertyName);
            }

            isFromPropertyBlock = false;
            return material.GetTexture(propertyName);
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
