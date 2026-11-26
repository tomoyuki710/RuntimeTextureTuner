using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整セッションの JSON の1項目。テクスチャ1枚ぶんの調整結果。
    /// </summary>
    [Serializable]
    public sealed class TuningSessionEntry
    {
        public const string BiasField = "bias";
        public const string AnisoField = "aniso";
        public const string FilterField = "filter";

        [SerializeField] private string guid;

        // long は JSON を扱う他のツールで精度が落ちることがあるため、文字列で保存する。
        [SerializeField] private string localFileId;
        [SerializeField] private string sourceFingerprint;
        [SerializeField] private string importerFingerprint;
        [SerializeField] private string pathHint;
        [SerializeField] private SamplerSettingsData originalRuntime;
        [SerializeField] private SamplerSettingsData originalImporter;
        [SerializeField] private SamplerSettingsData adjusted;
        [SerializeField] private List<string> changedFields = new List<string>();
        [SerializeField] private bool kept;

        // JSON からの読み込み用。
        private TuningSessionEntry()
        {
        }

        public TuningSessionEntry(
            TextureManifestEntry manifestEntry,
            TextureSamplerSettings originalRuntime,
            TextureSamplerSettings adjusted,
            bool kept)
        {
            guid = manifestEntry.Key.Guid;
            localFileId = manifestEntry.Key.LocalFileId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sourceFingerprint = manifestEntry.SourceFingerprint;
            importerFingerprint = manifestEntry.ImporterFingerprint;
            pathHint = manifestEntry.PathHint;
            originalImporter = manifestEntry.ImporterSettings;
            this.originalRuntime = SamplerSettingsData.From(originalRuntime);
            this.adjusted = SamplerSettingsData.From(adjusted);
            this.kept = kept;
            AddChangedFields(originalRuntime, adjusted);
        }

        public string Guid => guid;

        public string LocalFileIdText => localFileId;

        public string SourceFingerprint => sourceFingerprint;

        public string ImporterFingerprint => importerFingerprint;

        public string PathHint => pathHint;

        /// <summary>
        /// 調整前の、実機での実効値。
        /// </summary>
        public SamplerSettingsData OriginalRuntime => originalRuntime;

        /// <summary>
        /// ビルド時点の TextureImporter の値。
        /// </summary>
        public SamplerSettingsData OriginalImporter => originalImporter;

        public SamplerSettingsData Adjusted => adjusted;

        /// <summary>
        /// 調整で変わった項目。"bias"、"aniso"、"filter" のいずれか。
        /// </summary>
        public IReadOnlyList<string> ChangedFields => changedFields;

        public bool Kept => kept;

        /// <summary>
        /// アセットを特定するキーを取り出す。値が不正な場合は false を返す。
        /// </summary>
        public bool TryGetKey(out TextureKey key)
        {
            bool isParsed = long.TryParse(
                localFileId,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out long parsedId);
            key = new TextureKey(guid, parsedId);
            return isParsed && key.IsValid;
        }

        public bool HasChanged(string fieldName)
        {
            return changedFields != null && changedFields.Contains(fieldName);
        }

        private void AddChangedFields(TextureSamplerSettings original, TextureSamplerSettings adjustedSettings)
        {
            if (!Mathf.Approximately(original.MipMapBias, adjustedSettings.MipMapBias))
            {
                changedFields.Add(BiasField);
            }

            if (original.AnisoLevel != adjustedSettings.AnisoLevel)
            {
                changedFields.Add(AnisoField);
            }

            if (original.FilterMode != adjustedSettings.FilterMode)
            {
                changedFields.Add(FilterField);
            }
        }
    }
}
