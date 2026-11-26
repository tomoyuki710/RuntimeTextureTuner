using System;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 実行時のテクスチャと、その元になったアセットの対応。調整用ビルドの作成時に Editor が作る。
    /// </summary>
    [Serializable]
    public sealed class TextureManifestEntry
    {
        [SerializeField] private Texture2D texture;
        [SerializeField] private TextureKey key;
        [SerializeField] private TextureExportSupport support;
        [SerializeField] private string pathHint;
        [SerializeField] private string sourceFingerprint;
        [SerializeField] private string importerFingerprint;
        [SerializeField] private SamplerSettingsData importerSettings;

        public TextureManifestEntry(
            Texture2D texture,
            TextureKey key,
            TextureExportSupport support,
            string pathHint,
            string sourceFingerprint,
            string importerFingerprint,
            SamplerSettingsData importerSettings)
        {
            this.texture = texture;
            this.key = key;
            this.support = support;
            this.pathHint = pathHint;
            this.sourceFingerprint = sourceFingerprint;
            this.importerFingerprint = importerFingerprint;
            this.importerSettings = importerSettings;
        }

        public Texture2D Texture => texture;

        public TextureKey Key => key;

        public TextureExportSupport Support => support;

        /// <summary>
        /// アセットのパス。表示用であり、アセットの特定には使わない。
        /// </summary>
        public string PathHint => pathHint;

        /// <summary>
        /// ソースファイルの内容のハッシュ。ビルド後にソースが変わったことの検出に使う。
        /// </summary>
        public string SourceFingerprint => sourceFingerprint;

        /// <summary>
        /// インポート設定（.meta）のハッシュ。
        /// </summary>
        public string ImporterFingerprint => importerFingerprint;

        /// <summary>
        /// ビルド時点の TextureImporter の値。未指定を表す値（bias の -100 など）もそのまま持つ。
        /// </summary>
        public SamplerSettingsData ImporterSettings => importerSettings;
    }
}
