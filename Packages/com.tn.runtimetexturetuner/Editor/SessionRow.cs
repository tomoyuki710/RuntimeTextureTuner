namespace RuntimeTextureTuner
{
    /// <summary>
    /// セッションの1項目と、現在のプロジェクトに対する判定結果。一覧の1行に当たる。
    /// </summary>
    public sealed class SessionRow
    {
        public SessionRow(TuningSessionEntry entry, SessionRowStatus status, string note, string assetPath, SamplerSettingsData current)
        {
            Entry = entry;
            Status = status;
            Note = note;
            AssetPath = assetPath;
            Current = current;
            IsSelected = status == SessionRowStatus.Ready;
        }

        public TuningSessionEntry Entry { get; }

        public SessionRowStatus Status { get; }

        /// <summary>
        /// 判定の理由や補足。
        /// </summary>
        public string Note { get; }

        /// <summary>
        /// 現在のアセットのパス。見つからない場合はセッションに記録されたパス。
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// 現在の TextureImporter の値。アセットが見つからない場合は既定値。
        /// </summary>
        public SamplerSettingsData Current { get; }

        /// <summary>
        /// 反映の対象として選ばれているかどうか。
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 指定した許可のもとで反映できる状態かどうか。
        /// </summary>
        public bool CanApply(bool allowStale, bool allowConflict)
        {
            switch (Status)
            {
                case SessionRowStatus.Ready:
                    return true;
                case SessionRowStatus.Stale:
                    return allowStale;
                case SessionRowStatus.Conflict:
                    return allowConflict;
                default:
                    return false;
            }
        }
    }
}
