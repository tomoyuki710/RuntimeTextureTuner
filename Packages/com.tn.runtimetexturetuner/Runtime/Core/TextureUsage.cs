using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャがどの Renderer のどのマテリアルスロット、どのプロパティで使われているか。
    /// </summary>
    public readonly struct TextureUsage
    {
        public TextureUsage(Renderer renderer, int materialIndex, Material material, string propertyName, bool hasPropertyBlock)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            Material = material;
            PropertyName = propertyName;
            HasPropertyBlock = hasPropertyBlock;
        }

        public Renderer Renderer { get; }

        public int MaterialIndex { get; }

        public Material Material { get; }

        public string PropertyName { get; }

        /// <summary>
        /// Renderer に MaterialPropertyBlock が設定されているかどうか。
        /// 設定されている場合、実際に描画に使われるテクスチャがマテリアルの値と異なる可能性がある。
        /// </summary>
        public bool HasPropertyBlock { get; }
    }
}
