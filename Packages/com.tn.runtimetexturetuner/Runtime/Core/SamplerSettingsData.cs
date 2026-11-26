using System;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// サンプラー設定の保存用の形。JSON とシーンへの保存の両方に使う。
    /// TextureImporter の値も入れるため、未指定を表す値（bias の -100 など）をそのまま保持できるようにしている。
    /// </summary>
    [Serializable]
    public struct SamplerSettingsData : IEquatable<SamplerSettingsData>
    {
        [SerializeField] private float bias;
        [SerializeField] private int aniso;
        [SerializeField] private string filter;

        public SamplerSettingsData(float bias, int aniso, string filter)
        {
            this.bias = bias;
            this.aniso = aniso;
            this.filter = filter;
        }

        public float Bias => bias;

        public int Aniso => aniso;

        public string Filter => filter;

        public static SamplerSettingsData From(TextureSamplerSettings settings)
        {
            return new SamplerSettingsData(settings.MipMapBias, settings.AnisoLevel, settings.FilterMode.ToString());
        }

        /// <summary>
        /// 実行時のテクスチャに設定できる値へ変換する。値が不正な場合は false を返す。
        /// </summary>
        public bool TryToSettings(out TextureSamplerSettings settings)
        {
            settings = default;
            if (!TryParseFilter(filter, out FilterMode filterMode))
            {
                return false;
            }

            settings = new TextureSamplerSettings(bias, aniso, filterMode);
            return settings.IsValid;
        }

        /// <summary>
        /// フィルターモードの名前を解釈する。数値だけの文字列は受け付けない。
        /// </summary>
        public static bool TryParseFilter(string text, out FilterMode filterMode)
        {
            filterMode = default;
            bool hasName = !string.IsNullOrEmpty(text) && !char.IsDigit(text[0]) && text[0] != '-';
            if (!hasName)
            {
                return false;
            }

            return Enum.TryParse(text, false, out filterMode) && Enum.IsDefined(typeof(FilterMode), filterMode);
        }

        public bool Equals(SamplerSettingsData other)
        {
            bool isSameNumbers = bias.Equals(other.bias) && aniso == other.aniso;
            return isSameNumbers && string.Equals(filter, other.filter, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SamplerSettingsData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(bias, aniso, filter);
        }

        public override string ToString()
        {
            FormattableString text = $"bias {bias:+0.00;-0.00;0.00} aniso {aniso} {filter}";
            return FormattableString.Invariant(text);
        }
    }
}
