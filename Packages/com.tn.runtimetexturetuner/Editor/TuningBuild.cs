using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整用ビルドを作る。プロジェクトのビルド設定（シーン一覧、スクリプト定義シンボル）は書き換えず、
    /// このビルドに限ってシンボルを追加する。
    /// </summary>
    public static class TuningBuild
    {
        /// <summary>
        /// 調整機能を有効にするスクリプト定義シンボル。
        /// </summary>
        public const string Define = "RUNTIME_TEXTURE_TUNER_ENABLED";

        private const string MenuPath = "Tools/Runtime Texture Tuner/Build Tuning Player...";
        private const string VerboseKey = "RuntimeTextureTuner.VerboseBuildLog";

        private static bool _isBuildingTuningPlayer;

        /// <summary>
        /// ビルド時の処理が呼ばれたかどうかを、調整用でないビルドでもログに出す。キャッシュの挙動の検証用。
        /// </summary>
        public static bool IsVerbose
        {
            get => SessionState.GetBool(VerboseKey, false);
            set => SessionState.SetBool(VerboseKey, value);
        }

        /// <summary>
        /// 調整用のシンボルを追加してプレイヤーをビルドする。options の他の内容は変更しない。
        /// </summary>
        public static BuildReport BuildPlayer(BuildPlayerOptions options)
        {
            var defines = new List<string>(options.extraScriptingDefines ?? Array.Empty<string>());
            if (!defines.Contains(Define))
            {
                defines.Add(Define);
            }

            options.extraScriptingDefines = defines.ToArray();
            _isBuildingTuningPlayer = true;
            try
            {
                return BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                _isBuildingTuningPlayer = false;
            }
        }

        /// <summary>
        /// いま行っているビルドが調整用かどうか。このクラスからのビルドのほか、
        /// Player Settings やアクティブな Build Profile にシンボルが設定されている場合も調整用とみなす。
        /// </summary>
        public static bool IsTuningBuild()
        {
            if (_isBuildingTuningPlayer)
            {
                return true;
            }

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), out string[] playerDefines);
            if (Array.IndexOf(playerDefines, Define) >= 0)
            {
                return true;
            }

            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            bool hasProfileDefines = activeProfile != null && activeProfile.scriptingDefines != null;
            return hasProfileDefines && Array.IndexOf(activeProfile.scriptingDefines, Define) >= 0;
        }

        [MenuItem(MenuPath)]
        private static void MenuBuildTuningPlayer()
        {
            BuildWithCurrentSettings();
        }

        private static void BuildWithCurrentSettings()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string extension = target == BuildTarget.Android ? "apk" : string.Empty;
            string location = EditorUtility.SaveFilePanel("Build Tuning Player", "Builds", $"{PlayerSettings.productName}_Tuning", extension);
            if (string.IsNullOrEmpty(location))
            {
                return;
            }

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = location,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None,
            };

            BuildReport report = BuildPlayer(options);
            BuildSummary summary = report.summary;
            string message = $"[RuntimeTextureTuner] Tuning build {summary.result}: {Path.GetFileName(location)} (errors {summary.totalErrors})";
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }
    }
}
