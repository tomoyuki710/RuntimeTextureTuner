using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// Phase 1 の実機検証用。PoC シーン内の全テクスチャに対して、コントローラーのボタンで
    /// Bias / Aniso / Filter のプリセットを切り替える。対象の選択や保存は行わない。
    /// </summary>
    public sealed class PocTuner : MonoBehaviour
    {
        private const float HudRefreshIntervalSeconds = 1f;
        private const float TrackingWarmUpSeconds = 1f;
        private const string LogPrefix = "[RuntimeTextureTuner]";

        // null は「元の値のまま」を表す。まず効果の分かりやすい正の Bias から確認する。
        // -0.7 は Unity 公式の推奨範囲（-0.5 まで）より強い値。ちらつきと負荷の確認用に含める。
        private static readonly float?[] BiasPresets = { null, 1f, 2f, 0f, -0.25f, -0.5f, -0.7f };
        private static readonly int?[] AnisoPresets = { null, 1, 4, 8, 16 };
        private static readonly FilterMode?[] FilterPresets = { null, FilterMode.Point, FilterMode.Bilinear, FilterMode.Trilinear };

        private readonly SamplerOverrideService _service = new SamplerOverrideService();
        private readonly List<Texture> _textures = new List<Texture>();
        private readonly StringBuilder _hudText = new StringBuilder(1024);

        private Transform _targetRoot;
        private PocButtonReader _buttonReader;
        private PocHud _hud;
        private int _biasIndex;
        private int _anisoIndex;
        private int _filterIndex;
        private float _nextHudRefreshTime;

        /// <summary>
        /// 調整対象の Renderer をまとめたルートを指定する。Start より前に呼ぶこと。
        /// </summary>
        public void Initialize(Transform targetRoot)
        {
            _targetRoot = targetRoot;
        }

        /// <summary>
        /// ボタンが押されたものとして処理する。コントローラーなしでの動作確認に使う。
        /// </summary>
        public void SimulateButton(PocButton button)
        {
            HandleButton(button);
        }

        private void Start()
        {
            if (_targetRoot == null)
            {
                Debug.LogError($"{LogPrefix} Target root is not assigned.");
                enabled = false;
                return;
            }

            CollectTextures();

            _buttonReader = new PocButtonReader();
            _buttonReader.OnPressed += OnButtonPressed;

            _hud = PocHud.Create();
            _hud.Hide();
            StartCoroutine(ShowHudAfterTrackingStarts());
            LogState("Started");
        }

        private void Update()
        {
            _buttonReader.Poll();
            RefreshHudPeriodically();
        }

        private void OnDestroy()
        {
            if (_buttonReader != null)
            {
                _buttonReader.OnPressed -= OnButtonPressed;
            }

            // Editor ではテクスチャアセットのインスタンスが残るため、必ず元の値に戻す。
            _service.ResetAll();
        }

        private void OnButtonPressed(PocButton button)
        {
            HandleButton(button);
        }

        private void HandleButton(PocButton button)
        {
            switch (button)
            {
                case PocButton.RightPrimary:
                    _biasIndex = (_biasIndex + 1) % BiasPresets.Length;
                    ApplyPresets();
                    break;
                case PocButton.RightSecondary:
                    _anisoIndex = (_anisoIndex + 1) % AnisoPresets.Length;
                    ApplyPresets();
                    break;
                case PocButton.LeftPrimary:
                    _filterIndex = (_filterIndex + 1) % FilterPresets.Length;
                    ApplyPresets();
                    break;
                case PocButton.LeftSecondary:
                    ToggleCompareMode();
                    break;
                case PocButton.RightTrigger:
                    ToggleHud();
                    break;
                case PocButton.LeftGrip:
                    ResetAll();
                    break;
            }

            RefreshHud();
            LogState(button.ToString());
        }

        private void CollectTextures()
        {
            var materials = new List<Material>();
            var propertyIds = new List<int>();
            foreach (Renderer targetRenderer in _targetRoot.GetComponentsInChildren<Renderer>(true))
            {
                // sharedMaterials を使い、マテリアルの暗黙の複製を避ける。
                targetRenderer.GetSharedMaterials(materials);
                foreach (Material material in materials)
                {
                    RegisterTexturesOf(material, propertyIds);
                }
            }

            _service.GetTextures(_textures);
        }

        private void RegisterTexturesOf(Material material, List<int> propertyIds)
        {
            if (material == null)
            {
                return;
            }

            material.GetTexturePropertyNameIDs(propertyIds);
            foreach (int propertyId in propertyIds)
            {
                if (material.GetTexture(propertyId) is Texture2D texture)
                {
                    _service.TryRegister(texture);
                }
            }
        }

        private void ApplyPresets()
        {
            foreach (Texture texture in _textures)
            {
                if (!_service.TryGetOriginal(texture, out TextureSamplerSettings original))
                {
                    continue;
                }

                var adjusted = new TextureSamplerSettings(
                    BiasPresets[_biasIndex] ?? original.MipMapBias,
                    AnisoPresets[_anisoIndex] ?? original.AnisoLevel,
                    FilterPresets[_filterIndex] ?? original.FilterMode);
                _service.TrySetAdjusted(texture, adjusted);
            }
        }

        private void ToggleCompareMode()
        {
            bool isAdjusted = _service.CompareMode == CompareMode.Adjusted;
            _service.SetCompareMode(isAdjusted ? CompareMode.Original : CompareMode.Adjusted);
        }

        private void ToggleHud()
        {
            if (_hud.IsVisible)
            {
                _hud.Hide();
                return;
            }

            ShowHud();
        }

        private void ShowHud()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _hud.ShowInFrontOf(mainCamera.transform);
            }
        }

        private void ResetAll()
        {
            _biasIndex = 0;
            _anisoIndex = 0;
            _filterIndex = 0;
            _service.ResetAll();
        }

        private IEnumerator ShowHudAfterTrackingStarts()
        {
            // 起動直後は頭の姿勢がまだ届いていないことがあるため、少し待ってから配置する。
            yield return new WaitForSeconds(TrackingWarmUpSeconds);

            PocTrackingOrigin.Log();

            RefreshHud();
            ShowHud();
        }

        private void RefreshHudPeriodically()
        {
            if (Time.unscaledTime < _nextHudRefreshTime)
            {
                return;
            }

            RefreshHud();
        }

        private void RefreshHud()
        {
            _nextHudRefreshTime = Time.unscaledTime + HudRefreshIntervalSeconds;
            _hudText.Clear();
            AppendState(_hudText);
            _hudText.AppendLine();
            AppendTextures(_hudText);
            _hudText.AppendLine();
            PocDiagnostics.AppendTo(_hudText);
            _hud.SetText(_hudText.ToString());
        }

        private void AppendState(StringBuilder builder)
        {
            string mode = _service.CompareMode == CompareMode.Adjusted ? "ADJUSTED" : "ORIGINAL";
            builder.AppendLine($"SHOWING: {mode}   [Y] switch Original / Adjusted");
            builder.AppendLine($"[A] Bias:   {FormatBias(BiasPresets[_biasIndex])}");
            builder.AppendLine($"[B] Aniso:  {FormatPreset(AnisoPresets[_anisoIndex])}");
            builder.AppendLine($"[X] Filter: {FormatPreset(FilterPresets[_filterIndex])}");
            builder.AppendLine("[R Trigger] hide/show panel   [L Grip] reset all");
            builder.AppendLine("Mip colors: 0 red, 1 green, 2 blue, 3 yellow, 4 magenta, 5 cyan");
        }

        private void AppendTextures(StringBuilder builder)
        {
            builder.AppendLine("texture (mips): bias / aniso / filter, original -> read back");
            foreach (Texture texture in _textures)
            {
                bool isAlive = texture != null;
                if (!isAlive || !_service.TryGetOriginal(texture, out TextureSamplerSettings original))
                {
                    continue;
                }

                TextureSamplerSettings current = TextureSamplerSettings.Capture(texture);
                string shortName = texture.name.Replace("Poc_", string.Empty);
                string mipInfo = texture.mipmapCount > 1 ? texture.mipmapCount.ToString() : "NO MIPS";
                builder.AppendLine($"{shortName} ({mipInfo}): {FormatCompact(original)} -> {FormatCompact(current)}");
            }
        }

        private void LogState(string reason)
        {
            FormattableString message =
                $"{LogPrefix} {reason}: showing={_service.CompareMode} bias={FormatBias(BiasPresets[_biasIndex])} aniso={FormatPreset(AnisoPresets[_anisoIndex])} filter={FormatPreset(FilterPresets[_filterIndex])} textures={_textures.Count}";
            Debug.Log(FormattableString.Invariant(message));
        }

        private static string FormatCompact(TextureSamplerSettings settings)
        {
            FormattableString text = $"{settings.MipMapBias:+0.00;-0.00;0.00} / {settings.AnisoLevel} / {settings.FilterMode}";
            return FormattableString.Invariant(text);
        }

        private static string FormatBias(float? bias)
        {
            if (!bias.HasValue)
            {
                return "(original)";
            }

            FormattableString text = $"{bias.Value:+0.00;-0.00;0.00}";
            return FormattableString.Invariant(text);
        }

        private static string FormatPreset<T>(T? preset)
            where T : struct
        {
            return preset.HasValue ? preset.Value.ToString() : "(original)";
        }
    }
}
