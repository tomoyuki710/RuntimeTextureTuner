using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// Editor の Play Mode で、テクスチャから元アセットの情報を直接引けるようにする。
    /// これにより、ビルドせずに Editor 上で調整から持ち帰りまでを試せる。シーンには何も追加しない。
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorRegistryResolver
    {
        private static readonly Dictionary<Texture, TextureManifestEntry> _cache = new Dictionary<Texture, TextureManifestEntry>();

        static EditorRegistryResolver()
        {
            TextureRegistry.EditorResolver = Resolve;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            ClearCache();
        }

        private static void ClearCache()
        {
            // Play Mode の出入りの間にソースやインポート設定が変わり得るため、指紋を作り直す。
            _cache.Clear();
        }

        private static TextureManifestEntry Resolve(Texture texture)
        {
            if (!(texture is Texture2D texture2D))
            {
                return null;
            }

            if (_cache.TryGetValue(texture, out TextureManifestEntry cached))
            {
                return cached;
            }

            TextureManifestEntry entry = TextureManifestBuilder.CreateEntry(texture2D);
            _cache[texture] = entry;
            return entry;
        }
    }
}
