using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整中の状態（対象の一覧、選択、調整値）と、それに対する操作をまとめる。表示や入力には依存しない。
    /// </summary>
    public sealed class TunerSession
    {
        // スライダーの操作ごとにテクスチャへ書き込まず、最大 20Hz にまとめる。
        private const float ApplyIntervalSeconds = 0.05f;

        private readonly TunerSettings _settings;
        private readonly RendererCatalog _catalog = new RendererCatalog();
        private readonly SamplerOverrideService _service = new SamplerOverrideService();
        private readonly List<Renderer> _rendererBuffer = new List<Renderer>();
        private readonly List<SelectionCandidate> _candidates = new List<SelectionCandidate>();

        private TextureSamplerSettings _pendingSettings;
        private bool _hasPendingSettings;
        private float _nextApplyTime;

        public TunerSession(TunerSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 選択や調整値など、表示に関わる状態が変わったときに呼ばれる。
        /// </summary>
        public event Action OnChanged;

        public TunerListMode ListMode { get; private set; }

        public IReadOnlyList<SelectionCandidate> Candidates => _candidates;

        public Renderer SelectedRenderer { get; private set; }

        public TextureTarget SelectedTarget { get; private set; }

        public IReadOnlyList<TextureTarget> SelectedRendererTargets => _catalog.GetTargets(SelectedRenderer);

        public CompareMode CompareMode => _service.CompareMode;

        public int RendererCount => _catalog.Renderers.Count;

        public int KeptCount => _service.CountKept();

        /// <summary>
        /// シーン内の Renderer を調べ直す。excludedRoot の配下（調整機能自身の表示物）は対象から外す。
        /// </summary>
        public void RefreshCatalog(Transform excludedRoot)
        {
            RendererCatalog.FindSceneRenderers(_rendererBuffer);
            if (excludedRoot != null)
            {
                _rendererBuffer.RemoveAll(candidate => candidate.transform.IsChildOf(excludedRoot));
            }

            _catalog.Rebuild(_rendererBuffer);
            _candidates.Clear();
            RestoreSelection();
            NotifyChanged();
        }

        /// <summary>
        /// レイで指した方向にある Renderer を候補として一覧に出す。
        /// </summary>
        public void SelectAlongRay(Ray ray)
        {
            SelectionService.FindCandidates(ray, _catalog.Renderers, _settings.MaxSelectionDistance, _candidates);
            ListMode = TunerListMode.Candidates;
            NotifyChanged();
        }

        /// <summary>
        /// 候補の中から Renderer を確定し、そのテクスチャ一覧に切り替える。
        /// </summary>
        public void SelectCandidate(int index)
        {
            if (index < 0 || index >= _candidates.Count)
            {
                return;
            }

            FlushPendingSettings();
            SelectedRenderer = _candidates[index].Renderer;
            SelectedTarget = null;
            ListMode = TunerListMode.Textures;

            // 最初の調整可能なテクスチャを選んでおく。別のテクスチャは一覧から選び直せる。
            IReadOnlyList<TextureTarget> targets = SelectedRendererTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsAdjustable)
                {
                    SelectTarget(targets[i]);
                    break;
                }
            }

            NotifyChanged();
        }

        /// <summary>
        /// 選択中の Renderer のテクスチャ一覧から、調整するテクスチャを選ぶ。
        /// </summary>
        public void SelectTexture(int index)
        {
            IReadOnlyList<TextureTarget> targets = SelectedRendererTargets;
            if (index < 0 || index >= targets.Count)
            {
                return;
            }

            FlushPendingSettings();
            SelectTarget(targets[index]);
            NotifyChanged();
        }

        public void ShowCandidates()
        {
            ListMode = TunerListMode.Candidates;
            NotifyChanged();
        }

        /// <summary>
        /// 選択中のテクスチャの元の値、調整値、Keep の状態を取得する。調整できる対象を選んでいない場合は false を返す。
        /// </summary>
        public bool TryGetSelectedSettings(out TextureSamplerSettings original, out TextureSamplerSettings adjusted, out bool isKept)
        {
            original = default;
            adjusted = default;
            isKept = false;
            if (!IsSelectedAdjustable())
            {
                return false;
            }

            Texture texture = SelectedTarget.Texture;
            bool hasOriginal = _service.TryGetOriginal(texture, out original);
            bool hasAdjusted = _service.TryGetAdjusted(texture, out adjusted);
            if (!hasOriginal || !hasAdjusted)
            {
                return false;
            }

            if (_hasPendingSettings)
            {
                adjusted = _pendingSettings;
            }

            bool hasKept = _service.TryGetKept(texture, out TextureSamplerSettings kept);
            isKept = hasKept && kept.Equals(adjusted);
            return true;
        }

        public void SetBias(float bias)
        {
            if (!TryGetSelectedSettings(out _, out TextureSamplerSettings adjusted, out _))
            {
                return;
            }

            float clamped = Mathf.Clamp(bias, _settings.MinBias, _settings.MaxBias);
            float rounded = Mathf.Round(clamped * 100f) / 100f;
            SetPendingSettings(new TextureSamplerSettings(rounded, adjusted.AnisoLevel, adjusted.FilterMode));
        }

        /// <summary>
        /// Bias を設定の刻み幅の整数倍だけ動かす。
        /// </summary>
        public void NudgeBias(int steps)
        {
            if (TryGetSelectedSettings(out _, out TextureSamplerSettings adjusted, out _))
            {
                SetBias(adjusted.MipMapBias + steps * _settings.BiasStep);
            }
        }

        public void SetAnisoLevel(int anisoLevel)
        {
            if (TryGetSelectedSettings(out _, out TextureSamplerSettings adjusted, out _))
            {
                SetPendingSettings(new TextureSamplerSettings(adjusted.MipMapBias, anisoLevel, adjusted.FilterMode));
            }
        }

        public void SetFilterMode(FilterMode filterMode)
        {
            if (TryGetSelectedSettings(out _, out TextureSamplerSettings adjusted, out _))
            {
                SetPendingSettings(new TextureSamplerSettings(adjusted.MipMapBias, adjusted.AnisoLevel, filterMode));
            }
        }

        /// <summary>
        /// Original と Adjusted の表示を切り替える。管理している全テクスチャに反映する。
        /// </summary>
        public void ToggleCompareMode()
        {
            FlushPendingSettings();
            bool isAdjusted = _service.CompareMode == CompareMode.Adjusted;
            _service.SetCompareMode(isAdjusted ? CompareMode.Original : CompareMode.Adjusted);
            NotifyChanged();
        }

        /// <summary>
        /// 選択中のテクスチャの現在の調整値を、採用予定として記録する。
        /// </summary>
        public void KeepSelected()
        {
            if (!IsSelectedAdjustable())
            {
                return;
            }

            FlushPendingSettings();
            _service.TryKeep(SelectedTarget.Texture);
            NotifyChanged();
        }

        public void ResetSelected()
        {
            if (!IsSelectedAdjustable())
            {
                return;
            }

            _hasPendingSettings = false;
            _service.TryReset(SelectedTarget.Texture);
            NotifyChanged();
        }

        /// <summary>
        /// 管理している全テクスチャを元の値に戻し、採用予定も取り消す。
        /// </summary>
        public void ResetAll()
        {
            _hasPendingSettings = false;
            _service.ResetAll();
            NotifyChanged();
        }

        /// <summary>
        /// まとめておいた調整値を、間隔を空けてテクスチャへ書き込む。毎フレーム呼ぶ。
        /// </summary>
        public void Tick(float unscaledTime)
        {
            if (!_hasPendingSettings || unscaledTime < _nextApplyTime)
            {
                return;
            }

            _nextApplyTime = unscaledTime + ApplyIntervalSeconds;
            FlushPendingSettings();
        }

        private void SelectTarget(TextureTarget target)
        {
            SelectedTarget = target;
            if (target.IsAdjustable)
            {
                // 初めて選んだ時点の実効値を、元の値として記録する。
                _service.TryRegister(target.Texture);
            }
        }

        private void RestoreSelection()
        {
            // 一覧を作り直した後、破棄された対象への参照を残さない。
            if (SelectedRenderer == null)
            {
                SelectedRenderer = null;
                SelectedTarget = null;
                ListMode = TunerListMode.Candidates;
                return;
            }

            Texture selectedTexture = SelectedTarget == null ? null : SelectedTarget.Texture;
            SelectedTarget = _catalog.TryGetTarget(selectedTexture, out TextureTarget target) ? target : null;
        }

        private bool IsSelectedAdjustable()
        {
            bool hasTexture = SelectedTarget != null && SelectedTarget.Texture != null;
            return hasTexture && SelectedTarget.IsAdjustable;
        }

        private void SetPendingSettings(TextureSamplerSettings settings)
        {
            _pendingSettings = settings;
            _hasPendingSettings = true;
            NotifyChanged();
        }

        private void FlushPendingSettings()
        {
            if (!_hasPendingSettings)
            {
                return;
            }

            _hasPendingSettings = false;
            if (IsSelectedAdjustable())
            {
                _service.TrySetAdjusted(SelectedTarget.Texture, _pendingSettings);
            }
        }

        private void NotifyChanged()
        {
            OnChanged?.Invoke();
        }
    }
}
