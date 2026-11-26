using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 実機から持ち帰った調整結果を確認し、選んだ項目だけを TextureImporter に反映するウィンドウ。
    /// 自動では何も反映しない。
    /// </summary>
    public sealed class TunerSessionsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Runtime Texture Tuner/Sessions";
        private const float SelectWidth = 22f;
        private const float StatusWidth = 80f;
        private const float ValueWidth = 190f;
        private const float FieldsWidth = 110f;

        private readonly List<string> _messages = new List<string>();
        private TuningSessionData _session;
        private List<SessionRow> _rows = new List<SessionRow>();
        private List<ApplyTransaction> _transactions = new List<ApplyTransaction>();
        private string _sessionPath = string.Empty;
        private bool _allowStale;
        private bool _allowConflict;
        private Vector2 _rowScroll;
        private Vector2 _messageScroll;

        [MenuItem(MenuPath)]
        private static void MenuOpen()
        {
            Open();
        }

        public static TunerSessionsWindow Open()
        {
            TunerSessionsWindow window = GetWindow<TunerSessionsWindow>();
            window.titleContent = new GUIContent("Texture Tuner Sessions");
            window.minSize = new Vector2(900f, 360f);
            return window;
        }

        /// <summary>
        /// セッションの JSON を読み込み、現在のプロジェクトと照合する。
        /// </summary>
        public bool LoadSession(string path)
        {
            _messages.Clear();
            if (!SessionImportService.TryLoad(path, out TuningSessionData session, out string error))
            {
                _session = null;
                _rows = new List<SessionRow>();
                _messages.Add($"Could not load {path}: {error}");
                return false;
            }

            _session = session;
            _sessionPath = path;
            _rows = SessionImportService.BuildRows(session);
            _messages.Add($"Loaded {_rows.Count} item(s) from {Path.GetFileName(path)}.");
            return true;
        }

        private void OnEnable()
        {
            _transactions = ApplyTransactionStore.LoadAll();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSessionInfo();
            DrawRows();
            DrawActions();
            DrawMessages();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Load JSON...", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    LoadFromFilePanel();
                }

                if (GUILayout.Button("Pull from device (adb)", EditorStyles.toolbarButton, GUILayout.Width(160f)))
                {
                    PullFromDevice();
                }

                using (new EditorGUI.DisabledScope(_session == null))
                {
                    if (GUILayout.Button("Re-check", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        LoadSession(_sessionPath);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawSessionInfo()
        {
            if (_session == null)
            {
                EditorGUILayout.HelpBox("Load a session exported from the tuning build, or pull it from a connected device.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("File", _sessionPath);
            EditorGUILayout.LabelField("Build / device", $"{_session.BuildId}   {_session.Device}   {_session.GraphicsApi}   Quality: {_session.QualityName}   Unity {_session.UnityVersion}");
        }

        private void DrawRows()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(string.Empty, GUILayout.Width(SelectWidth));
                GUILayout.Label("Status", GUILayout.Width(StatusWidth));
                GUILayout.Label("Asset", GUILayout.MinWidth(200f));
                GUILayout.Label("Current (importer)", GUILayout.Width(ValueWidth));
                GUILayout.Label("Adjusted (device)", GUILayout.Width(ValueWidth));
                GUILayout.Label("Changed", GUILayout.Width(FieldsWidth));
            }

            _rowScroll = EditorGUILayout.BeginScrollView(_rowScroll);
            foreach (SessionRow row in _rows)
            {
                DrawRow(row);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(SessionRow row)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!row.CanApply(_allowStale, _allowConflict)))
                {
                    row.IsSelected = EditorGUILayout.Toggle(row.IsSelected, GUILayout.Width(SelectWidth));
                }

                GUILayout.Label(new GUIContent(row.Status.ToString(), row.Note), GUILayout.Width(StatusWidth));
                if (GUILayout.Button(new GUIContent(row.AssetPath, row.Note), EditorStyles.label, GUILayout.MinWidth(200f)))
                {
                    PingAsset(row.AssetPath);
                }

                bool hasEntry = row.Entry != null;
                GUILayout.Label(row.Current.ToString(), GUILayout.Width(ValueWidth));
                GUILayout.Label(hasEntry ? row.Entry.Adjusted.ToString() : "-", GUILayout.Width(ValueWidth));
                GUILayout.Label(hasEntry ? string.Join(", ", row.Entry.ChangedFields) : "-", GUILayout.Width(FieldsWidth));
            }

            if (!string.IsNullOrEmpty(row.Note))
            {
                EditorGUILayout.LabelField(" ", row.Note, EditorStyles.miniLabel);
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _allowStale = EditorGUILayout.ToggleLeft("Allow stale (source changed)", _allowStale, GUILayout.Width(210f));
                _allowConflict = EditorGUILayout.ToggleLeft("Allow conflict (settings changed)", _allowConflict, GUILayout.Width(230f));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(CountApplicable() == 0))
                {
                    if (GUILayout.Button($"Apply selected ({CountApplicable()})", GUILayout.Width(170f)))
                    {
                        ApplySelected();
                    }
                }

                using (new EditorGUI.DisabledScope(_transactions.Count == 0))
                {
                    if (GUILayout.Button("Restore last Apply", GUILayout.Width(150f)))
                    {
                        RestoreLast();
                    }
                }
            }
        }

        private void DrawMessages()
        {
            if (_messages.Count == 0)
            {
                return;
            }

            _messageScroll = EditorGUILayout.BeginScrollView(_messageScroll, GUILayout.Height(90f));
            foreach (string message in _messages)
            {
                EditorGUILayout.LabelField(message, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private int CountApplicable()
        {
            int count = 0;
            foreach (SessionRow row in _rows)
            {
                if (row.IsSelected && row.CanApply(_allowStale, _allowConflict))
                {
                    count++;
                }
            }

            return count;
        }

        private void LoadFromFilePanel()
        {
            string path = EditorUtility.OpenFilePanel("Load tuning session", ApplyTransactionStore.WorkFolder, "json");
            if (!string.IsNullOrEmpty(path))
            {
                LoadSession(path);
            }
        }

        private void PullFromDevice()
        {
            _messages.Clear();
            if (!AdbSessionPuller.TryPull(out string localFolder, out string message))
            {
                _messages.Add(message);
                return;
            }

            // 取得したファイルのうち、名前の順で最後（最も新しい日時）のものを開く。
            string[] files = Directory.GetFiles(localFolder, "*.json", SearchOption.AllDirectories);
            System.Array.Sort(files, System.StringComparer.Ordinal);
            LoadSession(files[files.Length - 1].Replace('\\', '/'));
            _messages.Add($"Pulled {files.Length} file(s) to {localFolder}. Use 'Load JSON...' to open another one.");
        }

        private void ApplySelected()
        {
            int count = CountApplicable();
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Apply texture settings",
                $"Apply the adjusted values to {count} texture(s)?\n\n" +
                "Mip Map Bias, Aniso Level and Filter Mode are import settings shared by all platforms, " +
                "so the change also affects platforms other than Android.\n\n" +
                "Only the fields changed on the device are written. The textures are reimported. " +
                "You can undo this with 'Restore last Apply'.",
                "Apply",
                "Cancel");
            if (!isConfirmed)
            {
                return;
            }

            _messages.Clear();
            ApplyTransaction transaction;
            try
            {
                transaction = TextureImporterApplier.Apply(_rows, _session.BuildId, _allowStale, _allowConflict, ShowApplyProgress, _messages);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (transaction != null)
            {
                string path = ApplyTransactionStore.Save(transaction);
                _messages.Add($"Applied {transaction.Items.Count} texture(s). Record saved: {path}");
                _transactions = ApplyTransactionStore.LoadAll();
            }
            else
            {
                _messages.Add("Nothing was applied.");
            }

            _rows = SessionImportService.BuildRows(_session);
        }

        private void RestoreLast()
        {
            ApplyTransaction transaction = _transactions[0];
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Restore texture settings",
                $"Restore {transaction.Items.Count} texture(s) to the values before the Apply made at {transaction.CreatedAtUtc}?\n\n" +
                "Textures whose settings were changed after that Apply are left as they are.",
                "Restore",
                "Cancel");
            if (!isConfirmed)
            {
                return;
            }

            _messages.Clear();
            int restoredCount;
            try
            {
                restoredCount = TextureImporterApplier.Restore(transaction, ShowRestoreProgress, _messages);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _messages.Add($"Restored {restoredCount} of {transaction.Items.Count} texture(s).");
            if (_session != null)
            {
                _rows = SessionImportService.BuildRows(_session);
            }
        }

        private static bool ShowApplyProgress(string assetPath, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar("Applying texture settings", assetPath, progress);
        }

        private static bool ShowRestoreProgress(string assetPath, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar("Restoring texture settings", assetPath, progress);
        }

        private static void PingAsset(string assetPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
