using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整の対象になり得るテクスチャ1枚と、その使用箇所。
    /// 同じテクスチャを複数のマテリアルや Renderer が共有していても、対象は1つにまとめる。
    /// </summary>
    public sealed class TextureTarget
    {
        private readonly List<TextureUsage> _usages = new List<TextureUsage>();
        private readonly HashSet<Renderer> _renderers = new HashSet<Renderer>();
        private readonly HashSet<Material> _materials = new HashSet<Material>();

        internal TextureTarget(Texture texture)
        {
            Texture = texture;
        }

        public Texture Texture { get; }

        /// <summary>
        /// サンプラー設定の調整に対応している種類かどうか。初期対象は Texture2D のみ。
        /// </summary>
        public bool IsAdjustable => Texture is Texture2D;

        /// <summary>
        /// Mip を持つかどうか。持たない場合、Mip Bias を変えても見た目は変わらない。
        /// </summary>
        public bool HasMipmaps => Texture.mipmapCount > 1;

        public IReadOnlyList<TextureUsage> Usages => _usages;

        /// <summary>
        /// このテクスチャを使っている、把握済みの Renderer の数。
        /// </summary>
        public int RendererCount => _renderers.Count;

        /// <summary>
        /// このテクスチャを使っている、把握済みのマテリアルの数。
        /// </summary>
        public int MaterialCount => _materials.Count;

        /// <summary>
        /// 使用箇所のいずれかで MaterialPropertyBlock が設定されているかどうか。
        /// </summary>
        public bool HasPropertyBlockUsage { get; private set; }

        internal void AddUsage(TextureUsage usage)
        {
            _usages.Add(usage);
            _renderers.Add(usage.Renderer);
            _materials.Add(usage.Material);
            HasPropertyBlockUsage |= usage.HasPropertyBlock;
        }
    }
}
