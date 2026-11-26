using System;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 見え方の比較条件に影響する実行環境の値を文字列にまとめる。
    /// </summary>
    internal static class TunerDiagnostics
    {
        private static readonly int GlobalMipBiasId = Shader.PropertyToID("_GlobalMipBias");

        /// <summary>
        /// 個別の anisoLevel が品質設定によって上書きされる状態かどうか。
        /// </summary>
        public static bool IsAnisoOverriddenByQuality => QualitySettings.anisotropicFiltering != AnisotropicFiltering.Enable;

        public static void AppendTo(StringBuilder builder)
        {
            Vector4 globalMipBias = Shader.GetGlobalVector(GlobalMipBiasId);
            string qualityName = QualitySettings.names[QualitySettings.GetQualityLevel()];
            string streaming = QualitySettings.streamingMipmapsActive ? "on" : "off";

            AppendLine(builder, $"{SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceName}   Quality: {qualityName}   Aniso setting: {QualitySettings.anisotropicFiltering}");
            AppendLine(builder, $"Mip limit: {QualitySettings.globalTextureMipmapLimit}   Mip streaming: {streaming}   URP global mip bias: {globalMipBias.x:F3}");
            AppendLine(builder, $"Eye: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}   Res scale: {XRSettings.eyeTextureResolutionScale:F2}   Viewport: {XRSettings.renderViewportScale:F2}   {XRSettings.stereoRenderingMode}");
        }

        private static void AppendLine(StringBuilder builder, FormattableString line)
        {
            builder.AppendLine(FormattableString.Invariant(line));
        }
    }
}
