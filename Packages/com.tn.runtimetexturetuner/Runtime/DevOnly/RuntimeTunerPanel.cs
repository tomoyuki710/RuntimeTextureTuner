using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// HMD 内に出す調整パネル。World Space に置き、開いた位置に留まる（頭には追従しない）。
    /// </summary>
    internal sealed class RuntimeTunerPanel
    {
        private const float Width = 1.04f;
        private const float Height = 0.7f;
        private const float HeightOffset = -0.08f;
        private const float TextSize = 0.0036f;
        private const float SmallTextSize = 0.003f;
        private const float RowHeight = 0.044f;
        private const float RowPitch = 0.05f;
        private const int RowCount = 7;
        private const int MaxNameLength = 34;
        private const int MaxRowLength = 46;
        private const int MaxMessageLength = 40;
        private const int MaxPathLength = 46;
        private const float RecommendedMinBias = -0.5f;
        private const float DragBarHeight = 0.065f;

        private static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.05f, 1f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color ActiveColor = new Color(0.1f, 0.45f, 0.3f, 1f);
        private static readonly Color DisabledColor = new Color(0.12f, 0.12f, 0.13f, 1f);
        private static readonly Color TrackColor = new Color(0.12f, 0.12f, 0.16f, 1f);
        private static readonly Color HandleColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color WarningColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DragBarColor = new Color(0.1f, 0.11f, 0.17f, 1f);
        private static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

        private static readonly int[] AnisoChoices = { 0, 1, 2, 4, 8, 16 };
        private static readonly FilterMode[] FilterChoices = { FilterMode.Point, FilterMode.Bilinear, FilterMode.Trilinear };

        private readonly TunerSession _session;
        private readonly TunerSettings _settings;
        private readonly GameObject _root;
        private readonly List<PanelWidget> _widgets = new List<PanelWidget>();
        private readonly PanelWidget[] _rows = new PanelWidget[RowCount];
        private readonly PanelWidget[] _anisoButtons = new PanelWidget[AnisoChoices.Length];
        private readonly PanelWidget[] _filterButtons = new PanelWidget[FilterChoices.Length];
        private readonly StringBuilder _text = new StringBuilder(512);

        private PanelWidget _statusLabel;
        private PanelWidget _listHeader;
        private PanelWidget _pageLabel;
        private PanelWidget _infoLabel;
        private PanelWidget _warningLabel;
        private PanelWidget _biasLabel;
        private PanelWidget _biasTrack;
        private PanelWidget _biasHandle;
        private PanelWidget _compareButton;
        private PanelWidget _keepButton;
        private PanelWidget _diagnosticsLabel;
        private PanelWidget _exportButton;
        private PanelWidget _exportMessageLabel;
        private PanelWidget _hovered;
        private PanelWidget _pressed;
        private int _page;

        public RuntimeTunerPanel(Transform parent, TunerSession session, TunerSettings settings, Action onHide, Action onRefresh)
        {
            _session = session;
            _settings = settings;
            _root = new GameObject(nameof(RuntimeTunerPanel));
            _root.transform.SetParent(parent, false);

            Build(onHide, onRefresh);
            _root.SetActive(false);
        }

        public bool IsVisible => _root.activeSelf;

        /// <summary>
        /// 頭の正面（水平方向）にパネルを置いて表示する。
        /// </summary>
        public void Show(Pose head)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 position = head.position + forward * _settings.PanelDistance + Vector3.up * HeightOffset;
            _root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            _root.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// 指定した位置が、パネルをつかんで動かせる領域（上端の帯のうちボタン以外）かどうか。
        /// </summary>
        public bool IsDragArea(Vector2 point)
        {
            bool isInBar = point.y >= Height * 0.5f - DragBarHeight;
            return isInBar && FindInteractive(point) == null;
        }

        /// <summary>
        /// つかんだ点（パネルのローカル座標 grabLocal）がワールド座標の grabPoint に来るようにパネルを動かす。
        /// パネルは頭の方を向いたまま、水平に回る。
        /// </summary>
        public void MoveTo(Vector3 grabPoint, Vector2 grabLocal, Vector3 headPosition)
        {
            Vector3 forward = Vector3.ProjectOnPlane(grabPoint - headPosition, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = _root.transform.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            Vector3 position = grabPoint - rotation * new Vector3(grabLocal.x, grabLocal.y, 0f);
            _root.transform.SetPositionAndRotation(position, rotation);
        }

        public void Hide()
        {
            SetHovered(null);
            _pressed = null;
            _root.SetActive(false);
        }

        public void Destroy()
        {
            foreach (PanelWidget widget in _widgets)
            {
                widget.Destroy();
            }
        }

        /// <summary>
        /// レイがパネルに当たる位置を、パネルのローカル座標で求める。当たらない場合は false を返す。
        /// </summary>
        public bool TryGetLocalPoint(Ray ray, out Vector2 localPoint, out float distance)
        {
            localPoint = default;
            distance = 0f;
            Transform panel = _root.transform;
            var plane = new Plane(panel.forward, panel.position);
            if (!IsVisible || !plane.Raycast(ray, out distance))
            {
                return false;
            }

            Vector3 local = panel.InverseTransformPoint(ray.GetPoint(distance));
            localPoint = new Vector2(local.x, local.y);
            bool isInsideWidth = Mathf.Abs(local.x) <= Width * 0.5f;
            bool isInsideHeight = Mathf.Abs(local.y) <= Height * 0.5f;
            return isInsideWidth && isInsideHeight;
        }

        /// <summary>
        /// レイの位置と決定ボタンの状態から、ホバー、押下、ドラッグを処理する。
        /// point が null の場合、レイはパネルの外にある。
        /// </summary>
        public void UpdatePointer(Vector2? point, bool isSelectDown, bool isSelectHeld)
        {
            PanelWidget hovered = point.HasValue ? FindInteractive(point.Value) : null;
            SetHovered(hovered);

            if (isSelectDown)
            {
                _pressed = hovered;
                if (hovered != null && hovered.OnPress != null)
                {
                    hovered.OnPress();
                }
            }

            if (!isSelectHeld)
            {
                _pressed = null;
            }

            bool isDragging = _pressed != null && _pressed.OnDrag != null;
            if (isDragging && point.HasValue)
            {
                Rect rect = _pressed.Rect;
                _pressed.OnDrag(Mathf.InverseLerp(rect.xMin, rect.xMax, point.Value.x));
            }
        }

        /// <summary>
        /// セッションの状態を表示に反映する。
        /// </summary>
        public void Refresh()
        {
            if (!IsVisible)
            {
                return;
            }

            RefreshStatus();
            RefreshList();
            RefreshTarget();
            RefreshDiagnostics();
        }

        public void RefreshDiagnostics()
        {
            if (!IsVisible)
            {
                return;
            }

            _text.Clear();
            TunerDiagnostics.AppendTo(_text);
            _diagnosticsLabel.SetText(_text.ToString());
        }

        private void Build(Action onHide, Action onRefresh)
        {
            float left = -Width * 0.5f;
            float top = Height * 0.5f;
            AddWidget("Background", new Rect(left, -Height * 0.5f, Width, Height), PanelColor, TextSize, TextAnchor.MiddleCenter, 0);

            // 上端の帯をつかんで動かせる。帯の上のボタンは帯より手前の層に置く。
            AddWidget("DragBar", new Rect(left, top - DragBarHeight, Width, DragBarHeight), DragBarColor, TextSize, TextAnchor.MiddleCenter, 1);
            PanelWidget title = AddLabel("Title", new Rect(left + 0.01f, top - 0.05f, 0.5f, 0.04f), SmallTextSize, TextAnchor.MiddleLeft);
            title.SetText("TEXTURE TUNER (tuning build)   [drag this bar to move]");
            _statusLabel = AddLabel("Status", new Rect(0.0f, top - 0.05f, 0.36f, 0.04f), TextSize, TextAnchor.MiddleLeft);
            PanelWidget hideButton = AddWidget("Hide", new Rect(Width * 0.5f - 0.15f, top - 0.055f, 0.14f, RowHeight), ButtonColor, SmallTextSize, TextAnchor.MiddleCenter, 2);
            hideButton.SetText("Hide panel");
            hideButton.OnPress = onHide;

            BuildList(left + 0.01f, top - 0.1f, onRefresh);
            BuildTargetControls(left + 0.47f, top - 0.1f);

            _diagnosticsLabel = AddLabel("Diagnostics", new Rect(left + 0.01f, -Height * 0.5f + 0.005f, Width - 0.02f, 0.085f), SmallTextSize, TextAnchor.UpperLeft);
        }

        private void BuildList(float left, float top, Action onRefresh)
        {
            const float ListWidth = 0.44f;
            _listHeader = AddLabel("ListHeader", new Rect(left, top - 0.035f, ListWidth, 0.035f), SmallTextSize, TextAnchor.MiddleLeft);

            float rowTop = top - 0.04f;
            for (int i = 0; i < RowCount; i++)
            {
                int rowIndex = i;
                var rect = new Rect(left, rowTop - (i + 1) * RowPitch + (RowPitch - RowHeight), ListWidth, RowHeight);
                PanelWidget row = AddWidget($"Row{i}", rect, ButtonColor, SmallTextSize, TextAnchor.MiddleLeft, 1);
                row.OnPress = () => OnRowPressed(rowIndex);
                _rows[i] = row;
            }

            float buttonY = rowTop - (RowCount + 1) * RowPitch + (RowPitch - RowHeight);
            AddButton("Candidates", new Rect(left, buttonY, 0.13f, RowHeight), "Candidates", OnCandidatesPressed);
            AddButton("Prev", new Rect(left + 0.14f, buttonY, 0.055f, RowHeight), "<", OnPreviousPagePressed);
            _pageLabel = AddLabel("Page", new Rect(left + 0.2f, buttonY, 0.06f, RowHeight), SmallTextSize, TextAnchor.MiddleCenter);
            AddButton("Next", new Rect(left + 0.265f, buttonY, 0.055f, RowHeight), ">", OnNextPagePressed);
            AddButton("Rescan", new Rect(left + 0.33f, buttonY, 0.11f, RowHeight), "Rescan", onRefresh);
        }

        private void BuildTargetControls(float left, float top)
        {
            const float ColumnWidth = 0.55f;
            const float LabelWidth = 0.13f;
            _infoLabel = AddLabel("Info", new Rect(left, top - 0.135f, ColumnWidth, 0.135f), SmallTextSize, TextAnchor.UpperLeft);
            _warningLabel = AddLabel("Warning", new Rect(left, top - 0.2f, ColumnWidth, 0.06f), SmallTextSize, TextAnchor.UpperLeft);
            _warningLabel.SetTextColor(WarningColor);

            float biasY = top - 0.255f;
            _biasLabel = AddLabel("BiasLabel", new Rect(left, biasY, LabelWidth, RowHeight), TextSize, TextAnchor.MiddleLeft);
            AddButton("BiasDown", new Rect(left + LabelWidth, biasY, 0.05f, RowHeight), "-", OnBiasDownPressed);
            _biasTrack = AddWidget("BiasTrack", new Rect(left + LabelWidth + 0.06f, biasY + 0.012f, 0.29f, 0.02f), TrackColor, SmallTextSize, TextAnchor.MiddleCenter, 1);
            _biasTrack.OnDrag = OnBiasDragged;
            _biasHandle = AddWidget("BiasHandle", new Rect(left + LabelWidth + 0.06f, biasY + 0.002f, 0.014f, 0.04f), HandleColor, SmallTextSize, TextAnchor.MiddleCenter, 2);
            AddButton("BiasUp", new Rect(left + LabelWidth + 0.36f, biasY, 0.05f, RowHeight), "+", OnBiasUpPressed);

            float anisoY = biasY - RowPitch - 0.004f;
            AddLabel("AnisoLabel", new Rect(left, anisoY, LabelWidth, RowHeight), TextSize, TextAnchor.MiddleLeft).SetText("Aniso");
            for (int i = 0; i < AnisoChoices.Length; i++)
            {
                int anisoLevel = AnisoChoices[i];
                var rect = new Rect(left + LabelWidth + i * 0.07f, anisoY, 0.062f, RowHeight);
                _anisoButtons[i] = AddButton($"Aniso{anisoLevel}", rect, anisoLevel.ToString(), () => OnAnisoPressed(anisoLevel));
            }

            float filterY = anisoY - RowPitch - 0.004f;
            AddLabel("FilterLabel", new Rect(left, filterY, LabelWidth, RowHeight), TextSize, TextAnchor.MiddleLeft).SetText("Filter");
            for (int i = 0; i < FilterChoices.Length; i++)
            {
                FilterMode filterMode = FilterChoices[i];
                var rect = new Rect(left + LabelWidth + i * 0.14f, filterY, 0.132f, RowHeight);
                _filterButtons[i] = AddButton($"Filter{filterMode}", rect, filterMode.ToString(), () => OnFilterPressed(filterMode));
            }

            float actionY = filterY - RowPitch - 0.014f;
            _compareButton = AddButton("Compare", new Rect(left, actionY, 0.2f, RowHeight), string.Empty, OnComparePressed);
            _keepButton = AddButton("Keep", new Rect(left + 0.21f, actionY, 0.09f, RowHeight), "Keep", OnKeepPressed);
            AddButton("Reset", new Rect(left + 0.31f, actionY, 0.1f, RowHeight), "Reset", OnResetPressed);
            AddButton("ResetAll", new Rect(left + 0.42f, actionY, 0.13f, RowHeight), "Reset all", OnResetAllPressed);

            float exportY = actionY - RowPitch - 0.004f;
            _exportButton = AddButton("Export", new Rect(left, exportY, 0.2f, RowHeight), string.Empty, OnExportPressed);
            _exportMessageLabel = AddLabel("ExportMessage", new Rect(left + 0.21f, exportY, 0.34f, RowHeight), SmallTextSize, TextAnchor.MiddleLeft);
        }

        private PanelWidget AddWidget(string name, Rect rect, Color color, float textSize, TextAnchor anchor, int layer)
        {
            var widget = new PanelWidget(_root.transform, name, rect, color, textSize, anchor, layer);
            _widgets.Add(widget);
            return widget;
        }

        private PanelWidget AddLabel(string name, Rect rect, float textSize, TextAnchor anchor)
        {
            return AddWidget(name, rect, Transparent, textSize, anchor, 1);
        }

        private PanelWidget AddButton(string name, Rect rect, string text, Action onPress)
        {
            PanelWidget button = AddWidget(name, rect, ButtonColor, SmallTextSize, TextAnchor.MiddleCenter, 1);
            button.SetText(text);
            button.OnPress = onPress;
            return button;
        }

        private PanelWidget FindInteractive(Vector2 point)
        {
            foreach (PanelWidget widget in _widgets)
            {
                if (widget.IsInteractive && widget.Contains(point))
                {
                    return widget;
                }
            }

            return null;
        }

        private void SetHovered(PanelWidget hovered)
        {
            if (hovered == _hovered)
            {
                return;
            }

            if (_hovered != null)
            {
                _hovered.SetHovered(false);
            }

            if (hovered != null)
            {
                hovered.SetHovered(true);
            }

            _hovered = hovered;
        }

        private void OnRowPressed(int rowIndex)
        {
            SelectRow(rowIndex);
        }

        private void OnCandidatesPressed()
        {
            _page = 0;
            _session.ShowCandidates();
        }

        private void OnPreviousPagePressed()
        {
            ChangePage(-1);
        }

        private void OnNextPagePressed()
        {
            ChangePage(1);
        }

        private void OnBiasDownPressed()
        {
            _session.NudgeBias(-1);
        }

        private void OnBiasUpPressed()
        {
            _session.NudgeBias(1);
        }

        private void OnBiasDragged(float normalized)
        {
            _session.SetBias(Mathf.Lerp(_settings.MinBias, _settings.MaxBias, normalized));
        }

        private void OnAnisoPressed(int anisoLevel)
        {
            _session.SetAnisoLevel(anisoLevel);
        }

        private void OnFilterPressed(FilterMode filterMode)
        {
            _session.SetFilterMode(filterMode);
        }

        private void OnComparePressed()
        {
            _session.ToggleCompareMode();
        }

        private void OnKeepPressed()
        {
            _session.KeepSelected();
        }

        private void OnResetPressed()
        {
            _session.ResetSelected();
        }

        private void OnExportPressed()
        {
            _session.TryExport();
        }

        private void OnResetAllPressed()
        {
            _session.ResetAll();
        }

        private void SelectRow(int rowIndex)
        {
            int index = _page * RowCount + rowIndex;
            if (_session.ListMode == TunerListMode.Candidates)
            {
                _page = 0;
                _session.SelectCandidate(index);
                return;
            }

            _session.SelectTexture(index);
        }

        private void ChangePage(int delta)
        {
            int pageCount = GetPageCount();
            _page = Mathf.Clamp(_page + delta, 0, pageCount - 1);
            Refresh();
        }

        private int GetItemCount()
        {
            bool isCandidates = _session.ListMode == TunerListMode.Candidates;
            return isCandidates ? _session.Candidates.Count : _session.SelectedRendererTargets.Count;
        }

        private int GetPageCount()
        {
            return Mathf.Max(1, Mathf.CeilToInt(GetItemCount() / (float)RowCount));
        }

        private void RefreshStatus()
        {
            bool isAdjusted = _session.CompareMode == CompareMode.Adjusted;
            _statusLabel.SetText(isAdjusted ? "SHOWING: ADJUSTED" : "SHOWING: ORIGINAL");
            _statusLabel.SetTextColor(isAdjusted ? Color.white : WarningColor);
            _compareButton.SetText(isAdjusted ? "Show ORIGINAL" : "Show ADJUSTED");
            _compareButton.SetColor(isAdjusted ? ButtonColor : ActiveColor);
            _exportButton.SetText($"Export kept ({_session.KeptCount})");
            _exportMessageLabel.SetText(ShortenTo(_session.LastExportMessage, MaxMessageLength));
        }

        private void RefreshList()
        {
            bool isCandidates = _session.ListMode == TunerListMode.Candidates;
            _page = Mathf.Clamp(_page, 0, GetPageCount() - 1);
            _listHeader.SetText(BuildListHeader(isCandidates));
            _pageLabel.SetText($"{_page + 1}/{GetPageCount()}");

            int itemCount = GetItemCount();
            for (int i = 0; i < RowCount; i++)
            {
                int index = _page * RowCount + i;
                PanelWidget row = _rows[i];
                bool hasItem = index < itemCount;
                row.SetVisible(hasItem);
                if (!hasItem)
                {
                    continue;
                }

                if (isCandidates)
                {
                    RefreshCandidateRow(row, index);
                }
                else
                {
                    RefreshTextureRow(row, index);
                }
            }
        }

        private string BuildListHeader(bool isCandidates)
        {
            if (!isCandidates)
            {
                string rendererName = _session.SelectedRenderer == null ? "-" : _session.SelectedRenderer.name;
                return ShortenTo($"Textures of: {rendererName}", MaxRowLength);
            }

            if (_session.Candidates.Count == 0)
            {
                return $"Aim at an object, pull the trigger ({_session.RendererCount} found)";
            }

            return $"Candidates, nearest first ({_session.Candidates.Count})";
        }

        private void RefreshCandidateRow(PanelWidget row, int index)
        {
            SelectionCandidate candidate = _session.Candidates[index];
            bool isAlive = candidate.Renderer != null;
            string name = isAlive ? Shorten(candidate.Renderer.name) : "(destroyed)";
            string inside = candidate.IsOriginInside ? " (inside)" : string.Empty;
            FormattableString text = $"{index + 1}. {name}  {candidate.Distance:0.0} m{inside}";
            row.SetText(ShortenTo(FormattableString.Invariant(text), MaxRowLength));
            row.SetColor(isAlive && candidate.Renderer == _session.SelectedRenderer ? ActiveColor : ButtonColor);
        }

        private void RefreshTextureRow(PanelWidget row, int index)
        {
            TextureTarget target = _session.SelectedRendererTargets[index];
            string propertyNames = BuildPropertyNames(target, _session.SelectedRenderer);
            string textureName = target.Texture == null ? "(destroyed)" : Shorten(target.Texture.name);
            string unsupported = target.IsAdjustable ? string.Empty : " unsupported";
            row.SetText(ShortenTo($"{textureName}  ({propertyNames}){unsupported}", MaxRowLength));

            bool isSelected = target == _session.SelectedTarget;
            Color color = target.IsAdjustable ? ButtonColor : DisabledColor;
            row.SetColor(isSelected ? ActiveColor : color);
        }

        /// <summary>
        /// 選択中の Renderer でこのテクスチャが入っているプロパティ名を並べる。
        /// シェーダーが実際には使っていないプロパティ（互換用の _MainTex など）も含まれる。
        /// </summary>
        private string BuildPropertyNames(TextureTarget target, Renderer selectedRenderer)
        {
            _text.Clear();
            foreach (TextureUsage usage in target.Usages)
            {
                bool isSameRenderer = usage.Renderer == selectedRenderer;
                if (!isSameRenderer || _text.ToString().Contains(usage.PropertyName))
                {
                    continue;
                }

                if (_text.Length > 0)
                {
                    _text.Append(", ");
                }

                _text.Append(usage.PropertyName);
            }

            return _text.ToString();
        }

        private void RefreshTarget()
        {
            bool hasSettings = _session.TryGetSelectedSettings(
                out TextureSamplerSettings original,
                out TextureSamplerSettings adjusted,
                out bool isKept);

            _infoLabel.SetText(BuildInfoText(hasSettings, original, adjusted, isKept));
            _warningLabel.SetText(BuildWarningText(hasSettings, adjusted));
            _keepButton.SetColor(isKept ? ActiveColor : ButtonColor);
            _keepButton.SetText(isKept ? "Kept" : "Keep");

            FormattableString biasText = $"Bias {adjusted.MipMapBias:+0.00;-0.00;0.00}";
            _biasLabel.SetText(hasSettings ? FormattableString.Invariant(biasText) : "Bias -");
            float normalized = Mathf.InverseLerp(_settings.MinBias, _settings.MaxBias, adjusted.MipMapBias);
            Rect track = _biasTrack.Rect;
            _biasHandle.SetCenterX(Mathf.Lerp(track.xMin, track.xMax, hasSettings ? normalized : 0f));
            _biasHandle.SetVisible(hasSettings);

            for (int i = 0; i < AnisoChoices.Length; i++)
            {
                bool isCurrent = hasSettings && adjusted.AnisoLevel == AnisoChoices[i];
                _anisoButtons[i].SetColor(isCurrent ? ActiveColor : ButtonColor);
            }

            for (int i = 0; i < FilterChoices.Length; i++)
            {
                bool isCurrent = hasSettings && adjusted.FilterMode == FilterChoices[i];
                _filterButtons[i].SetColor(isCurrent ? ActiveColor : ButtonColor);
            }
        }

        private string BuildInfoText(bool hasSettings, TextureSamplerSettings original, TextureSamplerSettings adjusted, bool isKept)
        {
            TextureTarget target = _session.SelectedTarget;
            if (target == null || target.Texture == null)
            {
                return "No texture selected.\n1. Aim at a wall or floor and pull the trigger.\n2. Pick the object, then pick a texture.";
            }

            Texture texture = target.Texture;
            _text.Clear();
            _text.AppendLine(Shorten(texture.name));
            _text.AppendLine(BuildAssetLine());
            _text.AppendLine($"{texture.width}x{texture.height}  {texture.graphicsFormat}  mips: {texture.mipmapCount}");
            _text.AppendLine($"Used by {target.RendererCount} renderer(s), {target.MaterialCount} material(s). All are affected.");
            if (hasSettings)
            {
                _text.AppendLine($"Original: {original}");
                _text.Append($"Adjusted: {adjusted}{(isKept ? "   [KEPT]" : string.Empty)}   Kept in total: {_session.KeptCount}");
            }

            return _text.ToString();
        }

        /// <summary>
        /// 選択中のテクスチャの元アセットのパス、または持ち帰れない理由を1行で返す。
        /// </summary>
        private string BuildAssetLine()
        {
            TextureExportSupport support = _session.GetSelectedExportSupport(out TextureManifestEntry entry);
            if (support != TextureExportSupport.Exportable || entry == null)
            {
                return $"Asset: not exportable ({support})";
            }

            // パスが長い場合は、ファイル名が見えるように末尾を残す。
            string path = entry.PathHint ?? string.Empty;
            if (path.Length > MaxPathLength)
            {
                path = "..." + path.Substring(path.Length - (MaxPathLength - 3));
            }

            return $"Asset: {path}";
        }

        private string BuildWarningText(bool hasSettings, TextureSamplerSettings adjusted)
        {
            TextureTarget target = _session.SelectedTarget;
            if (target == null || target.Texture == null)
            {
                return string.Empty;
            }

            if (!target.IsAdjustable)
            {
                return "Unsupported texture type. Only Texture2D can be adjusted.";
            }

            _text.Clear();
            if (!target.HasMipmaps)
            {
                _text.AppendLine("No mipmaps: bias has no visible effect.");
            }

            if (hasSettings && adjusted.MipMapBias < RecommendedMinBias - 0.001f)
            {
                _text.AppendLine("Bias below -0.5: may shimmer and cost more GPU time.");
            }

            if (TunerDiagnostics.IsAnisoOverriddenByQuality)
            {
                _text.AppendLine($"Quality setting ({QualitySettings.anisotropicFiltering}) overrides per-texture aniso.");
            }

            if (_session.GetSelectedExportSupport(out _) != TextureExportSupport.Exportable)
            {
                _text.AppendLine($"Cannot be exported ({_session.GetSelectedExportSupport(out _)}). You can still try values here.");
            }

            if (target.HasPropertyBlockUsage)
            {
                _text.AppendLine("Renderer has a MaterialPropertyBlock: drawn texture may differ.");
            }

            return _text.ToString();
        }

        private static string Shorten(string text)
        {
            return ShortenTo(text, MaxNameLength);
        }

        /// <summary>
        /// 表示の枠からはみ出さないよう、長い文字列を切り詰める。
        /// </summary>
        private static string ShortenTo(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}
