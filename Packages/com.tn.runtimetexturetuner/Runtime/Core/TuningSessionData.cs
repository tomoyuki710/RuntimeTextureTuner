using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整セッションの JSON 全体。実機で保存し、Editor で読み込む。
    /// </summary>
    [Serializable]
    public sealed class TuningSessionData
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string buildId;
        [SerializeField] private string unityVersion;
        [SerializeField] private string device;
        [SerializeField] private string graphicsApi;
        [SerializeField] private string qualityName;
        [SerializeField] private string exportedAtUtc;
        [SerializeField] private List<TuningSessionEntry> entries = new List<TuningSessionEntry>();

        // JSON からの読み込み用。
        private TuningSessionData()
        {
        }

        public TuningSessionData(string buildId, string unityVersion, string device, string graphicsApi, string qualityName, string exportedAtUtc)
        {
            this.buildId = buildId;
            this.unityVersion = unityVersion;
            this.device = device;
            this.graphicsApi = graphicsApi;
            this.qualityName = qualityName;
            this.exportedAtUtc = exportedAtUtc;
        }

        public int SchemaVersion => schemaVersion;

        /// <summary>
        /// どのビルドで調整したかを表す ID。認証のための秘密ではない。
        /// </summary>
        public string BuildId => buildId;

        public string UnityVersion => unityVersion;

        public string Device => device;

        public string GraphicsApi => graphicsApi;

        public string QualityName => qualityName;

        public string ExportedAtUtc => exportedAtUtc;

        public IReadOnlyList<TuningSessionEntry> Entries => entries;

        public void AddEntry(TuningSessionEntry entry)
        {
            entries.Add(entry);
        }

        /// <summary>
        /// JSON から読み込んだ直後、項目のリストがない場合に空のリストへ置き換える。
        /// </summary>
        internal void EnsureEntries()
        {
            if (entries == null)
            {
                entries = new List<TuningSessionEntry>();
            }
        }
    }
}
