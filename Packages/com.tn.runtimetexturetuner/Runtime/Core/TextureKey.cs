using System;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャアセットを特定するキー。アセットの GUID と、ファイル内の ID の組。
    /// ファイル名や Texture.name、インスタンス ID では同名や複製を区別できないため、これを正規のキーにする。
    /// </summary>
    [Serializable]
    public struct TextureKey : IEquatable<TextureKey>
    {
        private const int GuidLength = 32;

        [SerializeField] private string guid;
        [SerializeField] private long localFileId;

        public TextureKey(string guid, long localFileId)
        {
            this.guid = guid;
            this.localFileId = localFileId;
        }

        public string Guid => guid;

        public long LocalFileId => localFileId;

        /// <summary>
        /// GUID が 32 桁の 16 進数で、ファイル内 ID が設定されているかどうか。
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(guid) || guid.Length != GuidLength || localFileId == 0)
                {
                    return false;
                }

                foreach (char character in guid)
                {
                    if (!Uri.IsHexDigit(character))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool Equals(TextureKey other)
        {
            return localFileId == other.localFileId && string.Equals(guid, other.guid, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is TextureKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            int guidHash = guid == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(guid);
            return HashCode.Combine(guidHash, localFileId);
        }

        public override string ToString()
        {
            return $"{guid}:{localFileId}";
        }
    }
}
