using Styly.NetSync;
using UnityEngine;

namespace AncientFuture.WaterAvatar
{
    /// <summary>Builds a floating upper-body pose from NetSync's three tracked anchors.</summary>
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class WaterAvatarRig : MonoBehaviour
    {
        public NetSyncAvatar avatar;
        public Transform torso;
        public Transform headBone;
        public Transform leftUpperArm;
        public Transform leftForearm;
        public Transform leftPalm;
        public Transform rightUpperArm;
        public Transform rightForearm;
        public Transform rightPalm;
        public Renderer upperBody;
        public Renderer leftHandRenderer;
        public Renderer rightHandRenderer;
        public ParticleSystem droplets;

        [Tooltip("Hide the head and torso for the wearer, while keeping their water hands visible.")]
        public bool handsOnlyForLocalPlayer = true;
        [Range(1f, 20f)] public float bodyTurnSpeed = 8f;
        [Tooltip("Controller and normalized hand poses use forward as the finger direction.")]
        public Vector3 palmRotationOffset;

        public static readonly Vector3 RestHead = new Vector3(0f, 1.70f, 0f);
        public static readonly Vector3 RestTorso = new Vector3(0f, 1.25f, -0.015f);
        public static readonly Vector3 RestShoulder = new Vector3(0.19f, 1.42f, 0f);
        public static readonly Vector3 RestElbow = new Vector3(0.36f, 1.17f, 0.015f);
        public static readonly Vector3 RestWrist = new Vector3(0.46f, 0.99f, 0.04f);

        private Quaternion bodyRotation;
        private bool initialized;
        private bool lastLocalVisibility;
        private bool leftTracking = true;
        private bool rightTracking = true;
        private WaterAvatarRemoteForm remoteForm;

        private void OnEnable()
        {
            initialized = false;
            leftTracking = true;
            rightTracking = true;
            remoteForm = GetComponent<WaterAvatarRemoteForm>();
            if (avatar == null) avatar = GetComponent<NetSyncAvatar>();
            if (avatar != null)
            {
                avatar.OnHandTrackingLost.AddListener(OnHandTrackingLost);
                avatar.OnHandTrackingRestored.AddListener(OnHandTrackingRestored);
            }
            Application.onBeforeRender += ApplyPoseBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyPoseBeforeRender;
            if (avatar != null)
            {
                avatar.OnHandTrackingLost.RemoveListener(OnHandTrackingLost);
                avatar.OnHandTrackingRestored.RemoveListener(OnHandTrackingRestored);
            }
        }

        private void OnHandTrackingLost(Hand hand)
        {
            if (hand == Hand.Left) leftTracking = false;
            else rightTracking = false;
        }

        private void OnHandTrackingRestored(Hand hand)
        {
            if (hand == Hand.Left) leftTracking = true;
            else rightTracking = true;
        }

        private void LateUpdate()
        {
            ApplyPose(Time.deltaTime);
        }

        // This callback is on Unity's main thread, after XR has refreshed its pose.
        private void ApplyPoseBeforeRender()
        {
            if (avatar != null && avatar.IsLocalAvatar) ApplyPose(0f);
        }

        public void ApplyPose(float deltaTime)
        {
            if (remoteForm != null && remoteForm.enabled && avatar != null && !avatar.IsLocalAvatar)
            {
                remoteForm.ApplyPose(this, deltaTime, leftTracking, rightTracking);
                return;
            }
            if (avatar == null || avatar._head == null || torso == null || headBone == null) return;

            Transform head = avatar._head;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(forward.normalized, Vector3.up);
                bodyRotation = initialized
                    ? Quaternion.Slerp(bodyRotation, target, 1f - Mathf.Exp(-bodyTurnSpeed * deltaTime))
                    : target;
            }
            else if (!initialized)
            {
                bodyRotation = transform.rotation;
            }

            torso.SetPositionAndRotation(head.position + bodyRotation * (RestTorso - RestHead), bodyRotation);
            headBone.SetPositionAndRotation(head.position, head.rotation);

            PoseArm(-1f, avatar._leftHand, leftUpperArm, leftForearm, leftPalm, leftHandRenderer);
            PoseArm(1f, avatar._rightHand, rightUpperArm, rightForearm, rightPalm, rightHandRenderer);

            bool local = handsOnlyForLocalPlayer && avatar.IsLocalAvatar;
            if (upperBody != null) upperBody.enabled = !local;
            if (droplets != null && (!initialized || local != lastLocalVisibility))
            {
                if (local) droplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else droplets.Play(true);
            }
            lastLocalVisibility = local;
            initialized = true;
        }

