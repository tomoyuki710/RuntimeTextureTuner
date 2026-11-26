using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 読み込まれているシーンの対応表から、実行時のテクスチャに対応するアセットの情報を引く。
    /// </summary>
    public static class TextureRegistry
    {
        private const string EditorBuildId = "editor-play-mode";

        private static readonly List<TextureRegistryScene> _scenes = new List<TextureRegistryScene>();
        private static readonly Dictionary<Texture, TextureManifestEntry> _entries = new Dictionary<Texture, TextureManifestEntry>();
        private static bool _isDirty;

        /// <summary>
        /// Editor の Play Mode で、対応表なしにアセットの情報を引くための処理。Editor 側のコードが設定する。
        /// プレイヤービルドでは使われない。
        /// </summary>
        public static Func<Texture, TextureManifestEntry> EditorResolver { get; set; }

        /// <summary>
        /// 読み込まれている対応表のビルド ID。対応表がない場合、Editor では固定の値、それ以外は空文字列。
        /// </summary>
        public static string BuildId
        {
            get
            {
                if (_scenes.Count > 0)
                {
                    return _scenes[0].BuildId;
                }

                return EditorResolver != null ? EditorBuildId : string.Empty;
            }
        }

        /// <summary>
        /// テクスチャに対応するアセットの情報を引く。見つからない場合は entry が null になり、
        /// 対応表にないことを表す値を返す。
        /// </summary>
        public static TextureExportSupport Resolve(Texture texture, out TextureManifestEntry entry)
        {
            entry = null;
            if (texture == null)
            {
                return TextureExportSupport.NotInRegistry;
            }

            RebuildIfDirty();
            bool isFound = _entries.TryGetValue(texture, out entry);
            if (!isFound && EditorResolver != null)
            {
                entry = EditorResolver(texture);
            }

            return entry == null ? TextureExportSupport.NotInRegistry : entry.Support;
        }

        internal static void Register(TextureRegistryScene scene)
        {
            if (!_scenes.Contains(scene))
            {
                _scenes.Add(scene);
                _isDirty = true;
            }
        }

        internal static void Unregister(TextureRegistryScene scene)
        {
            if (_scenes.Remove(scene))
            {
                _isDirty = true;
            }
        }

        private static void RebuildIfDirty()
        {
            if (!_isDirty)
            {
                return;
            }

            _isDirty = false;
            _entries.Clear();
            foreach (TextureRegistryScene scene in _scenes)
            {
                foreach (TextureManifestEntry entry in scene.Entries)
                {
                    bool hasTexture = entry != null && entry.Texture != null;
                    if (hasTexture)
                    {
                        _entries[entry.Texture] = entry;
                    }
                }
            }
        }
    }
}
