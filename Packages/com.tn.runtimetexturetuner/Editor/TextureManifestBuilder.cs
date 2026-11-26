using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テクスチャと元アセットの対応（manifest の項目）を作る。
    /// 持ち帰りの対象は、Assets 配下で TextureImporter を持つ独立したテクスチャアセットに限る。
    /// </summary>
    public static class TextureManifestBuilder
    {
        private const string AssetsPrefix = "Assets/";

        /// <summary>
        /// テクスチャに対応する項目を作る。持ち帰れないテクスチャの場合は、その理由を持つ項目を返す。
        /// </summary>
        public static TextureManifestEntry CreateEntry(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureExportSupport support = Classify(texture, path, out TextureImporter importer);

            TextureKey key = default;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out string guid, out long localFileId))
            {
                key = new TextureKey(guid, localFileId);
            }

            if (support != TextureExportSupport.Exportable)
            {
                return new TextureManifestEntry(texture, key, support, path, string.Empty, string.Empty, default);
            }

            return new TextureManifestEntry(
                texture,
                key,
                support,
                path,
                ComputeFileFingerprint(path),
                ComputeFileFingerprint(path + ".meta"),
                ReadImporterSettings(importer));
        }

        /// <summary>
        /// 持ち帰りの対象になるかどうかを判定する。対象になる場合は importer に TextureImporter を返す。
        /// </summary>
        public static TextureExportSupport Classify(Texture texture, string assetPath, out TextureImporter importer)
        {
            importer = null;
            if (texture == null || string.IsNullOrEmpty(assetPath))
            {
                return TextureExportSupport.NotAnAsset;
            }

            if (!IsUnderAssetsFolder(assetPath))
            {
                return TextureExportSupport.OutsideAssetsFolder;
            }

            if (!AssetDatabase.IsMainAsset(texture))
            {
                return TextureExportSupport.SubAsset;
            }

            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            return importer == null ? TextureExportSupport.NoTextureImporter : TextureExportSupport.Exportable;
        }

        /// <summary>
        /// TextureImporter の値をそのまま読む。未指定を表す値も変換しない。
        /// </summary>
        public static SamplerSettingsData ReadImporterSettings(TextureImporter importer)
        {
            return new SamplerSettingsData(importer.mipMapBias, importer.anisoLevel, importer.filterMode.ToString());
        }

        /// <summary>
        /// ファイルの内容の指紋を 16 進文字列で返す。ファイルがない場合は空文字列を返す。
        /// 計算結果は保存され、ファイルのサイズと更新日時が変わらない限り使い回される。
        /// </summary>
        public static string ComputeFileFingerprint(string path)
        {
            return FileFingerprintCache.GetFingerprint(path);
        }

        /// <summary>
        /// シーン内の Renderer（非アクティブなものを含む）が参照している Texture2D を集める。
        /// 後から有効化される Renderer のテクスチャも、同じシーンの既存の参照として含める。
        /// </summary>
        public static void CollectSceneTextures(Scene scene, List<Texture2D> results)
        {
            results.Clear();
            var seen = new HashSet<Texture2D>();
            var materials = new List<Material>();
            var propertyNames = new List<string>();
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Renderer sceneRenderer in rootObject.GetComponentsInChildren<Renderer>(true))
                {
                    sceneRenderer.GetSharedMaterials(materials);
                    foreach (Material material in materials)
                    {
                        AddTexturesOf(material, propertyNames, seen, results);
                    }
                }
            }
        }

        private static void AddTexturesOf(Material material, List<string> propertyNames, HashSet<Texture2D> seen, List<Texture2D> results)
        {
            if (material == null)
            {
                return;
            }

            material.GetTexturePropertyNames(propertyNames);
            foreach (string propertyName in propertyNames)
            {
                if (material.GetTexture(propertyName) is Texture2D texture && seen.Add(texture))
                {
                    results.Add(texture);
                }
            }
        }

        private static bool IsUnderAssetsFolder(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(AssetsPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            // パスの見かけだけでなく、実際の場所がプロジェクトの Assets フォルダー内にあることも確かめる。
            string fullPath = Path.GetFullPath(normalized).Replace('\\', '/');
            string assetsRoot = Path.GetFullPath(Application.dataPath).Replace('\\', '/') + "/";
            return fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
