using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整用ビルドのときだけ、ビルド用のシーンコピーにテクスチャの対応表を追加する。
    /// 元のシーンやプレハブは保存しない。調整用でないビルドと Play Mode では何も追加しない。
    /// </summary>
    internal sealed class TuningBuildProcessor : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        private const string LogPrefix = "[RuntimeTextureTuner]";

        private static string _buildId;

        // 他の処理がシーンを組み替えた後の、最終的な Renderer とマテリアルを対象にする。
        public int callbackOrder => 10000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Play Mode では対応表を差し込まず、Editor 側で直接アセットの情報を引く。
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            if (!TuningBuild.IsTuningBuild())
            {
                if (TuningBuild.IsVerbose)
                {
                    Debug.Log($"{LogPrefix} Not a tuning build. No registry was added: {scene.path}");
                }

                return;
            }

            AddRegistry(scene, GetBuildId(report));
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // ビルドごとに1つの ID を決め、そのビルドの全シーンの対応表で共有する。
            _buildId = CreateBuildId(report);
        }

        private static string GetBuildId(BuildReport report)
        {
            if (string.IsNullOrEmpty(_buildId))
            {
                _buildId = CreateBuildId(report);
            }

            return _buildId;
        }

        private static string CreateBuildId(BuildReport report)
        {
            string buildId = report == null ? string.Empty : report.summary.guid.ToString();
            bool isEmpty = string.IsNullOrEmpty(buildId) || buildId.Trim('0').Length == 0;
            return isEmpty ? Guid.NewGuid().ToString("N") : buildId;
        }

        private static void AddRegistry(Scene scene, string buildId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var textures = new List<Texture2D>();
            TextureManifestBuilder.CollectSceneTextures(scene, textures);

            var entries = new List<TextureManifestEntry>(textures.Count);
            int exportableCount = 0;
            foreach (Texture2D texture in textures)
            {
                TextureManifestEntry entry = TextureManifestBuilder.CreateEntry(texture);
                entries.Add(entry);
                if (entry.Support == TextureExportSupport.Exportable)
                {
                    exportableCount++;
                }
            }

            entries.Sort(CompareByPath);

            var registryObject = new GameObject(TextureRegistryScene.ObjectName);
            SceneManager.MoveGameObjectToScene(registryObject, scene);
            registryObject.AddComponent<TextureRegistryScene>().Initialize(buildId, entries);
            FileFingerprintCache.Save();
            Debug.Log($"{LogPrefix} Registry added to the build copy of {scene.path}: {entries.Count} texture(s), {exportableCount} exportable, {stopwatch.Elapsed.TotalSeconds:0.0} s. Build ID: {buildId}");
        }

        private static int CompareByPath(TextureManifestEntry left, TextureManifestEntry right)
        {
            int byPath = string.CompareOrdinal(left.PathHint, right.PathHint);
            return byPath != 0 ? byPath : left.Key.LocalFileId.CompareTo(right.Key.LocalFileId);
        }
    }
}
