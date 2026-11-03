using UnityEngine;

namespace AncientFuture.WaterAvatar
{
    /// <summary>Poses detached water forms without inferring elbows or finger orientation.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WaterAvatarRig))]
    public sealed class WaterAvatarRemoteForm : MonoBehaviour
    {
        public Renderer leftFlow;
        public Renderer rightFlow;
        [Min(0.05f)] public float fadeOutSeconds = 0.22f;
        [Min(0.05f)] public float fadeInSeconds = 0.32f;
        [Min(1f)] public float followSpeed = 18f;
        [Min(0.5f)] public float maximumReach = 1.35f;
        [Min(0.1f)] public float relocationDistance = 0.45f;

        private int visibilityId;
        private readonly HandPose left = new HandPose();
        private readonly HandPose right = new HandPose();
        private MaterialPropertyBlock properties;
        private Quaternion bodyRotation;
        private bool hasHead;
        private float bodyVisibility;

        private sealed class HandPose
        {
            public bool hasPose;
            public Vector3 position;
            public Quaternion flowRotation;
            public float visibility;
            public float flowLength;
        }

        private void OnEnable()
        {
            visibilityId = Shader.PropertyToID("_Visibility");
            hasHead = false;
            bodyVisibility = 0f;
            left.hasPose = right.hasPose = false;
            left.visibility = right.visibility = 0f;
        }

        // Called exclusively by WaterAvatarRig on the Unity main thread.
        public void ApplyPose(WaterAvatarRig rig, float deltaTime, bool leftTracking, bool rightTracking)
        {
            if (rig.avatar == null || rig.torso == null || rig.headBone == null) return;
            float dt = Mathf.Max(0f, deltaTime);
            Transform head = rig.avatar._head;
            bool headValid = IsValidPose(head);
            if (headValid)
            {
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude > 0.001f)
                {
                    Quaternion target = Quaternion.LookRotation(forward.normalized, Vector3.up);
                    bodyRotation = hasHead ? Quaternion.Slerp(bodyRotation, target,
                        1f - Mathf.Exp(-rig.bodyTurnSpeed * dt)) : target;
                }
                else if (!hasHead) bodyRotation = transform.rotation;
                rig.headBone.SetPositionAndRotation(head.position, head.rotation);
                rig.torso.SetPositionAndRotation(head.position + bodyRotation * (WaterAvatarRig.RestTorso - WaterAvatarRig.RestHead), bodyRotation);
                hasHead = true;
            }
            // Missing/invalid head data holds the last finite form while it dissolves.
            bodyVisibility = Fade(bodyVisibility, headValid && hasHead, dt);
            SetVisibility(rig.upperBody, bodyVisibility);
            PoseHand(rig, -1f, rig.avatar._leftHand, rig.leftPalm, rig.leftHandRenderer, leftFlow,
                left, headValid && leftTracking, dt);
            PoseHand(rig, 1f, rig.avatar._rightHand, rig.rightPalm, rig.rightHandRenderer, rightFlow,
                right, headValid && rightTracking, dt);
            if (rig.droplets != null)
            {
                if (headValid && !rig.droplets.isPlaying) rig.droplets.Play(true);
                else if (!headValid && rig.droplets.isPlaying) rig.droplets.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void PoseHand(WaterAvatarRig rig, float side, Transform target, Transform palm,
            Renderer hand, Renderer flow, HandPose state, bool tracking, float dt)
        {
            if (palm == null) return;
            Vector3 shoulder = rig.headBone.position + bodyRotation *
                (WaterAvatarRig.Mirror(WaterAvatarRig.RestShoulder, side) - WaterAvatarRig.RestHead);
            bool valid = tracking && hasHead && IsValidPose(target);
            Vector3 desired = state.position;
            if (valid)
            {
                desired = target.position;
                float reach = Vector3.Distance(desired, shoulder);
                // Reject default-origin and out-of-range samples, but never infer loss from stillness.
                bool defaultPose = target.localPosition.sqrMagnitude < 0.000001f &&
                    Quaternion.Angle(target.localRotation, Quaternion.identity) < 0.1f;
                valid = !defaultPose && reach <= maximumReach;
            }

            // A large correction dissolves at the old position before reappearing at the new one.
            bool relocating = valid && state.hasPose && state.visibility > 0.001f &&
                Vector3.Distance(state.position, desired) > relocationDistance;
            if (valid && !relocating)
            {
                bool snapInvisible = !state.hasPose || state.visibility <= 0.001f;
                float blend = 1f - Mathf.Exp(-followSpeed * dt);
                state.position = snapInvisible ? desired : Vector3.Lerp(state.position, desired, blend);
                Vector3 towardBody = shoulder - state.position;
                // Only position defines the current's direction; wrist roll cannot flip it.
                Quaternion rotation = towardBody.sqrMagnitude > 0.0001f
                    ? WaterAvatarRig.SegmentRotation(towardBody, bodyRotation * Vector3.forward)
                    : bodyRotation;
                state.flowRotation = snapInvisible ? rotation : Quaternion.Slerp(state.flowRotation, rotation, blend);
                state.flowLength = Mathf.Clamp01((Vector3.Distance(shoulder, state.position) - 0.22f) / 0.20f);
                state.hasPose = true;
            }
            state.visibility = Fade(state.visibility, valid && !relocating && state.hasPose, dt);
            if (state.hasPose)
            {
                palm.SetPositionAndRotation(state.position, Quaternion.identity);
                if (flow != null)
                {
                    flow.transform.SetPositionAndRotation(state.position + state.flowRotation * Vector3.up * 0.085f, state.flowRotation);
                    // Keep a gap to the torso even when hands are folded near the chest.
                    flow.transform.localScale = new Vector3(1f, state.flowLength, 1f);
                }
            }
            SetVisibility(hand, state.visibility);
            SetVisibility(flow, state.visibility * Mathf.Clamp01(state.flowLength * 3f));
        }

        private float Fade(float value, bool visible, float dt)
        {
            return Mathf.MoveTowards(value, visible ? 1f : 0f,
                dt / Mathf.Max(0.05f, visible ? fadeInSeconds : fadeOutSeconds));
        }

        private void SetVisibility(Renderer renderer, float value)
        {
            if (renderer == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(visibilityId, value);
            renderer.SetPropertyBlock(properties);
            renderer.enabled = value > 0.001f;
        }

        private static bool IsValidPose(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            Vector3 p = target.position;
            Quaternion r = target.rotation;
            return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) &&
                float.IsFinite(r.x) && float.IsFinite(r.y) && float.IsFinite(r.z) && float.IsFinite(r.w);
        }
    }
}
