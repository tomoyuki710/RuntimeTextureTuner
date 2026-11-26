namespace RuntimeTextureTuner
{
    /// <summary>
    /// PoC で使うコントローラーのボタン。
    /// </summary>
    public enum PocButton
    {
        /// <summary>
        /// 右手 A ボタン: Bias のプリセットを進める。
        /// </summary>
        RightPrimary,

        /// <summary>
        /// 右手 B ボタン: Aniso のプリセットを進める。
        /// </summary>
        RightSecondary,

        /// <summary>
        /// 左手 X ボタン: Filter のプリセットを進める。
        /// </summary>
        LeftPrimary,

        /// <summary>
        /// 左手 Y ボタン: Original と Adjusted を切り替える。
        /// </summary>
        LeftSecondary,

        /// <summary>
        /// 右手トリガー: 表示パネルを出す、または隠す。
        /// </summary>
        RightTrigger,

        /// <summary>
        /// 左手グリップ: すべて元の値に戻す。
        /// </summary>
        LeftGrip,
    }
}
