using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整機能の表示物に使うマテリアルを作る。
    /// 導入先のレンダーパイプラインやシェーダーの同梱状況に依存しないよう、複数の候補から探す。
    /// </summary>
    internal static class TunerMaterials
    {
        // 上から順に探す。UI/Default と Sprites/Default は通常、常に同梱されるシェーダーに含まれる。
        private static readonly string[] ShaderCandidates =
        {
            "UI/Default",
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
        };

        private static Shader _shader;

        public static Material CreateUnlit(Color color)
        {
            var material = new Material(FindShader()) { name = "RuntimeTextureTunerUnlit" };
            material.color = color;
            return material;
        }

        /// <summary>
        /// 影やプローブなど、表示物に不要な処理を止める。
        /// </summary>
        public static void DisableLighting(Renderer target)
        {
            target.shadowCastingMode = ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.lightProbeUsage = LightProbeUsage.Off;
            target.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Shader FindShader()
        {
            if (_shader != null)
            {
                return _shader;
            }

            foreach (string shaderName in ShaderCandidates)
            {
                _shader = Shader.Find(shaderName);
                if (_shader != null)
                {
                    return _shader;
                }
            }

            Debug.LogError("[RuntimeTextureTuner] No shader was found for the tuner panel.");
            return null;
        }
    }
}
