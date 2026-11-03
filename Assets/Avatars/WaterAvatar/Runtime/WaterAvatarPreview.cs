using Styly.NetSync;
using Styly.XRRig;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace AncientFuture.WaterAvatar
{
    /// <summary>Optional test-scene controls; never included in the network avatar prefabs.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WaterAvatarPreview : MonoBehaviour
    {
        public NetSyncAvatar previewAvatar;
        public StylyXrRig xrRig;
        public GameObject virtualEnvironment;
        public bool animatePreview = true;
        public bool mixedReality;
        public bool showDesktopControls = true;

        [Header("Automatic VR / MR Test")]
        [Tooltip("Alternate between VR and MR repeatedly while the test scene is running.")]
        public bool autoSwitchModes = true;
        [Min(1f), Tooltip("Real-time seconds in each mode. Manual switches restart this interval.")]
        public float modeSwitchInterval = 10f;

        private bool appliedMode;
        private bool previousModeButton;
        private bool previousDemoButton;
        private double modeStartedAt;

        private void OnEnable()
        {
            modeStartedAt = Time.realtimeSinceStartupAsDouble;
        }

        private void Start()
        {
            ApplyMode();
        }

        private void Update()
        {
            if (mixedReality != appliedMode) ApplyMode();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SetVirtualReality();
                if (keyboard.digit2Key.wasPressedThisFrame) SetMixedReality();
                if (keyboard.digit3Key.wasPressedThisFrame) SetPreviewVisible(previewAvatar != null && !previewAvatar.gameObject.activeSelf);
            }
            UnityEngine.XR.InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            UnityEngine.XR.InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool modeButton);
            leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool demoButton);
            if (modeButton && !previousModeButton)
            {
                if (mixedReality) SetVirtualReality();
                else SetMixedReality();
            }
            if (demoButton && !previousDemoButton) SetPreviewVisible(previewAvatar != null && !previewAvatar.gameObject.activeSelf);
            previousModeButton = modeButton;
            previousDemoButton = demoButton;
            if (autoSwitchModes)
            {
                if (Time.realtimeSinceStartupAsDouble - modeStartedAt >= Mathf.Max(1f, modeSwitchInterval))
                {
                    mixedReality = !mixedReality;
                    ApplyMode();
                }
            }
            else modeStartedAt = Time.realtimeSinceStartupAsDouble;
            if (!animatePreview || previewAvatar == null || !previewAvatar.gameObject.activeInHierarchy) return;
            float t = Time.time;
            if (previewAvatar._head != null)
            {
                previewAvatar._head.localPosition = WaterAvatarRig.RestHead + new Vector3(0.018f * Mathf.Sin(t * 0.7f), 0.018f * Mathf.Sin(t * 0.9f), 0f);
                previewAvatar._head.localRotation = Quaternion.Euler(3f * Mathf.Sin(t * 0.6f), 14f * Mathf.Sin(t * 0.36f), 3f * Mathf.Sin(t * 0.47f));
            }
            AnimateHand(previewAvatar._leftHand, -1f, t);
            AnimateHand(previewAvatar._rightHand, 1f, t + 1.7f);
        }

        private static void AnimateHand(Transform hand, float side, float t)
        {
            if (hand == null) return;
            hand.localPosition = new Vector3(side * (0.43f + 0.055f * Mathf.Sin(t * 0.55f)),
                1.08f + 0.09f * Mathf.Sin(t * 0.67f), 0.09f + 0.09f * Mathf.Sin(t * 0.43f));
            hand.localRotation = Quaternion.Euler(72f + 14f * Mathf.Sin(t * 0.57f), side * 8f, -side * (15f + 12f * Mathf.Sin(t * 0.5f)));
        }

        [ContextMenu("Preview / Switch to VR")]
        public void SetVirtualReality() { mixedReality = false; ApplyMode(); }

        [ContextMenu("Preview / Switch to MR")]
        public void SetMixedReality() { mixedReality = true; ApplyMode(); }

        public void SetPreviewVisible(bool visible)
        {
            if (previewAvatar != null) previewAvatar.gameObject.SetActive(visible);
        }

        private void ApplyMode()
        {
            appliedMode = mixedReality;
            modeStartedAt = Time.realtimeSinceStartupAsDouble;
            if (virtualEnvironment != null) virtualEnvironment.SetActive(!mixedReality);
            if (!Application.isPlaying) return;
            if (xrRig != null)
            {
                if (mixedReality) xrRig.SwitchToMR(0f);
                else xrRig.SwitchToVR(0f);
            }
            // Desktop has no camera passthrough. Transparent black is deliberate in MR.
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = mixedReality ? Color.clear : new Color(0.008f, 0.04f, 0.065f, 1f);
            }
        }

        private void OnGUI()
        {
            if (!showDesktopControls || XRSettings.isDeviceActive) return;
            GUI.color = new Color(0.70f, 0.92f, 0.98f);
            GUI.Label(new Rect(24, 20, 450, 26), "W A T E R   /   STYLY NETSYNC");
            GUI.Label(new Rect(24, 44, 450, 24), "UPPER-BODY AVATAR  ·  Head + hands tracking");
            GUI.color = Color.white;
            if (GUI.Button(new Rect(24, Screen.height - 62, 120, 30), "1   Underwater VR")) SetVirtualReality();
            if (GUI.Button(new Rect(152, Screen.height - 62, 120, 30), "2   Passthrough MR")) SetMixedReality();
            if (GUI.Button(new Rect(280, Screen.height - 62, 120, 30), "3   Show / hide demo")) SetPreviewVisible(previewAvatar != null && !previewAvatar.gameObject.activeSelf);
            GUI.Label(new Rect(24, Screen.height - 28, 700, 24), mixedReality
                ? "MR · Real passthrough requires the headset. The desktop background is transparent black."
                : "VR · Offline demo. Headset: right primary button switches VR/MR; left primary button toggles the demo.");
        }
    }
}
