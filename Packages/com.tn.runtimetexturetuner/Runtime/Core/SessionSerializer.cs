using System;
using System.IO;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整セッションと JSON の相互変換、およびファイルへの保存。
    /// </summary>
    public static class SessionSerializer
    {
        private const string TemporarySuffix = ".tmp";

        public static string ToJson(TuningSessionData session)
        {
            return JsonUtility.ToJson(session, true);
        }

        /// <summary>
        /// JSON を読み込む。壊れている場合や対応していない版の場合は false を返し、error に理由を入れる。
        /// 個々の項目の値の検証は行わない（項目ごとに判定して、問題のあるものだけを除外できるようにするため）。
        /// </summary>
        public static bool TryFromJson(string json, out TuningSessionData session, out string error)
        {
            session = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "The file is empty.";
                return false;
            }

            try
            {
                session = JsonUtility.FromJson<TuningSessionData>(json);
            }
            catch (ArgumentException exception)
            {
                error = $"The file is not valid JSON: {exception.Message}";
                return false;
            }

            if (session == null)
            {
                error = "The file is not valid JSON.";
                return false;
            }

            if (session.SchemaVersion != TuningSessionData.CurrentSchemaVersion)
            {
                error = $"Unsupported schema version: {session.SchemaVersion}.";
                session = null;
                return false;
            }

            session.EnsureEntries();
            return true;
        }

        /// <summary>
        /// 一時ファイルに書いてから名前を変えて保存する。途中で失敗しても、書きかけのファイルを残さない。
        /// 既に同じ名前のファイルがある場合は上書きせず、例外を投げる。
        /// </summary>
        public static void WriteFile(string path, string json)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(path))
            {
                throw new IOException($"The file already exists: {path}");
            }

            string temporaryPath = path + TemporarySuffix;
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, path);
        }
    }
}
