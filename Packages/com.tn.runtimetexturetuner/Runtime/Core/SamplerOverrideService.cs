using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャのサンプラー設定を一時的に上書きし、元の値との切り替えと復元を行う。
    /// 変更するのは既存のテクスチャインスタンスのサンプラー設定だけで、画素データやマテリアルは変更しない。
    /// Unity のメインスレッドから呼ぶこと。
    /// </summary>
    public sealed class SamplerOverrideService
    {
        private sealed class Entry
        {
            public Entry(TextureSamplerSettings original)
            {
                Original = original;
                Adjusted = original;
            }

            public TextureSamplerSettings Original { get; }

            public TextureSamplerSettings Adjusted { get; set; }
        }

        private readonly Dictionary<Texture, Entry> _entries = new Dictionary<Texture, Entry>();
        private readonly List<Texture> _textures = new List<Texture>();

        /// <summary>
        /// 現在表示している側。既定は調整値。
        /// </summary>
        public CompareMode CompareMode { get; private set; } = CompareMode.Adjusted;

        /// <summary>
        /// 管理しているテクスチャの数。
        /// </summary>
        public int Count => _textures.Count;

        /// <summary>
        /// テクスチャを管理対象に加え、その時点の実効値を元の値として記録する。
        /// 既に登録済み、または null の場合は false を返す。
        /// </summary>
        public bool TryRegister(Texture texture)
        {
            if (texture == null || _entries.ContainsKey(texture))
            {
                return false;
            }

            _entries.Add(texture, new Entry(TextureSamplerSettings.Capture(texture)));
            _textures.Add(texture);
            return true;
        }

        public bool TryGetOriginal(Texture texture, out TextureSamplerSettings original)
        {
            if (!TryGetEntry(texture, out Entry entry))
            {
                original = default;
                return false;
            }

            original = entry.Original;
            return true;
        }

        public bool TryGetAdjusted(Texture texture, out TextureSamplerSettings adjusted)
        {
            if (!TryGetEntry(texture, out Entry entry))
            {
                adjusted = default;
                return false;
            }

            adjusted = entry.Adjusted;
            return true;
        }

        /// <summary>
        /// 調整値を設定する。未登録のテクスチャは先に登録する。
        /// 表示が調整値側のときだけテクスチャへ書き込む。値が不正な場合は false を返す。
        /// </summary>
        public bool TrySetAdjusted(Texture texture, TextureSamplerSettings adjusted)
        {
            if (texture == null || !adjusted.IsValid)
            {
                return false;
            }

            TryRegister(texture);
            Entry entry = _entries[texture];
            entry.Adjusted = adjusted;
            if (CompareMode == CompareMode.Adjusted)
            {
                adjusted.ApplyTo(texture);
            }

            return true;
        }

        /// <summary>
        /// 表示する側を切り替え、管理している全テクスチャへ反映する。
        /// </summary>
        public void SetCompareMode(CompareMode compareMode)
        {
            CompareMode = compareMode;
            RemoveDestroyedTextures();
            foreach (Texture texture in _textures)
            {
                Entry entry = _entries[texture];
                TextureSamplerSettings settings = compareMode == CompareMode.Adjusted ? entry.Adjusted : entry.Original;
                settings.ApplyTo(texture);
            }
        }

        /// <summary>
        /// 指定したテクスチャの調整値を捨て、元の値に戻す。
        /// </summary>
        public bool TryReset(Texture texture)
        {
            if (!TryGetEntry(texture, out Entry entry))
            {
                return false;
            }

            entry.Adjusted = entry.Original;
            entry.Original.ApplyTo(texture);
            return true;
        }

        /// <summary>
        /// 管理している全テクスチャの調整値を捨て、元の値に戻す。
        /// </summary>
        public void ResetAll()
        {
            RemoveDestroyedTextures();
            foreach (Texture texture in _textures)
            {
                Entry entry = _entries[texture];
                entry.Adjusted = entry.Original;
                entry.Original.ApplyTo(texture);
            }
        }

        /// <summary>
        /// 管理しているテクスチャを登録順に取得する。
        /// </summary>
        public void GetTextures(List<Texture> results)
        {
            RemoveDestroyedTextures();
            results.Clear();
            results.AddRange(_textures);
        }

        private bool TryGetEntry(Texture texture, out Entry entry)
        {
            if (texture == null)
            {
                entry = null;
                return false;
            }

            return _entries.TryGetValue(texture, out entry);
        }

        private void RemoveDestroyedTextures()
        {
            for (int i = _textures.Count - 1; i >= 0; i--)
            {
                Texture texture = _textures[i];
                if (texture != null)
                {
                    continue;
                }

                // 破棄済みの UnityEngine.Object も辞書のキーとしては参照が一致する。
                _entries.Remove(texture);
                _textures.RemoveAt(i);
            }
        }
    }
}
