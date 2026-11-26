using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// UnityEngine.XR の InputDevices からコントローラーを読む既定の入力アダプター。
    /// Input System、XR Interaction Toolkit、各社の SDK に依存せず、既存の入力アクションにも触れない。
    /// </summary>
    public sealed class XrDeviceInputAdapter : ITunerInputAdapter
    {
        private static readonly InputFeatureUsage<Vector3> PointerPosition = new InputFeatureUsage<Vector3>("PointerPosition");
        private static readonly InputFeatureUsage<Quaternion> PointerRotation = new InputFeatureUsage<Quaternion>("PointerRotation");

        private readonly TunerSettings _settings;
        private InputDevice _leftHand;
        private InputDevice _rightHand;
        private Transform _head;

        public XrDeviceInputAdapter(TunerSettings settings)
        {
            _settings = settings;
        }

        public bool IsOpenChordHeld
        {
            get
            {
                IReadOnlyList<TunerButton> chord = _settings.OpenChord;
                if (chord.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < chord.Count; i++)
                {
                    if (!IsPressed(chord[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsSelectHeld => IsPressed(_settings.SelectButton);

        public void Poll()
        {
            _leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            Camera mainCamera = Camera.main;
            _head = mainCamera == null ? null : mainCamera.transform;
        }

        public bool TryGetPointerRay(out Ray ray)
        {
            InputDevice device = _settings.PointerHand == TunerHand.Left ? _leftHand : _rightHand;

            // 狙いを付けるための姿勢を優先し、なければ握りの姿勢を使う。
            bool hasPose = TryGetPose(device, PointerPosition, PointerRotation, out Pose pose);
            if (!hasPose && !TryGetPose(device, CommonUsages.devicePosition, CommonUsages.deviceRotation, out pose))
            {
                ray = default;
                return false;
            }

            // デバイスの姿勢はトラッキング空間の値なので、カメラの親を基準にワールド座標へ変換する。
            Transform trackingSpace = _head == null ? null : _head.parent;
            if (trackingSpace != null)
            {
                pose = new Pose(trackingSpace.TransformPoint(pose.position), trackingSpace.rotation * pose.rotation);
            }

            ray = new Ray(pose.position, pose.rotation * Vector3.forward);
            return true;
        }

        public bool TryGetHeadPose(out Pose pose)
        {
            if (_head == null)
            {
                pose = Pose.identity;
                return false;
            }

            pose = new Pose(_head.position, _head.rotation);
            return true;
        }

        private static bool TryGetPose(
            InputDevice device,
            InputFeatureUsage<Vector3> positionUsage,
            InputFeatureUsage<Quaternion> rotationUsage,
            out Pose pose)
        {
            // 無効なデバイスでは TryGetFeatureValue が false を返す。
            bool hasPosition = device.TryGetFeatureValue(positionUsage, out Vector3 position);
            bool hasRotation = device.TryGetFeatureValue(rotationUsage, out Quaternion rotation);
            pose = new Pose(position, rotation);
            return hasPosition && hasRotation;
        }

        private bool IsPressed(TunerButton button)
        {
            bool isLeft = button <= TunerButton.LeftMenu;
            InputDevice device = isLeft ? _leftHand : _rightHand;
            bool hasValue = device.TryGetFeatureValue(GetUsage(button), out bool value);
            return hasValue && value;
        }

        private static InputFeatureUsage<bool> GetUsage(TunerButton button)
        {
            switch (button)
            {
                case TunerButton.LeftPrimary:
                case TunerButton.RightPrimary:
                    return CommonUsages.primaryButton;
                case TunerButton.LeftSecondary:
                case TunerButton.RightSecondary:
                    return CommonUsages.secondaryButton;
                case TunerButton.LeftTrigger:
                case TunerButton.RightTrigger:
                    return CommonUsages.triggerButton;
                case TunerButton.LeftGrip:
                case TunerButton.RightGrip:
                    return CommonUsages.gripButton;
                case TunerButton.LeftStickClick:
                case TunerButton.RightStickClick:
                    return CommonUsages.primary2DAxisClick;
                default:
                    return CommonUsages.menuButton;
            }
        }
    }
}
