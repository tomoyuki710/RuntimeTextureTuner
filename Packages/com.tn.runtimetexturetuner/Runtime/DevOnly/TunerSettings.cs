using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整機能の設定。ボタンの使われ方はコンテンツごとに違うため、割り当てをここで変えられるようにする。
    /// Resources に <see cref="ResourcesName"/> の名前で置くと読み込まれる。なければ既定値を使う。
    /// </summary>
    [CreateAssetMenu(menuName = "Runtime Texture Tuner/Settings", fileName = ResourcesName)]
    public sealed class TunerSettings : ScriptableObject
    {
        public const string ResourcesName = "RuntimeTextureTunerSettings";

        [Header("Open / Hide")]
        [SerializeField, Tooltip("同時に長押しするとパネルを開く（開いているときは隠す）ボタンの組み合わせ")]
        private TunerButton[] openChord = { TunerButton.LeftStickClick, TunerButton.RightStickClick };

        [SerializeField, Tooltip("長押しと判定するまでの秒数")]
        private float openHoldSeconds = 1f;

        [Header("Pointer")]
        [SerializeField, Tooltip("レイを出す手")]
        private TunerHand pointerHand = TunerHand.Right;

        [SerializeField, Tooltip("パネルの操作と対象の選択に使う決定ボタン")]
        private TunerButton selectButton = TunerButton.RightTrigger;

        [SerializeField, Tooltip("対象を選択できる最大距離（メートル）")]
        private float maxSelectionDistance = 100f;

        [Header("Bias")]
        [SerializeField, Tooltip("Bias の下限。-0.5 より強い負の値は負荷とちらつきが増えることがある")]
        private float minBias = -1f;

        [SerializeField, Tooltip("Bias の上限")]
        private float maxBias = 2f;

        [SerializeField, Tooltip("ボタン1回あたりの Bias の変化量")]
        private float biasStep = 0.05f;

        [Header("Panel")]
        [SerializeField, Tooltip("パネルを開く位置までの、頭からの距離（メートル）")]
        private float panelDistance = 1.2f;

        public IReadOnlyList<TunerButton> OpenChord => openChord;

        public float OpenHoldSeconds => openHoldSeconds;

        public TunerHand PointerHand => pointerHand;

        public TunerButton SelectButton => selectButton;

        public float MaxSelectionDistance => maxSelectionDistance;

        public float MinBias => minBias;

        public float MaxBias => maxBias;

        public float BiasStep => biasStep;

        public float PanelDistance => panelDistance;

        /// <summary>
        /// Resources から設定を読み込む。なければ既定値の設定を作る。
        /// </summary>
        public static TunerSettings LoadOrCreateDefault()
        {
            TunerSettings loaded = Resources.Load<TunerSettings>(ResourcesName);
            return loaded != null ? loaded : CreateInstance<TunerSettings>();
        }
    }
}
