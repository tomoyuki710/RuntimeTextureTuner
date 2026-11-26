using System;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャのサンプラー設定（Mip Bias、異方性フィルタリング、フィルターモード）の値。
    /// </summary>
    public readonly struct TextureSamplerSettings : IEquatable<TextureSamplerSettings>
    {
        public const int MinAnisoLevel = 0;
        public const int MaxAnisoLevel = 16;

        public TextureSamplerSettings(float mipMapBias, int anisoLevel, FilterMode filterMode)
        {
            MipMapBias = mipMapBias;
            AnisoLevel = anisoLevel;
            FilterMode = filterMode;
        }

        public float MipMapBias { get; }

        public int AnisoLevel { get; }

        public FilterMode FilterMode { get; }

        /// <summary>
        /// Unity の API に渡してよい値かどうか。
        /// </summary>
        public bool IsValid
        {
            get
            {
                bool isBiasFinite = !float.IsNaN(MipMapBias) && !float.IsInfinity(MipMapBias);
                bool isAnisoInRange = AnisoLevel >= MinAnisoLevel && AnisoLevel <= MaxAnisoLevel;
                return isBiasFinite && isAnisoInRange && Enum.IsDefined(typeof(FilterMode), FilterMode);
            }
        }

        /// <summary>
        /// テクスチャから現在の実効値を読み取る。
        /// </summary>
        public static TextureSamplerSettings Capture(Texture texture)
        {
            return new TextureSamplerSettings(texture.mipMapBias, texture.anisoLevel, texture.filterMode);
        }

        /// <summary>
        /// テクスチャにこの設定を書き込む。現在値と異なる項目だけを書き込む。
        /// </summary>
        public void ApplyTo(Texture texture)
        {
            if (!Mathf.Approximately(texture.mipMapBias, MipMapBias))
            {
                texture.mipMapBias = MipMapBias;
            }

            if (texture.anisoLevel != AnisoLevel)
            {
                texture.anisoLevel = AnisoLevel;
            }

            if (texture.filterMode != FilterMode)
            {
                texture.filterMode = FilterMode;
            }
        }

        public bool Equals(TextureSamplerSettings other)
        {
            bool isSameSampling = AnisoLevel == other.AnisoLevel && FilterMode == other.FilterMode;
            return isSameSampling && MipMapBias.Equals(other.MipMapBias);
        }

        public override bool Equals(object obj)
        {
            return obj is TextureSamplerSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MipMapBias, AnisoLevel, (int)FilterMode);
        }

        public override string ToString()
        {
            FormattableString text = $"bias {MipMapBias:+0.00;-0.00;0.00} aniso {AnisoLevel} {FilterMode}";
            return FormattableString.Invariant(text);
        }
    }
}
