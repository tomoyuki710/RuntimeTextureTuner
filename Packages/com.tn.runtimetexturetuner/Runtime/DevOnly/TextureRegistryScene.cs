using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// シーンが使っているテクスチャと、元のアセットの対応表。
    /// 調整用ビルドの作成時に、ビルド用のシーンコピーへ Editor が追加する。元のシーンには保存されない。
    /// シーンが既に参照しているテクスチャだけを持つため、ビルドに余分なアセットを増やさない。
    /// </summary>
    public sealed class TextureRegistryScene : MonoBehaviour
    {
        /// <summary>
        /// 対応表を載せる GameObject の名前。
        /// </summary>
        public const string ObjectName = "RuntimeTextureTunerRegistry";

        [SerializeField, Tooltip("どのビルドで作られた対応表かを表す ID")]
        private string buildId;

        [SerializeField, Tooltip("シーンが使っているテクスチャと、元のアセットの対応")]
        private List<TextureManifestEntry> entries = new List<TextureManifestEntry>();

        public string BuildId => buildId;

        public IReadOnlyList<TextureManifestEntry> Entries => entries;

        /// <summary>
        /// 対応表の内容を設定する。Editor がビルド時に呼ぶ。
        /// </summary>
        public void Initialize(string newBuildId, IEnumerable<TextureManifestEntry> newEntries)
        {
            buildId = newBuildId;
            entries = new List<TextureManifestEntry>(newEntries);
        }

        private void OnEnable()
        {
            TextureRegistry.Register(this);
        }

        private void OnDisable()
        {
            // シーンの破棄に合わせて登録を外し、テクスチャへの参照を残さない。
            TextureRegistry.Unregister(this);
        }
    }
}
