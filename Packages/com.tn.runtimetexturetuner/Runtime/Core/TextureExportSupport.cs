namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整結果を Editor へ持ち帰れるかどうかと、持ち帰れない場合の理由。
    /// 持ち帰れないテクスチャも、実機で値を変えて見え方を試すことはできる。
    /// </summary>
    public enum TextureExportSupport
    {
        /// <summary>
        /// 持ち帰れる。Assets 配下にあり、TextureImporter を持つ独立したテクスチャアセット。
        /// </summary>
        Exportable,

        /// <summary>
        /// ビルド時の対応表に載っていない。動的に読み込まれたもの、実行時に差し替えられたものなど。
        /// </summary>
        NotInRegistry,

        /// <summary>
        /// アセットではない。実行時に生成されたテクスチャなど。
        /// </summary>
        NotAnAsset,

        /// <summary>
        /// Assets フォルダーの外にある。Packages、組み込みリソースなど。
        /// </summary>
        OutsideAssetsFolder,

        /// <summary>
        /// TextureImporter を持たない。.asset として保存されたテクスチャなど。
        /// </summary>
        NoTextureImporter,

        /// <summary>
        /// 他のアセットに埋め込まれたサブアセット。FBX 内のテクスチャ、Sprite Atlas など。
        /// </summary>
        SubAsset,
    }
}
