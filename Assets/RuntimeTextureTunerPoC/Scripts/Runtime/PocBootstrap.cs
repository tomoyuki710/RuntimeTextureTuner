using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// PoC シーンが読み込まれたときに、サンプル用の準備をして調整機能を起動する。
    /// 調整用ビルドでは調整機能が自動で起動するため、ここでの起動は Editor での確認用。
    /// </summary>
    public static class PocBootstrap
    {
        /// <summary>
        /// 調整対象の Renderer をまとめたルートオブジェクトの名前。
        /// </summary>
        public const string TargetRootName = "RuntimeTextureTunerPoC";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            StartSample();
        }

        private static void StartSample()
        {
            GameObject targetRoot = GameObject.Find(TargetRootName);
            if (targetRoot == null)
            {
                return;
            }

            PocPropertyBlockSample.Apply(targetRoot.transform);
            RuntimeTuner.EnsureStarted();
        }
    }
}
