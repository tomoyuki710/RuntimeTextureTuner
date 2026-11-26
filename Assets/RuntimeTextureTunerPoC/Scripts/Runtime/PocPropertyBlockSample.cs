using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// MaterialPropertyBlock でテクスチャを差し替えたパネルを用意する。
    /// MaterialPropertyBlock はシーンに保存されないため、実行時に設定する。
    /// </summary>
    public static class PocPropertyBlockSample
    {
        /// <summary>
        /// MaterialPropertyBlock でテクスチャを差し替えるパネルの名前。
        /// </summary>
        public const string PanelName = "Panel_GridViaPropertyBlock";

        /// <summary>
        /// 差し替えに使うテクスチャを持つ Renderer の名前。
        /// </summary>
        public const string SourceName = "Wall_Right_Grid";

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        public static void Apply(Transform targetRoot)
        {
            Renderer panel = FindRenderer(targetRoot, PanelName);
            Renderer source = FindRenderer(targetRoot, SourceName);
            if (panel == null || source == null)
            {
                Debug.LogWarning("[RuntimeTextureTuner] MaterialPropertyBlock sample objects were not found.");
                return;
            }

            Texture texture = source.sharedMaterial.GetTexture(BaseMapId);
            var propertyBlock = new MaterialPropertyBlock();
            panel.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(BaseMapId, texture);
            panel.SetPropertyBlock(propertyBlock);
        }

        private static Renderer FindRenderer(Transform targetRoot, string objectName)
        {
            Transform found = targetRoot.Find(objectName);
            return found == null ? null : found.GetComponent<Renderer>();
        }
    }
}
