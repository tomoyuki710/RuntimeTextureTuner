using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// ファイルの内容の指紋（ハッシュ）を計算し、結果をプロジェクトの Library 配下に保存して使い回す。
    /// 大きなテクスチャが多いプロジェクトで、ビルドのたびに全ファイルを読み直さないためのもの。
    /// ファイルのサイズと更新日時が前回と同じなら、保存してある指紋を返す。
    /// </summary>
    public static class FileFingerprintCache
    {
        private const string CachePath = ApplyTransactionStore.WorkFolder + "/FingerprintCache.json";
        private const int BufferSize = 1024 * 1024;

        [Serializable]
        private sealed class CacheFile
        {
            public List<CacheItem> items = new List<CacheItem>();
        }

        [Serializable]
        private sealed class CacheItem
        {
            public string path;
            public long length;
            public long lastWriteTicks;
            public string fingerprint;
        }

        private static Dictionary<string, CacheItem> _items;
        private static bool _isDirty;

        /// <summary>
        /// ファイルの内容の指紋を 16 進文字列で返す。ファイルがない場合は空文字列を返す。
        /// </summary>
        public static string GetFingerprint(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                return string.Empty;
            }

            Dictionary<string, CacheItem> items = GetItems();
            string key = path.Replace('\\', '/');
            long lastWriteTicks = file.LastWriteTimeUtc.Ticks;
            if (items.TryGetValue(key, out CacheItem cached))
            {
                bool isSameFile = cached.length == file.Length && cached.lastWriteTicks == lastWriteTicks;
                if (isSameFile)
                {
                    return cached.fingerprint;
                }
            }

            var item = new CacheItem
            {
                path = key,
                length = file.Length,
                lastWriteTicks = lastWriteTicks,
                fingerprint = Compute(path),
            };
            items[key] = item;
            _isDirty = true;
            return item.fingerprint;
        }

        /// <summary>
        /// 新しく計算した指紋があれば、保存する。保存に失敗しても、次回に計算し直すだけなので無視する。
        /// </summary>
        public static void Save()
        {
            if (!_isDirty || _items == null)
            {
                return;
            }

            var file = new CacheFile();
            file.items.AddRange(_items.Values);
            try
            {
                Directory.CreateDirectory(ApplyTransactionStore.WorkFolder);
                File.WriteAllText(CachePath, JsonUtility.ToJson(file));
                _isDirty = false;
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[RuntimeTextureTuner] The fingerprint cache was not saved: {exception.Message}");
            }
        }

        private static Dictionary<string, CacheItem> GetItems()
        {
            if (_items != null)
            {
                return _items;
            }

            _items = new Dictionary<string, CacheItem>(StringComparer.Ordinal);
            if (!File.Exists(CachePath))
            {
                return _items;
            }

            try
            {
                CacheFile file = JsonUtility.FromJson<CacheFile>(File.ReadAllText(CachePath));
                foreach (CacheItem item in file.items)
                {
                    _items[item.path] = item;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                // 壊れた保存データは捨てて、計算し直す。
                _items.Clear();
            }

            return _items;
        }

        private static string Compute(string path)
        {
            // 改ざんの検出ではなく変更の検出が目的なので、暗号学的ハッシュより速い Hash128 を使う。
            var hash = new Hash128();
            var buffer = new byte[BufferSize];
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                int readCount;
                while ((readCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hash.Append(buffer, 0, readCount);
                }
            }

            return hash.ToString();
        }
    }
}
