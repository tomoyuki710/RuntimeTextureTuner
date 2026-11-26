namespace RuntimeTextureTuner
{
    /// <summary>
    /// パネルの一覧に何を表示しているか。
    /// </summary>
    public enum TunerListMode
    {
        /// <summary>
        /// レイで指した方向にある Renderer の候補。
        /// </summary>
        Candidates,

        /// <summary>
        /// 選択した Renderer が使っているテクスチャ。
        /// </summary>
        Textures,
    }
}
