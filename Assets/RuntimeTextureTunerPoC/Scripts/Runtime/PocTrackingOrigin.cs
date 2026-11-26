using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// トラッキング原点の状態をログに出す。原点の設定はシーンの XR Origin が行う。
    /// </summary>
    internal static class PocTrackingOrigin
    {
        public static void Log()
        {
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count == 0)
            {
                Debug.Log("[RuntimeTextureTuner] No XR input subsystem.");
                return;
            }

            Debug.Log($"[RuntimeTextureTuner] Tracking origin: {subsystems[0].GetTrackingOriginMode()}");
        }
    }
}
