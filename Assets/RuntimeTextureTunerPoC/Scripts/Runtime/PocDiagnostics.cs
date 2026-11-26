using System;
using System.Text;
using UnityEngine;
using UnityEngine.XR;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 比較条件に影響する実行環境の値を文字列にまとめる。
    /// </summary>
    internal static class PocDiagnostics
    {
        private static readonly int GlobalMipBiasId = Shader.PropertyToID("_GlobalMipBias");

        public static void AppendTo(StringBuilder builder)
        {
            Vector4 globalMipBias = Shader.GetGlobalVector(GlobalMipBiasId);
            string qualityName = QualitySettings.names[QualitySettings.GetQualityLevel()];
            string streaming = QualitySettings.streamingMipmapsActive ? "on" : "off";

            AppendLine(builder, $"API: {SystemInfo.graphicsDeviceType}  GPU: {SystemInfo.graphicsDeviceName}");
            AppendLine(builder, $"Quality: {qualityName}  AnisoSetting: {QualitySettings.anisotropicFiltering}");
            AppendLine(builder, $"MipLimit: {QualitySettings.globalTextureMipmapLimit}  MipStreaming: {streaming}");
            AppendLine(builder, $"GlobalMipBias (URP): {globalMipBias.x:F3}");
            AppendLine(builder, $"Eye: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}  Stereo: {XRSettings.stereoRenderingMode}");
            AppendLine(builder, $"ResScale: {XRSettings.eyeTextureResolutionScale:F2}  Viewport: {XRSettings.renderViewportScale:F2}");
        }

        private static void AppendLine(StringBuilder builder, FormattableString line)
        {
            builder.AppendLine(FormattableString.Invariant(line));
        }
    }
}
