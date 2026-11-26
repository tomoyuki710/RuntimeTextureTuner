using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// USB 接続した Android 端末から、adb でセッションの JSON を取得する。
    /// 端末側のファイルは変更せず、プロジェクトの Library 配下へコピーするだけ。
    /// </summary>
    internal static class AdbSessionPuller
    {
        private const int TimeoutMilliseconds = 30000;
        private const string PulledFolder = ApplyTransactionStore.WorkFolder + "/Pulled";

        /// <summary>
        /// 端末のセッションフォルダーをコピーする。成功した場合は localFolder にコピー先を返す。
        /// </summary>
        public static bool TryPull(out string localFolder, out string message)
        {
            string applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string remoteFolder = $"/sdcard/Android/data/{applicationId}/files/{TunerSession.ExportFolderName}/.";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            localFolder = $"{PulledFolder}/{timestamp}";
            Directory.CreateDirectory(localFolder);

            bool isPulled = TryRunAdb($"pull \"{remoteFolder}\" \"{Path.GetFullPath(localFolder)}\"", out string output);
            bool hasFiles = Directory.GetFiles(localFolder, "*.json", SearchOption.AllDirectories).Length > 0;
            if (!isPulled || !hasFiles)
            {
                message = $"No session was pulled from {applicationId}. {output}".Trim();
                return false;
            }

            message = output;
            return true;
        }

        private static bool TryRunAdb(string arguments, out string output)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = FindAdb(),
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(TimeoutMilliseconds))
                    {
                        process.Kill();
                        output = "adb timed out.";
                        return false;
                    }

                    output = (standardOutput + standardError).Trim();
                    return process.ExitCode == 0;
                }
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                output = $"adb could not be started: {exception.Message}";
                return false;
            }
        }

        private static string FindAdb()
        {
            // Unity の Android Build Support に同梱されている adb を優先し、なければ PATH 上のものを使う。
            string executable = UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";
            string bundled = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", executable);
            return File.Exists(bundled) ? bundled : executable;
        }
    }
}
