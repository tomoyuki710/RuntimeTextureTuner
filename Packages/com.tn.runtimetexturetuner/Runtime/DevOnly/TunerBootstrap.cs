#if RUNTIME_TEXTURE_TUNER_ENABLED
using UnityEngine;
#endif

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整用ビルド（RUNTIME_TEXTURE_TUNER_ENABLED を付けたビルド）で、調整機能を自動で起動する。
    /// シンボルがないプレイヤービルドには、このアセンブリ自体が含まれない。
    /// Editor ではシンボルがなくてもコンパイルされるが、自動では起動しない。
    /// </summary>
    internal static class TunerBootstrap
    {
#if RUNTIME_TEXTURE_TUNER_ENABLED
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            RuntimeTuner.EnsureStarted();
        }
#endif
    }
}
