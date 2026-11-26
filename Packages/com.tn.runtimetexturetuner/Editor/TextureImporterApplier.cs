using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整結果を TextureImporter に反映し、後から元に戻す。
    /// 変更するのは mipMapBias、anisoLevel、filterMode のうち、実機で変えた項目だけ。
    /// ソース画像、最大サイズ、圧縮形式、Mip の生成設定などには触れない。
    /// </summary>
    public static class TextureImporterApplier
    {
        /// <summary>
        /// 進捗の通知。引数は対象のパスと進み具合（0〜1）。true を返すと、次の項目の前で中断する。
        /// </summary>
        public delegate bool ProgressCallback(string assetPath, float progress);

        /// <summary>
        /// 選択されている行を反映する。各行は書き込みの直前に照合し直す。
        /// 1件以上反映した場合は、変更前の値を含む記録を返す。messages には飛ばした項目の理由が入る。
        /// </summary>
        public static ApplyTransaction Apply(
            IReadOnlyList<SessionRow> rows,
            string sessionBuildId,
            bool allowStale,
            bool allowConflict,
            ProgressCallback onProgress,
            List<string> messages)
        {
            var transaction = new ApplyTransaction(sessionBuildId);
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    SessionRow row = rows[i];
                    if (!row.IsSelected)
                    {
                        continue;
                    }

                    if (onProgress != null && onProgress(row.AssetPath, i / (float)rows.Count))
                    {
                        messages.Add("Canceled. The remaining items were not applied.");
                        break;
                    }

                    ApplyRow(row, allowStale, allowConflict, transaction, messages);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return transaction.Items.Count > 0 ? transaction : null;
        }

        /// <summary>
        /// 記録をもとに、Apply 前の値へ戻す。Apply の後に別の値へ変えられた項目は戻さず、messages で知らせる。
        /// 戻した件数を返す。
        /// </summary>
        public static int Restore(ApplyTransaction transaction, ProgressCallback onProgress, List<string> messages)
        {
            int restoredCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < transaction.Items.Count; i++)
                {
                    ApplyTransactionItem item = transaction.Items[i];
                    if (onProgress != null && onProgress(item.PathHint, i / (float)transaction.Items.Count))
                    {
                        messages.Add("Canceled. The remaining items were not restored.");
                        break;
                    }

                    if (RestoreItem(item, messages))
                    {
                        restoredCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return restoredCount;
        }

        private static void ApplyRow(SessionRow row, bool allowStale, bool allowConflict, ApplyTransaction transaction, List<string> messages)
        {
            // 一覧を作った後にアセットが変わっている可能性があるため、書き込みの直前に判定し直す。
            SessionRow current = SessionImportService.Evaluate(row.Entry);
            if (!current.CanApply(allowStale, allowConflict))
            {
                messages.Add($"Skipped ({current.Status}): {current.AssetPath}");
                return;
            }

            var importer = AssetImporter.GetAtPath(current.AssetPath) as TextureImporter;
            if (importer == null)
            {
                messages.Add($"Skipped (no TextureImporter): {current.AssetPath}");
                return;
            }

            TuningSessionEntry entry = row.Entry;
            SamplerSettingsData before = TextureManifestBuilder.ReadImporterSettings(importer);
            WriteFields(importer, entry.ChangedFields, entry.Adjusted);
            importer.SaveAndReimport();

            SamplerSettingsData after = TextureManifestBuilder.ReadImporterSettings(importer);
            transaction.AddItem(new ApplyTransactionItem(entry.Guid, current.AssetPath, before, after, entry.ChangedFields));
        }

        private static bool RestoreItem(ApplyTransactionItem item, List<string> messages)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(item.Guid);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                messages.Add($"Skipped (asset not found): {item.PathHint}");
                return false;
            }

            SamplerSettingsData current = TextureManifestBuilder.ReadImporterSettings(importer);
            foreach (string fieldName in item.AppliedFields)
            {
                if (!SessionImportService.IsSameField(fieldName, current, item.After))
                {
                    messages.Add($"Skipped (changed after Apply, field '{fieldName}'): {assetPath}");
                    return false;
                }
            }

            WriteFields(importer, item.AppliedFields, item.Before);
            importer.SaveAndReimport();
            return true;
        }

        private static void WriteFields(TextureImporter importer, IReadOnlyList<string> fieldNames, SamplerSettingsData values)
        {
            foreach (string fieldName in fieldNames)
            {
                switch (fieldName)
                {
                    case TuningSessionEntry.BiasField:
                        importer.mipMapBias = values.Bias;
                        break;
                    case TuningSessionEntry.AnisoField:
                        importer.anisoLevel = values.Aniso;
                        break;
                    case TuningSessionEntry.FilterField:
                        importer.filterMode = ParseFilter(values.Filter);
                        break;
                }
            }
        }

        private static FilterMode ParseFilter(string text)
        {
            // TextureImporter の値は、未指定を表す数値（-1）の場合がある。名前と数値の両方を受け付ける。
            return (FilterMode)Enum.Parse(typeof(FilterMode), text);
        }
    }
}
