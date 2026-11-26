using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 実機から持ち帰ったセッションを現在のプロジェクトと照合し、項目ごとに反映できるかどうかを判定する。
    /// アセットの特定には GUID とファイル内 ID だけを使い、パスや名前は使わない。
    /// </summary>
    public static class SessionImportService
    {
        /// <summary>
        /// JSON ファイルを読み込む。失敗した場合は false を返し、error に理由を入れる。
        /// </summary>
        public static bool TryLoad(string path, out TuningSessionData session, out string error)
        {
            session = null;
            try
            {
                return SessionSerializer.TryFromJson(File.ReadAllText(path), out session, out error);
            }
            catch (IOException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static List<SessionRow> BuildRows(TuningSessionData session)
        {
            var rows = new List<SessionRow>(session.Entries.Count);
            foreach (TuningSessionEntry entry in session.Entries)
            {
                rows.Add(Evaluate(entry));
            }

            return rows;
        }

        /// <summary>
        /// 1項目を現在のプロジェクトと照合する。反映の直前にも呼び、状態を確かめ直す。
        /// </summary>
        public static SessionRow Evaluate(TuningSessionEntry entry)
        {
            if (entry == null)
            {
                return new SessionRow(null, SessionRowStatus.Invalid, "The entry is empty.", string.Empty, default);
            }

            bool hasKey = entry.TryGetKey(out TextureKey key);
            bool hasAdjusted = entry.Adjusted.TryToSettings(out _);
            if (!hasKey || !hasAdjusted)
            {
                return new SessionRow(entry, SessionRowStatus.Invalid, "The key or the adjusted values are not valid.", entry.PathHint, default);
            }

            if (!TryFindAsset(key, out string assetPath, out Texture2D texture, out string missingReason))
            {
                return new SessionRow(entry, SessionRowStatus.Missing, missingReason, entry.PathHint, default);
            }

            TextureExportSupport support = TextureManifestBuilder.Classify(texture, assetPath, out TextureImporter importer);
            if (support != TextureExportSupport.Exportable)
            {
                return new SessionRow(entry, SessionRowStatus.Unsupported, support.ToString(), assetPath, default);
            }

            SamplerSettingsData current = TextureManifestBuilder.ReadImporterSettings(importer);
            return EvaluateValues(entry, assetPath, current);
        }

        /// <summary>
        /// 指定した項目について、2つの設定値が同じかどうか。
        /// </summary>
        public static bool IsSameField(string fieldName, SamplerSettingsData left, SamplerSettingsData right)
        {
            switch (fieldName)
            {
                case TuningSessionEntry.BiasField:
                    return Mathf.Approximately(left.Bias, right.Bias);
                case TuningSessionEntry.AnisoField:
                    return left.Aniso == right.Aniso;
                case TuningSessionEntry.FilterField:
                    return left.Filter == right.Filter;
                default:
                    return true;
            }
        }

        private static SessionRow EvaluateValues(TuningSessionEntry entry, string assetPath, SamplerSettingsData current)
        {
            if (entry.ChangedFields.Count == 0)
            {
                return new SessionRow(entry, SessionRowStatus.Unchanged, "No field was changed on the device.", assetPath, current);
            }

            if (IsSameForChangedFields(entry, current, entry.Adjusted))
            {
                return new SessionRow(entry, SessionRowStatus.Unchanged, "Already has the adjusted values.", assetPath, current);
            }

            if (entry.SourceFingerprint != TextureManifestBuilder.ComputeFileFingerprint(assetPath))
            {
                return new SessionRow(entry, SessionRowStatus.Stale, "The source file changed after the tuning build.", assetPath, current);
            }

            if (!IsSameForChangedFields(entry, current, entry.OriginalImporter))
            {
                return new SessionRow(entry, SessionRowStatus.Conflict, "The import settings were changed after the tuning build.", assetPath, current);
            }

            bool isMetaChanged = entry.ImporterFingerprint != TextureManifestBuilder.ComputeFileFingerprint(assetPath + ".meta");
            string note = isMetaChanged ? "Other import settings changed after the tuning build." : string.Empty;
            return new SessionRow(entry, SessionRowStatus.Ready, note, assetPath, current);
        }

        private static bool IsSameForChangedFields(TuningSessionEntry entry, SamplerSettingsData left, SamplerSettingsData right)
        {
            foreach (string fieldName in entry.ChangedFields)
            {
                if (!IsSameField(fieldName, left, right))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindAsset(TextureKey key, out string assetPath, out Texture2D texture, out string reason)
        {
            texture = null;
            reason = null;
            assetPath = AssetDatabase.GUIDToAssetPath(key.Guid);
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            {
                reason = "No asset has this GUID. It may have been deleted.";
                return false;
            }

            // GUID が同じでも、ファイル内 ID が違えば別のオブジェクトを指している。
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                bool isTexture = asset is Texture2D;
                if (!isTexture || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long localFileId))
                {
                    continue;
                }

                if (localFileId == key.LocalFileId)
                {
                    texture = (Texture2D)asset;
                    return true;
                }
            }

            reason = "The asset no longer contains this texture.";
            return false;
        }
    }
}
