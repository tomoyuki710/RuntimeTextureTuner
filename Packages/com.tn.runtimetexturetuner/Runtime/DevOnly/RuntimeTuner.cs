using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整機能の本体。入力を読み、パネルの開閉、レイによる操作と対象の選択を行う。
    /// 既存の EventSystem、Interactor、Collider、Layer を使わず、コンテンツ側のオブジェクトも変更しない。
    /// </summary>
    public sealed class RuntimeTuner : MonoBehaviour
    {
        private const float PointerLength = 8f;
        private const float DiagnosticsIntervalSeconds = 1f;
        private const string LogPrefix = "[RuntimeTextureTuner]";

        private TunerSettings _settings;
        private TunerSession _session;
        private ITunerInputAdapter _input;
        private RuntimeTunerPanel _panel;
        private TunerPointerVisual _pointer;
        private bool _isCatalogDirty = true;
        private bool _isPanelDirty;
        private bool _wasSelectHeld;
        private bool _isChordConsumed;
        private bool _isDraggingPanel;
        private float _dragDistance;
        private Vector2 _dragLocalPoint;
        private float _chordHeldSeconds;
        private float _nextDiagnosticsTime;

        /// <summary>
        /// パネルが開いたとき（true）と隠れたとき（false）に呼ばれる。
        /// 調整中だけ自分の入力を止めたいコンテンツは、これを受けて処理する。
        /// </summary>
        public static event Action<bool> OnTuningModeChanged;

        /// <summary>
        /// 起動中の調整機能。起動していなければ null。
        /// </summary>
        public static RuntimeTuner Current { get; private set; }

        public TunerSession Session => _session;

        public bool IsPanelVisible => _panel != null && _panel.IsVisible;

        /// <summary>
        /// 調整機能を起動する。既に起動していれば何もしない。
        /// </summary>
        public static RuntimeTuner EnsureStarted()
        {
            if (Current != null)
            {
                return Current;
            }

            var tunerObject = new GameObject(nameof(RuntimeTuner));
            DontDestroyOnLoad(tunerObject);
            return tunerObject.AddComponent<RuntimeTuner>();
        }

        /// <summary>
        /// 入力の読み取り方を差し替える。
        /// </summary>
        public void SetInputAdapter(ITunerInputAdapter inputAdapter)
        {
            _input = inputAdapter;
        }

        /// <summary>
        /// 頭の正面にパネルを開く。既に開いている場合は正面に置き直す。
        /// </summary>
        public void OpenPanel()
        {
            if (!_input.TryGetHeadPose(out Pose head))
            {
                Debug.LogWarning($"{LogPrefix} The head pose is not available. The panel was not opened.");
                return;
            }

            bool wasVisible = _panel.IsVisible;
            _panel.Show(head);
            if (!wasVisible)
            {
                _isCatalogDirty = true;
                Debug.Log($"{LogPrefix} Tuning panel opened.");
                OnTuningModeChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// パネルを隠す。調整値は維持する。
        /// </summary>
        public void HidePanel()
        {
            if (!_panel.IsVisible)
            {
                return;
            }

            _isDraggingPanel = false;
            _panel.Hide();
            _pointer.HideRay();
            _pointer.HideBounds();
            Debug.Log($"{LogPrefix} Tuning panel hidden. Adjusted values are kept.");
            OnTuningModeChanged?.Invoke(false);
        }

        private void Awake()
        {
            Current = this;
            _settings = TunerSettings.LoadOrCreateDefault();
            _session = new TunerSession(_settings);
            _input = new XrDeviceInputAdapter(_settings);
            _panel = new RuntimeTunerPanel(transform, _session, _settings, OnHideRequested, OnRescanRequested);
            _pointer = new TunerPointerVisual(transform);

            _session.OnChanged += OnSessionChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            Debug.Log($"{LogPrefix} TUNING BUILD: the runtime texture tuner is active. Hold the open buttons for {_settings.OpenHoldSeconds:0.0} s to open the panel.");
        }

        private void Update()
        {
            Tick();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (_session != null)
            {
                _session.OnChanged -= OnSessionChanged;

                // Editor ではテクスチャアセットのインスタンスが残るため、必ず元の値に戻す。
                _session.ResetAll();
            }

            if (_panel != null)
            {
                _panel.Destroy();
            }

            if (_pointer != null)
            {
                _pointer.Destroy();
            }

            if (Current == this)
            {
                Current = null;
            }
        }

        private void OnHideRequested()
        {
            HidePanel();
        }

        private void OnRescanRequested()
        {
            _isCatalogDirty = true;
        }

        private void OnSessionChanged()
        {
            // 1フレームに何度変わっても、表示の更新は1回にまとめる。
            _isPanelDirty = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isCatalogDirty = true;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _isCatalogDirty = true;
        }

        private void Tick()
        {
            _input.Poll();
            UpdateOpenGesture();
            _session.Tick(Time.unscaledTime);
            if (!_panel.IsVisible)
            {
                return;
            }

            if (_isCatalogDirty)
            {
                _isCatalogDirty = false;
                _session.RefreshCatalog(transform);
            }

            UpdatePointer();
            UpdateSelectionBounds();
            RefreshPanel();
        }

        private void UpdateOpenGesture()
        {
            if (!_input.IsOpenChordHeld)
            {
                _chordHeldSeconds = 0f;
                _isChordConsumed = false;
                return;
            }

            if (_isChordConsumed)
            {
                return;
            }

            _chordHeldSeconds += Time.unscaledDeltaTime;
            if (_chordHeldSeconds < _settings.OpenHoldSeconds)
            {
                return;
            }

            // 押し続けている間に開閉を繰り返さないよう、離すまで次を受け付けない。
            _isChordConsumed = true;
            if (_panel.IsVisible)
            {
                HidePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        private void UpdatePointer()
        {
            bool isSelectHeld = _input.IsSelectHeld;
            bool isSelectDown = isSelectHeld && !_wasSelectHeld;
            _wasSelectHeld = isSelectHeld;

            if (!_input.TryGetPointerRay(out Ray ray))
            {
                _isDraggingPanel = false;
                _pointer.HideRay();
                _panel.UpdatePointer(null, false, false);
                return;
            }

            if (_isDraggingPanel)
            {
                DragPanel(ray, isSelectHeld);
                return;
            }

            if (_panel.TryGetLocalPoint(ray, out Vector2 localPoint, out float distance))
            {
                if (isSelectDown && _panel.IsDragArea(localPoint))
                {
                    // 上端の帯をつかんだ。以後、決定ボタンを離すまでパネルがレイに付いてくる。
                    _isDraggingPanel = true;
                    _dragDistance = distance;
                    _dragLocalPoint = localPoint;
                    _panel.UpdatePointer(null, false, false);
                    return;
                }

                _pointer.ShowRay(ray.origin, ray.GetPoint(distance));
                _panel.UpdatePointer(localPoint, isSelectDown, isSelectHeld);
                return;
            }

            _pointer.ShowRay(ray.origin, ray.GetPoint(PointerLength));
            _panel.UpdatePointer(null, false, isSelectHeld);
            if (isSelectDown)
            {
                _session.SelectAlongRay(ray);
            }
        }

        private void DragPanel(Ray ray, bool isSelectHeld)
        {
            if (!isSelectHeld)
            {
                _isDraggingPanel = false;
                return;
            }

            // つかんだときの距離を保ったまま、レイの先にパネルを運ぶ。
            Vector3 grabPoint = ray.GetPoint(_dragDistance);
            Vector3 headPosition = _input.TryGetHeadPose(out Pose head) ? head.position : ray.origin;
            _panel.MoveTo(grabPoint, _dragLocalPoint, headPosition);
            _pointer.ShowRay(ray.origin, grabPoint);
        }

        private void UpdateSelectionBounds()
        {
            Renderer selected = _session.SelectedRenderer;
            if (selected == null)
            {
                _pointer.HideBounds();
                return;
            }

            _pointer.ShowBounds(selected.bounds);
        }

        private void RefreshPanel()
        {
            if (_isPanelDirty)
            {
                _isPanelDirty = false;
                _panel.Refresh();
                return;
            }

            if (Time.unscaledTime >= _nextDiagnosticsTime)
            {
                _nextDiagnosticsTime = Time.unscaledTime + DiagnosticsIntervalSeconds;
                _panel.RefreshDiagnostics();
            }
        }
    }
}
