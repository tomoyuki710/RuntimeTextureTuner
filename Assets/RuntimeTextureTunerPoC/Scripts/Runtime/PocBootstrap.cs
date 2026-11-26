using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// Phase 1 の実機検証用。PoC シーンが読み込まれたときだけ調整機能を起動する。
    /// シーンには調整用のコンポーネントを保存せず、実行時に生成する。
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
            StartTuner();
        }

        private static void StartTuner()
        {
            GameObject targetRoot = GameObject.Find(TargetRootName);
            if (targetRoot == null)
            {
                return;
            }

            var tunerObject = new GameObject(nameof(PocTuner));
            PocTuner tuner = tunerObject.AddComponent<PocTuner>();
            tuner.Initialize(targetRoot.transform);
        }
    }
}
