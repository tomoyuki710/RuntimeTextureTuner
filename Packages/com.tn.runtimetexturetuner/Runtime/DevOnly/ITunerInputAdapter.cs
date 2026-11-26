using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 調整機能が必要とする入力。入力ライブラリへの依存をここに隔離する。
    /// 独自の入力の仕組みを持つコンテンツでは、この実装を差し替える。
    /// </summary>
    public interface ITunerInputAdapter
    {
        /// <summary>
        /// パネルを開く、または隠す操作のボタンがすべて押されているかどうか。
        /// </summary>
        bool IsOpenChordHeld { get; }

        /// <summary>
        /// 決定ボタンが押されているかどうか。
        /// </summary>
        bool IsSelectHeld { get; }

        /// <summary>
        /// 毎フレーム1回、他の値を読む前に呼ぶ。
        /// </summary>
        void Poll();

        /// <summary>
        /// 操作用のレイをワールド座標で取得する。取得できない場合は false を返す。
        /// </summary>
        bool TryGetPointerRay(out Ray ray);

        /// <summary>
        /// 頭の位置と向きをワールド座標で取得する。取得できない場合は false を返す。
        /// </summary>
        bool TryGetHeadPose(out Pose pose);
    }
}