        private void PoseArm(float side, Transform target, Transform upper, Transform lower,
            Transform palm, Renderer handRenderer)
        {
            if (upper == null || lower == null || palm == null) return;
            Vector3 shoulderRest = Mirror(RestShoulder, side);
            Vector3 elbowRest = Mirror(RestElbow, side);
            Vector3 wristRest = Mirror(RestWrist, side);
            Vector3 shoulder = avatar._head.position + bodyRotation * (shoulderRest - RestHead);
            bool tracked = target != null && target.gameObject.activeInHierarchy && (side < 0 ? leftTracking : rightTracking);
            Vector3 wrist = tracked
                ? target.position
                : avatar._head.position + bodyRotation * (wristRest - RestHead);

            Vector3 delta = wrist - shoulder;
            float distance = delta.magnitude;
            Vector3 direction = distance > 0.001f ? delta / distance : bodyRotation * Vector3.down;
            float upperLength = Vector3.Distance(shoulderRest, elbowRest);
            float lowerLength = Vector3.Distance(elbowRest, wristRest);
            // A little stretch is appropriate for water and avoids a detached wrist at full reach.
            float stretch = Mathf.Max(1f, distance / (upperLength + lowerLength - 0.002f));
            float a = upperLength * stretch;
            float b = lowerLength * stretch;
            float d = Mathf.Clamp(distance, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
            float along = (a * a - b * b + d * d) / (2f * d);
            float bend = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 pole = bodyRotation * new Vector3(side * 0.65f, -0.5f, -0.7f);
            pole = Vector3.ProjectOnPlane(pole, direction);
            if (pole.sqrMagnitude < 0.001f) pole = Vector3.ProjectOnPlane(bodyRotation * Vector3.forward, direction);
            Vector3 elbow = shoulder + direction * along + pole.normalized * bend;

            SetSegment(upper, shoulder, elbow, upperLength, bodyRotation * Vector3.forward);
            SetSegment(lower, elbow, wrist, lowerLength, bodyRotation * Vector3.forward);
            Quaternion rotation = tracked ? target.rotation : bodyRotation * Quaternion.Euler(90f, 0f, -side * 15f);
            palm.SetPositionAndRotation(wrist, rotation * Quaternion.Euler(palmRotationOffset));
            if (handRenderer != null) handRenderer.enabled = tracked;
        }

        private static void SetSegment(Transform bone, Vector3 start, Vector3 end, float restLength, Vector3 forward)
        {
            Vector3 direction = end - start;
            bone.position = start;
            if (direction.sqrMagnitude > 0.000001f) bone.rotation = SegmentRotation(direction, forward);
            bone.localScale = new Vector3(1f, Mathf.Max(0.01f, direction.magnitude / restLength), 1f);
        }

        public static Quaternion SegmentRotation(Vector3 direction, Vector3 forward)
        {
            Vector3 up = direction.normalized;
            Vector3 facing = Vector3.ProjectOnPlane(forward, up);
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.ProjectOnPlane(Vector3.up, up);
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.ProjectOnPlane(Vector3.right, up);
            // Keep twist relative to the torso, rather than shortest-arc rotation in world space.
            return Quaternion.LookRotation(facing.normalized, up);
        }

        public static Vector3 Mirror(Vector3 value, float side)
        {
            value.x *= side;
            return value;
        }
    }
}
