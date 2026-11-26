using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 1回の Apply で変更した内容の記録。Restore で元に戻すために使う。
    /// </summary>
    [Serializable]
    public sealed class ApplyTransaction
    {
        [SerializeField] private string id;
        [SerializeField] private string createdAtUtc;
        [SerializeField] private string sessionBuildId;
        [SerializeField] private List<ApplyTransactionItem> items = new List<ApplyTransactionItem>();

        // JSON からの読み込み用。
        private ApplyTransaction()
        {
        }

        public ApplyTransaction(string sessionBuildId)
        {
            DateTime now = DateTime.UtcNow;
            id = now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
            createdAtUtc = now.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            this.sessionBuildId = sessionBuildId;
        }

        public string Id => id;

        public string CreatedAtUtc => createdAtUtc;

        public string SessionBuildId => sessionBuildId;

        public IReadOnlyList<ApplyTransactionItem> Items => items ?? (items = new List<ApplyTransactionItem>());

        public void AddItem(ApplyTransactionItem item)
        {
            items.Add(item);
        }
    }

    /// <summary>
    /// 1つのテクスチャに対して変更した項目と、変更前後の TextureImporter の値。
    /// </summary>
    [Serializable]
    public sealed class ApplyTransactionItem
    {
        [SerializeField] private string guid;
        [SerializeField] private string pathHint;
        [SerializeField] private SamplerSettingsData before;
        [SerializeField] private SamplerSettingsData after;
        [SerializeField] private List<string> appliedFields = new List<string>();

        // JSON からの読み込み用。
        private ApplyTransactionItem()
        {
        }

        public ApplyTransactionItem(string guid, string pathHint, SamplerSettingsData before, SamplerSettingsData after, IEnumerable<string> appliedFields)
        {
            this.guid = guid;
            this.pathHint = pathHint;
            this.before = before;
            this.after = after;
            this.appliedFields = new List<string>(appliedFields);
        }

        public string Guid => guid;

        public string PathHint => pathHint;

        /// <summary>
        /// 変更前の値。未指定を表す値（bias の -100 など）もそのまま持つ。
        /// </summary>
        public SamplerSettingsData Before => before;

        public SamplerSettingsData After => after;

        public IReadOnlyList<string> AppliedFields => appliedFields ?? (appliedFields = new List<string>());
    }
}
