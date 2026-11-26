using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャがどの Renderer のどのマテリアルスロット、どのプロパティで使われているか。
    /// </summary>
    public readonly struct TextureUsage
    {
        public TextureUsage(Renderer renderer, int materialIndex, Material material, string propertyName, bool isFromPropertyBlock)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            Material = material;
            PropertyName = propertyName;
            IsFromPropertyBlock = isFromPropertyBlock;
        }

        public Renderer Renderer { get; }

        public int MaterialIndex { get; }

        public Material Material { get; }

        public string PropertyName { get; }

        /// <summary>
        /// このテクスチャが、マテリアルではなく Renderer の MaterialPropertyBlock で設定されているかどうか。
        /// </summary>
        public bool IsFromPropertyBlock { get; }
    }
}
