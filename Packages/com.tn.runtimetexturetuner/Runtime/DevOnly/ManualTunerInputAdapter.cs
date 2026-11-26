using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 値を外から設定する入力アダプター。コントローラーなしでの動作確認や、独自の入力から値を流し込む用途に使う。
    /// </summary>
    public sealed class ManualTunerInputAdapter : ITunerInputAdapter
    {
        public bool IsOpenChordHeld { get; set; }

        public bool IsSelectHeld { get; set; }

        /// <summary>
        /// 操作用のレイ。null の場合はレイを取得できない扱いになる。
        /// </summary>
        public Ray? PointerRay { get; set; }

        /// <summary>
        /// 頭の姿勢。null の場合はメインカメラの姿勢を使う。
        /// </summary>
        public Pose? HeadPose { get; set; }

        public void Poll()
        {
        }

        public bool TryGetPointerRay(out Ray ray)
        {
            ray = PointerRay.GetValueOrDefault();
            return PointerRay.HasValue;
        }

        public bool TryGetHeadPose(out Pose pose)
        {
            if (HeadPose.HasValue)
            {
                pose = HeadPose.Value;
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(mainCamera.transform.position, mainCamera.transform.rotation);
            return true;
        }
    }
}
