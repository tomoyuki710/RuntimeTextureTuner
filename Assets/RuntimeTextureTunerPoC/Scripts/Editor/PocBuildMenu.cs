using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// PoC の素材生成と Android 向けビルドのメニュー。
    /// プロジェクトのビルド設定（シーン一覧、スクリプト定義シンボル）は変更しない。
    /// </summary>
    public static class PocBuildMenu
    {
        /// <summary>
        /// 調整機能を有効にするスクリプト定義シンボル。
        /// </summary>
        public const string TunerDefine = "RUNTIME_TEXTURE_TUNER_ENABLED";

        public const string TuningApkPath = "Builds/PoC/RuntimeTextureTunerPoC.apk";
        public const string PlainApkPath = "Builds/PoCWithoutTuner/RuntimeTextureTunerPoCWithoutTuner.apk";

        private const string MenuRoot = "Tools/Runtime Texture Tuner/PoC/";

        [MenuItem(MenuRoot + "Generate Content")]
        private static void MenuGenerateContent()
        {
            GenerateContent();
        }

        [MenuItem(MenuRoot + "Build Tuning APK")]
        private static void MenuBuildTuningApk()
        {
            BuildTuningApk();
        }

        [MenuItem(MenuRoot + "Build APK Without Tuner (for verification)")]
        private static void MenuBuildPlainApk()
        {
            BuildPlainApk();
        }

        /// <summary>
        /// テストパターン、マテリアル、シーンを生成する。
        /// </summary>
        public static void GenerateContent()
        {
            PocTextureGenerator.GenerateAll();
            PocSceneBuilder.Build();
            Debug.Log($"[RuntimeTextureTuner] PoC content generated: {PocSceneBuilder.ScenePath}");
        }

        /// <summary>
        /// 調整機能つきの Development Build を作る。
        /// </summary>
        public static bool BuildTuningApk()
        {
            return Build(TuningApkPath, new[] { TunerDefine });
        }

        /// <summary>
        /// 調整機能のシンボルを付けない Development Build を作る。調整用コードが含まれないことの確認用。
        /// </summary>
        public static bool BuildPlainApk()
        {
            return Build(PlainApkPath, new string[0]);
        }

        private static bool Build(string apkPath, string[] extraDefines)
        {
            if (!File.Exists(PocSceneBuilder.ScenePath))
            {
                Debug.LogError("[RuntimeTextureTuner] PoC scene is missing. Run 'Generate Content' first.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(apkPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { PocSceneBuilder.ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
                extraScriptingDefines = extraDefines,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool isSucceeded = summary.result == BuildResult.Succeeded;
            string message = $"[RuntimeTextureTuner] Build {summary.result}: {apkPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s, errors {summary.totalErrors})";
            if (isSucceeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            return isSucceeded;
        }
    }
}
