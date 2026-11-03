using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AncientFuture.WaterAvatar.Editor
{
    public static partial class WaterAvatarBuilder
    {
        [MenuItem("Tools/Ancient Future/Rebuild Remote Water Avatar Only")]
        public static void RebuildRemote()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding the remote avatar.");
            const string path = Root + "/Prefabs/WaterAvatar_Remote.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WaterAvatarRig rig = root.GetComponent<WaterAvatarRig>();
                if (rig == null) throw new InvalidOperationException("The remote prefab requires WaterAvatarRig.");
                WaterAvatarRemoteForm form = root.GetComponent<WaterAvatarRemoteForm>();
                if (form == null) form = root.AddComponent<WaterAvatarRemoteForm>();

                Material source = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/LivingWater.mat");
                Material water = SaveMaterial("RemoteLivingWater", "Ancient Future/Remote Living Water");
                water.CopyPropertiesFromMaterial(source);
                water.SetFloat("_Visibility", 1f);
                EditorUtility.SetDirty(water);

                Mesh body = IsoSurface(RemoteBodyField, new Vector3(-0.26f, 0.45f, -0.17f),
                    new Vector3(0.26f, 1.88f, 0.17f), 0.010f, "Remote water body");
                Vector3[] vertices = body.vertices;
                Color[] colors = new Color[vertices.Length];
                BoneWeight[] weights = new BoneWeight[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 p = vertices[i];
                    float head = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.50f, 1.58f, p.y));
                    weights[i] = SortedWeight(1, head, 0, 1f - head, 0, 0);
                    float unevenHeight = p.y + 0.035f * Mathf.Sin(p.x * 31f + p.z * 23f);
                    float opacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 1.23f, unevenHeight));
                    colors[i] = new Color(1f, 1f, 1f, opacity);
                }
                body.colors = colors;
                body.boneWeights = weights;
                body.bindposes = new[] { rig.torso.worldToLocalMatrix * root.transform.localToWorldMatrix,
                    rig.headBone.worldToLocalMatrix * root.transform.localToWorldMatrix };
                body = SaveMesh(body, "WaterRemoteBody");
                SkinnedMeshRenderer skin = (SkinnedMeshRenderer)rig.upperBody;
                skin.sharedMesh = body;
                skin.sharedMaterial = water;
                skin.bones = new[] { rig.torso, rig.headBone };
                skin.localBounds = new Bounds(new Vector3(0, 1.1f, 0), new Vector3(2.5f, 3f, 2.5f));

                Mesh hand = IsoSurface(p => Ellipsoid(p, Vector3.zero, new Vector3(0.057f, 0.066f, 0.057f)),
                    new Vector3(-0.075f, -0.085f, -0.075f), new Vector3(0.075f, 0.085f, 0.075f), 0.007f, "Remote hand droplet");
                hand = SaveMesh(hand, "WaterRemoteHand");
                ReplaceHand(rig.leftHandRenderer, hand, water);
                ReplaceHand(rig.rightHandRenderer, hand, water);

                Mesh current = IsoSurface(p => Capsule(p, new Vector3(0, 0.025f, 0),
                    new Vector3(0, 0.185f, 0), 0.024f, 0.002f),
                    new Vector3(-0.055f, -0.005f, -0.055f), new Vector3(0.055f, 0.21f, 0.055f), 0.006f, "Detached water current");
                Vector3[] cv = current.vertices;
                Color[] cc = new Color[cv.Length];
                for (int i = 0; i < cv.Length; i++)
                {
                    float t = Mathf.Clamp01(cv[i].y / 0.19f);
                    cv[i].x += 0.012f * Mathf.Sin(t * Mathf.PI);
                    cc[i] = new Color(1, 1, 1, (1f - Mathf.SmoothStep(0f, 1f, t)) * 0.85f);
                }
                current.vertices = cv;
                current.colors = cc;
                current.RecalculateNormals();
                current.RecalculateBounds();
                current = SaveMesh(current, "WaterRemoteCurrent");
                form.leftFlow = ReplaceCurrent(rig.leftPalm.parent, "Left Detached Water Current", current, water);
                form.rightFlow = ReplaceCurrent(rig.rightPalm.parent, "Right Detached Water Current", current, water);
                InitializeCurrent(rig, -1f, rig.leftPalm, form.leftFlow.transform);
                InitializeCurrent(rig, 1f, rig.rightPalm, form.rightFlow.transform);
                if (rig.droplets != null)
                {
                    rig.droplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    rig.droplets.transform.localPosition = new Vector3(0.025f, -0.24f, 0);
                    ParticleSystem.MainModule main = rig.droplets.main;
                    main.maxParticles = 24;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.007f, 0.020f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3f);
                    main.startColor = new Color(1, 1, 1, 0.35f);
                    ParticleSystem.ShapeModule shape = rig.droplets.shape;
                    shape.scale = new Vector3(0.22f, 0.34f, 0.12f);
                    shape.rotation = new Vector3(90, 0, 0);
                    ParticleSystem.EmissionModule emission = rig.droplets.emission;
                    emission.rateOverTime = 5;
                    rig.droplets.GetComponent<ParticleSystemRenderer>().sharedMaterial = water;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
                Debug.Log($"Remote water avatar rebuilt: {body.triangles.Length / 3 + 2 * hand.triangles.Length / 3 + 2 * current.triangles.Length / 3:N0} triangles. Local assets preserved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ReplaceHand(Renderer renderer, Mesh mesh, Material material)
        {
            renderer.GetComponent<MeshFilter>().sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.parent.localRotation = Quaternion.identity;
            renderer.gameObject.name = "Water Hand Droplet";
        }

        private static Renderer ReplaceCurrent(Transform parent, string name, Mesh mesh, Material material)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                existing.GetComponent<MeshFilter>().sharedMesh = mesh;
                Renderer renderer = existing.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                return renderer;
            }
            return StaticMesh(parent, name, mesh, material);
        }

        private static void InitializeCurrent(WaterAvatarRig rig, float side, Transform palm, Transform current)
        {
            Vector3 shoulder = WaterAvatarRig.Mirror(WaterAvatarRig.RestShoulder, side);
            Quaternion rotation = WaterAvatarRig.SegmentRotation(shoulder - palm.position, Vector3.forward);
            current.SetPositionAndRotation(palm.position + rotation * Vector3.up * 0.085f, rotation);
        }

        private static float RemoteBodyField(Vector3 p)
        {
            // The chest flows into a curved, closed taper without a waist cap or hip outline.
            float d = Ellipsoid(p, new Vector3(0, 1.30f, -0.015f), new Vector3(0.164f, 0.18f, 0.095f));
            Vector3 symmetric = p;
            symmetric.x = Mathf.Abs(p.x);
            d = SmoothUnion(d, Ellipsoid(symmetric, new Vector3(0.135f, 1.40f, 0), new Vector3(0.065f, 0.055f, 0.065f)), 0.035f);
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0.005f, 1.14f, -0.009f), new Vector3(0.123f, 0.17f, 0.082f)), 0.055f);
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0.026f, 0.98f, 0.002f), new Vector3(0.089f, 0.16f, 0.065f)), 0.050f);
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0.061f, 0.82f, 0.014f), new Vector3(0.054f, 0.15f, 0.045f)), 0.040f);
            d = SmoothUnion(d, Capsule(p, new Vector3(0.067f, 0.79f, 0.015f), new Vector3(0.10f, 0.56f, 0.03f), 0.040f, 0.002f), 0.025f);
            d = SmoothUnion(d, Capsule(p, new Vector3(0, 1.44f, 0), new Vector3(0, 1.59f, 0.005f), 0.047f, 0.041f), 0.04f);
            // Retain the original face without clipping the neck field.
            p = symmetric;
            float head = Ellipsoid(p, new Vector3(0, 1.718f, -0.005f), new Vector3(0.098f, 0.114f, 0.098f));
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0, 1.663f, 0.028f), new Vector3(0.074f, 0.081f, 0.075f)), 0.015f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0, 1.619f, 0.035f), new Vector3(0.052f, 0.036f, 0.060f)), 0.012f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0.050f, 1.666f, 0.065f), new Vector3(0.028f, 0.027f, 0.033f)), 0.009f);
            head = SmoothUnion(head, Capsule(p, new Vector3(0, 1.702f, 0.087f), new Vector3(0, 1.666f, 0.122f), 0.011f, 0.014f), 0.009f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0, 1.637f, 0.102f), new Vector3(0.027f, 0.006f, 0.012f)), 0.004f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0, 1.627f, 0.10f), new Vector3(0.024f, 0.006f, 0.012f)), 0.004f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0.094f, 1.682f, 0f), new Vector3(0.015f, 0.028f, 0.019f)), 0.008f);
            float socket = Ellipsoid(p, new Vector3(0.038f, 1.697f, 0.094f), new Vector3(0.024f, 0.011f, 0.017f));
            head = -SmoothUnion(-head, socket, 0.004f);
            head = SmoothUnion(head, Ellipsoid(p, new Vector3(0.038f, 1.697f, 0.081f), new Vector3(0.022f, 0.008f, 0.010f)), 0.002f);
            return SmoothUnion(d, head, 0.018f);
        }
    }
}
