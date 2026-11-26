namespace RuntimeTextureTuner
{
    /// <summary>
    /// セッションの1項目を、現在のプロジェクトに反映できるかどうかの判定。
    /// </summary>
    public enum SessionRowStatus
    {
        /// <summary>
        /// 反映できる。
        /// </summary>
        Ready,

        /// <summary>
        /// 反映する差分がない。既に同じ値になっている。
        /// </summary>
        Unchanged,

        /// <summary>
        /// ビルド後にソースファイルが変わっている。既定では反映しない。
        /// </summary>
        Stale,

        /// <summary>
        /// ビルド後に、反映対象の設定値が別の値に変えられている。既定では反映しない。
        /// </summary>
        Conflict,

        /// <summary>
        /// 対象外のアセット。Assets の外、TextureImporter なし、サブアセットなど。
        /// </summary>
        Unsupported,

        /// <summary>
        /// アセットが見つからない。削除された、または GUID が別のアセットを指している。
        /// </summary>
        Missing,

        /// <summary>
        /// 項目の内容が不正。
        /// </summary>
        Invalid,
    }
}
