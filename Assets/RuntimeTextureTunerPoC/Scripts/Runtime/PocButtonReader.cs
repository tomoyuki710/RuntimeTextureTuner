using System;
using UnityEngine.XR;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// XR コントローラーのボタンを読み、押された瞬間を通知する。
    /// </summary>
    internal sealed class PocButtonReader
    {
        private readonly bool[] _wasPressed = new bool[Enum.GetValues(typeof(PocButton)).Length];

        public event Action<PocButton> OnPressed;

        public void Poll()
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            Check(PocButton.RightPrimary, rightHand, CommonUsages.primaryButton);
            Check(PocButton.RightSecondary, rightHand, CommonUsages.secondaryButton);
            Check(PocButton.RightTrigger, rightHand, CommonUsages.triggerButton);
            Check(PocButton.LeftPrimary, leftHand, CommonUsages.primaryButton);
            Check(PocButton.LeftSecondary, leftHand, CommonUsages.secondaryButton);
            Check(PocButton.LeftGrip, leftHand, CommonUsages.gripButton);
        }

        private void Check(PocButton button, InputDevice device, InputFeatureUsage<bool> usage)
        {
            // 無効なデバイスでは TryGetFeatureValue が false を返す。
            bool hasValue = device.TryGetFeatureValue(usage, out bool value);
            bool isPressed = hasValue && value;
            bool wasPressed = _wasPressed[(int)button];
            _wasPressed[(int)button] = isPressed;

            if (isPressed && !wasPressed)
            {
                OnPressed?.Invoke(button);
            }
        }
    }
}
