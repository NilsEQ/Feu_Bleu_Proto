using System;
using System.Collections.Generic;
using System.IO;
using AncientFuture.WaterAvatar;
using Styly.NetSync;
using Styly.XRRig;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace AncientFuture.WaterAvatar.Editor
{
    /// <summary>Bakes original water geometry into reusable assets. No mesh generation runs on the headset.</summary>
    public static partial class WaterAvatarBuilder
    {
        public const string Root = "Assets/Avatars/WaterAvatar";
        private const string ScenePath = "Assets/Scenes/Avatar_Test.unity";

        [MenuItem("Tools/Ancient Future/Build Water Avatar Test")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the avatar.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open Avatar_Test before building. Other scenes are never replaced.");
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Textures");
            AssetDatabase.Refresh();

            Material water = SaveMaterial("LivingWater", "Ancient Future/Living Water");
            water.SetColor("_DeepColor", new Color(0.025f, 0.20f, 0.27f));
            water.SetColor("_RimColor", new Color(0.62f, 0.90f, 1f));
            water.SetFloat("_Opacity", 0.16f);
            water.SetFloat("_FlowScale", 28f);
            water.SetFloat("_FlowBrightness", 1.8f);
            water.SetFloat("_RimStrength", 0.95f);
            water.SetTexture("_FlowTex", CreateFlowTexture());
            water.enableInstancing = true;
            EditorUtility.SetDirty(water);

            Mesh body = IsoSurface(BodyField, new Vector3(-0.52f, 0.90f, -0.15f), new Vector3(0.52f, 1.88f, 0.17f), 0.011f, "Water upper body");
            body = AddWisps(body);
            Mesh leftHand = IsoSurface(p => HandField(p, -1f), new Vector3(-0.072f, -0.045f, -0.015f), new Vector3(0.072f, 0.04f, 0.19f), 0.0045f, "Water left hand");
            Mesh rightHand = IsoSurface(p => HandField(p, 1f), new Vector3(-0.072f, -0.045f, -0.015f), new Vector3(0.072f, 0.04f, 0.19f), 0.0045f, "Water right hand");
            leftHand = SaveMesh(leftHand, "WaterHandLeft");
            rightHand = SaveMesh(rightHand, "WaterHandRight");
            Mesh bubble = CreateBubble();
            bubble = SaveMesh(bubble, "WaterDroplet");

            GameObject root = new GameObject("Water Avatar");
            NetSyncAvatar net = root.AddComponent<NetSyncAvatar>();
            net._head = Child(root.transform, "Head Tracker", WaterAvatarRig.RestHead);
            net._leftHand = Child(root.transform, "Left Hand Tracker", WaterAvatarRig.Mirror(WaterAvatarRig.RestWrist, -1f));
            net._rightHand = Child(root.transform, "Right Hand Tracker", WaterAvatarRig.RestWrist);
            net._leftHand.localRotation = Quaternion.Euler(90, 0, 15);
            net._rightHand.localRotation = Quaternion.Euler(90, 0, -15);
            net._virtualTransforms = Array.Empty<Transform>();

            WaterAvatarRig rig = root.AddComponent<WaterAvatarRig>();
            rig.avatar = net;
            Transform skeleton = Child(root.transform, "Water Skeleton", Vector3.zero);
            rig.torso = Child(skeleton, "Torso", WaterAvatarRig.RestTorso);
            rig.headBone = Child(skeleton, "Head", WaterAvatarRig.RestHead);
            rig.leftUpperArm = Segment(skeleton, "Left Upper Arm", -1f, true);
            rig.leftForearm = Segment(skeleton, "Left Forearm", -1f, false);
            rig.rightUpperArm = Segment(skeleton, "Right Upper Arm", 1f, true);
            rig.rightForearm = Segment(skeleton, "Right Forearm", 1f, false);
            rig.leftPalm = Child(skeleton, "Left Palm", net._leftHand.localPosition);
            rig.rightPalm = Child(skeleton, "Right Palm", net._rightHand.localPosition);
            rig.leftPalm.rotation = net._leftHand.rotation;
            rig.rightPalm.rotation = net._rightHand.rotation;

            Transform[] bones = { rig.torso, rig.headBone, rig.leftUpperArm, rig.leftForearm, rig.rightUpperArm, rig.rightForearm };
            Matrix4x4[] bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bind[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            body.bindposes = bind;
            Vector3[] vertices = body.vertices;
            BoneWeight[] weights = new BoneWeight[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) weights[i] = Weight(vertices[i]);
            body.boneWeights = weights;
            body = SaveMesh(body, "WaterUpperBody");

            Transform surface = Child(root.transform, "Water Surface - Upper Body", Vector3.zero);
            SkinnedMeshRenderer skin = surface.gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = body;
            skin.sharedMaterial = water;
            skin.bones = bones;
            skin.rootBone = rig.torso;
            skin.localBounds = new Bounds(Vector3.zero, new Vector3(3.5f, 3.5f, 3.5f));
            skin.quality = SkinQuality.Bone4;
            skin.updateWhenOffscreen = false;
            ConfigureRenderer(skin);
            rig.upperBody = skin;
            rig.leftHandRenderer = StaticMesh(rig.leftPalm, "Water Left Hand", leftHand, water);
            rig.rightHandRenderer = StaticMesh(rig.rightPalm, "Water Right Hand", rightHand, water);
            rig.droplets = CreateDroplets(rig.torso, water, bubble);

            root.name = "WaterAvatar_Remote";
            GameObject remote = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/WaterAvatar_Remote.prefab");
            root.name = "WaterAvatar_Local";
            GameObject local = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/WaterAvatar_Local.prefab");
            Object.DestroyImmediate(root);

            RebuildRemote();
            ConfigureTestScene(local, remote);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Water avatar ready: {body.vertexCount:N0} body vertices, {body.triangles.Length / 3:N0} body triangles; upper body only.");
        }

        private static void ConfigureTestScene(GameObject local, GameObject remote)
        {
            GameObject old = GameObject.Find("Water Avatar Test");
            if (old != null) Undo.DestroyObjectImmediate(old);
            GameObject test = new GameObject("Water Avatar Test");
            Undo.RegisterCreatedObjectUndo(test, "Create water avatar test");
            GameObject demo = (GameObject)PrefabUtility.InstantiatePrefab(remote);
            demo.name = "Water Avatar - Animated Preview";
            demo.transform.SetParent(test.transform, false);
            demo.transform.SetPositionAndRotation(new Vector3(0f, 0f, 1.65f), Quaternion.Euler(0, 180, 0));
            demo.GetComponent<NetSyncAvatar>().enabled = false;

            GameObject environment = new GameObject("VR Environment - Hidden in MR");
            environment.transform.SetParent(test.transform, false);
            Material backdrop = SaveMaterial("UnderwaterBackdrop", "Ancient Future/Underwater Backdrop");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Underwater Light";
            wall.transform.SetParent(environment.transform, false);
            wall.transform.position = new Vector3(0, 1.7f, 4.5f);
            wall.transform.localScale = new Vector3(12, 7, 1);
            Object.DestroyImmediate(wall.GetComponent<Collider>());
            wall.GetComponent<Renderer>().sharedMaterial = backdrop;
            ConfigureRenderer(wall.GetComponent<Renderer>());

            WaterAvatarPreview preview = test.AddComponent<WaterAvatarPreview>();
            preview.previewAvatar = demo.GetComponent<NetSyncAvatar>();
            preview.xrRig = Object.FindFirstObjectByType<StylyXrRig>();
            preview.virtualEnvironment = environment;
            preview.mixedReality = false;
            preview.autoSwitchModes = true;
            preview.modeSwitchInterval = 10f;
            if (preview.xrRig != null)
            {
                SerializedObject rig = new SerializedObject(preview.xrRig);
                rig.FindProperty("passthroughMode").boolValue = false;
                rig.FindProperty("CameraHeightInEditor").floatValue = 1.40f;
                rig.ApplyModifiedProperties();
            }
            NetSyncManager manager = Object.FindFirstObjectByType<NetSyncManager>();
            if (manager == null) throw new InvalidOperationException("Avatar_Test requires its existing NetSyncManager.");
            SerializedObject settings = new SerializedObject(manager);
            settings.FindProperty("_localAvatarPrefab").objectReferenceValue = local;
            settings.FindProperty("_remoteAvatarPrefab").objectReferenceValue = remote;
            settings.FindProperty("_offlineMode").boolValue = true;
            settings.ApplyModifiedProperties();

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.fieldOfView = 48;
                camera.nearClipPlane = 0.03f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = false;
                // Passthrough alpha is retained without requiring changes to shared pipeline assets.
                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(data);
            }
            Light light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(36f, -28f, 0f);
                light.color = new Color(0.76f, 0.91f, 1f);
                light.intensity = 1.3f;
                EditorUtility.SetDirty(light);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.18f, 0.24f);
            Selection.activeGameObject = demo;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1.40f, 1.65f), Quaternion.identity, 1.3f);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static Transform Segment(Transform parent, string name, float side, bool upper)
        {
            Vector3 start = WaterAvatarRig.Mirror(upper ? WaterAvatarRig.RestShoulder : WaterAvatarRig.RestElbow, side);
            Vector3 end = WaterAvatarRig.Mirror(upper ? WaterAvatarRig.RestElbow : WaterAvatarRig.RestWrist, side);
            Transform bone = Child(parent, name, start);
            bone.rotation = WaterAvatarRig.SegmentRotation(end - start, Vector3.forward);
            return bone;
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static Renderer StaticMesh(Transform parent, string name, Mesh mesh, Material material)
        {
            GameObject go = Child(parent, name, Vector3.zero).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            ConfigureRenderer(renderer);
            return renderer;
        }

        private static ParticleSystem CreateDroplets(Transform parent, Material material, Mesh mesh)
        {
            GameObject go = Child(parent, "Suspended Water Droplets", new Vector3(0, 0.1f, 0)).gameObject;
            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 5;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.019f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = Color.white;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 9;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.67f, 0.70f, 0.28f);
            shape.rotation = new Vector3(-90, 0, 0);
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(0.75f, 1), new Keyframe(1, 0)));
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            ConfigureRenderer(renderer);
            return particles;
        }

        private static Material SaveMaterial(string name, string shaderName)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            return material;
        }

        private static Texture2D CreateFlowTexture()
        {
            const int size = 128;
            string path = Root + "/Textures/SeamlessWaterFlow.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool create = texture == null;
            if (create) texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            float[] heights = new float[size * size];
            float TileNoise(float x, float y, float frequency)
            {
                float a = Mathf.PerlinNoise(31 + x * frequency, 47 + y * frequency);
                float b = Mathf.PerlinNoise(31 + (x - 1) * frequency, 47 + y * frequency);
                float c = Mathf.PerlinNoise(31 + x * frequency, 47 + (y - 1) * frequency);
                float d = Mathf.PerlinNoise(31 + (x - 1) * frequency, 47 + (y - 1) * frequency);
                return Mathf.Lerp(Mathf.Lerp(a, b, x), Mathf.Lerp(c, d, x), y);
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    heights[x + y * size] = TileNoise(u, v, 5) * 0.64f + TileNoise(u, v, 11) * 0.25f + TileNoise(u, v, 23) * 0.11f;
                }
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = heights[(x + 1) % size + y * size] - heights[(x + size - 1) % size + y * size];
                    float dy = heights[x + (y + 1) % size * size] - heights[x + (y + size - 1) % size * size];
                    colors[x + y * size] = new Color(0.5f + dx * 5, 0.5f + dy * 5, heights[x + y * size], 1);
                }
            texture.name = "SeamlessWaterFlow";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 2;
            texture.SetPixels(colors); texture.Apply(true, false);
            if (create) AssetDatabase.CreateAsset(texture, path);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Root + "/Meshes/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = name;
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                // Rebuild native vertex buffers explicitly; CopySerialized can leave a stale GPU stride.
                existing.Clear(false);
                existing.indexFormat = mesh.indexFormat;
                existing.vertices = mesh.vertices;
                existing.normals = mesh.normals;
                existing.colors = mesh.colors;
                existing.boneWeights = mesh.boneWeights;
                existing.bindposes = mesh.bindposes;
                existing.triangles = mesh.triangles;
                existing.bounds = mesh.bounds;
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);
                mesh = existing;
                EditorUtility.SetDirty(mesh);
            }
            return mesh;
        }

        private static float SmoothUnion(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1 - h);
        }

        private static float Ellipsoid(Vector3 p, Vector3 center, Vector3 radius)
        {
            p -= center;
            Vector3 q = new Vector3(p.x / radius.x, p.y / radius.y, p.z / radius.z);
            Vector3 q2 = new Vector3(q.x / radius.x, q.y / radius.y, q.z / radius.z);
            float k = q.magnitude;
            return k < 0.0001f ? -Mathf.Min(radius.x, Mathf.Min(radius.y, radius.z)) : k * (k - 1f) / q2.magnitude;
        }

        private static float Capsule(Vector3 p, Vector3 a, Vector3 b, float ra, float rb)
        {
            Vector3 axis = b - a;
            float h = Mathf.Clamp01(Vector3.Dot(p - a, axis) / axis.sqrMagnitude);
            return (p - a - axis * h).magnitude - Mathf.Lerp(ra, rb, h);
        }

        private static float BodyField(Vector3 p)
        {
            p.x = Mathf.Abs(p.x);
            float d = Ellipsoid(p, new Vector3(0, 1.26f, -0.02f), new Vector3(0.158f, 0.225f, 0.09f));
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0.072f, 1.34f, 0.023f), new Vector3(0.09f, 0.095f, 0.09f)), 0.04f);
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0, 1.09f, 0), new Vector3(0.098f, 0.12f, 0.072f)), 0.05f);
            d = SmoothUnion(d, Capsule(p, new Vector3(0.045f, 1.42f, 0), WaterAvatarRig.RestShoulder, 0.056f, 0.061f), 0.055f);
            d = SmoothUnion(d, Capsule(p, WaterAvatarRig.RestShoulder, WaterAvatarRig.RestElbow, 0.057f, 0.038f), 0.035f);
            d = SmoothUnion(d, Capsule(p, WaterAvatarRig.RestElbow, WaterAvatarRig.RestWrist, 0.039f, 0.024f), 0.025f);
            d = SmoothUnion(d, Capsule(p, new Vector3(0, 1.44f, 0), new Vector3(0, 1.59f, 0.005f), 0.047f, 0.041f), 0.04f);

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

        private static float HandField(Vector3 p, float side)
        {
            p.x *= -side;
            float d = Capsule(p, new Vector3(0, 0, -0.005f), new Vector3(0, 0, 0.03f), 0.024f, 0.025f);
            d = SmoothUnion(d, Ellipsoid(p, new Vector3(0, 0, 0.044f), new Vector3(0.038f, 0.018f, 0.047f)), 0.012f);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.028f + i * 0.0185f;
                float length = i == 0 ? 0.067f : i == 1 ? 0.087f : i == 2 ? 0.080f : 0.062f;
                Vector3 start = new Vector3(x, 0, 0.078f);
                Vector3 middle = new Vector3(x * 1.22f, -0.003f, 0.078f + length * 0.55f);
                Vector3 end = new Vector3(x * 1.32f, -0.014f, 0.078f + length);
                float finger = Capsule(p, start, middle, 0.0088f, 0.0074f);
                finger = SmoothUnion(finger, Capsule(p, middle, end, 0.0075f, 0.0058f), 0.005f);
                d = SmoothUnion(d, finger, 0.008f);
            }
            float thumb = Capsule(p, new Vector3(0.024f, -0.002f, 0.024f), new Vector3(0.052f, -0.009f, 0.058f), 0.014f, 0.010f);
            thumb = SmoothUnion(thumb, Capsule(p, new Vector3(0.052f, -0.009f, 0.058f), new Vector3(0.058f, -0.016f, 0.086f), 0.010f, 0.0065f), 0.006f);
            return SmoothUnion(d, thumb, 0.008f);
        }

        private static BoneWeight Weight(Vector3 p)
        {
            if (p.y > 1.50f)
            {
                float head = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.50f, 1.58f, p.y));
                return SortedWeight(1, head, 0, 1 - head, 0, 0);
            }
            float arm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.15f, 0.235f, Mathf.Abs(p.x)));
            float lower = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.13f, 1.23f, p.y));
            int upperIndex = p.x < 0 ? 2 : 4;
            return SortedWeight(0, 1 - arm, upperIndex, arm * (1 - lower), upperIndex + 1, arm * lower);
        }

        private static BoneWeight SortedWeight(int a, float wa, int b, float wb, int c, float wc)
        {
            // Unity requires descending weights, including when quality limits the influence count.
            void Swap(ref int i, ref float w, ref int j, ref float v)
            {
                if (w >= v) return;
                int index = i; i = j; j = index;
                float weight = w; w = v; v = weight;
            }
            Swap(ref a, ref wa, ref b, ref wb);
            Swap(ref a, ref wa, ref c, ref wc);
            Swap(ref b, ref wb, ref c, ref wc);
            return new BoneWeight { boneIndex0 = a, weight0 = wa, boneIndex1 = b, weight1 = wb, boneIndex2 = c, weight2 = wc };
        }

        private static Mesh IsoSurface(Func<Vector3, float> field, Vector3 min, Vector3 max, float step, string name)
        {
            // Surface nets share one vertex per intersecting cell, reducing the mobile triangle budget.
            int nx = Mathf.CeilToInt((max.x - min.x) / step) + 1;
            int ny = Mathf.CeilToInt((max.y - min.y) / step) + 1;
            int nz = Mathf.CeilToInt((max.z - min.z) / step) + 1;
            int Index(int x, int y, int z) => x + nx * (y + ny * z);
            float[] values = new float[nx * ny * nz];
            int[] cells = new int[values.Length];
            for (int z = 0; z < nz; z++)
                for (int y = 0; y < ny; y++)
                    for (int x = 0; x < nx; x++)
                    {
                        int index = Index(x, y, z);
                        values[index] = field(min + new Vector3(x, y, z) * step);
                        cells[index] = -1;
                    }
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            int[,] offsets = { {0,0,0}, {1,0,0}, {1,1,0}, {0,1,0}, {0,0,1}, {1,0,1}, {1,1,1}, {0,1,1} };
            int[,] edges = { {0,1}, {1,2}, {2,3}, {3,0}, {4,5}, {5,6}, {6,7}, {7,4}, {0,4}, {1,5}, {2,6}, {3,7} };
            Vector3[] corners = new Vector3[8];
            float[] cv = new float[8];
            Vector3 Normal(Vector3 point)
            {
                const float e = 0.0007f;
                return new Vector3(field(point + Vector3.right * e) - field(point - Vector3.right * e),
                    field(point + Vector3.up * e) - field(point - Vector3.up * e),
                    field(point + Vector3.forward * e) - field(point - Vector3.forward * e)).normalized;
            }
            for (int z = 0; z < nz - 1; z++)
                for (int y = 0; y < ny - 1; y++)
                    for (int x = 0; x < nx - 1; x++)
                    {
                        int inside = 0;
                        for (int c = 0; c < 8; c++)
                        {
                            int cx = x + offsets[c, 0], cy = y + offsets[c, 1], cz = z + offsets[c, 2];
                            corners[c] = min + new Vector3(cx, cy, cz) * step;
                            cv[c] = values[Index(cx, cy, cz)];
                            if (cv[c] < 0) inside++;
                        }
                        if (inside == 0 || inside == 8) continue;
                        Vector3 center = Vector3.zero;
                        int count = 0;
                        for (int e = 0; e < 12; e++)
                        {
                            int a = edges[e, 0], b = edges[e, 1];
                            if ((cv[a] < 0) == (cv[b] < 0)) continue;
                            center += Vector3.LerpUnclamped(corners[a], corners[b], cv[a] / (cv[a] - cv[b]));
                            count++;
                        }
                        center /= count;
                        cells[Index(x, y, z)] = vertices.Count;
                        vertices.Add(center);
                        normals.Add(Normal(center));
                    }
            void Triangle(int a, int b, int c)
            {
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                triangles.Add(a);
                if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) >= 0)
                { triangles.Add(b); triangles.Add(c); }
                else { triangles.Add(c); triangles.Add(b); }
            }
            void Quad(int ia, int ib, int ic, int id)
            {
                int a = cells[ia], b = cells[ib], c = cells[ic], d = cells[id];
                if (a < 0 || b < 0 || c < 0 || d < 0) return;
                Triangle(a, b, c); Triangle(a, c, d);
            }
            for (int z = 1; z < nz - 1; z++)
                for (int y = 1; y < ny - 1; y++)
                    for (int x = 1; x < nx - 1; x++)
                    {
                        int p = Index(x, y, z);
                        bool inside = values[p] < 0;
                        if (inside != (values[Index(x + 1, y, z)] < 0))
                            Quad(Index(x, y - 1, z - 1), Index(x, y, z - 1), p, Index(x, y - 1, z));
                        if (inside != (values[Index(x, y + 1, z)] < 0))
                            Quad(Index(x - 1, y, z - 1), Index(x - 1, y, z), p, Index(x, y, z - 1));
                        if (inside != (values[Index(x, y, z + 1)] < 0))
                            Quad(Index(x - 1, y - 1, z), Index(x, y - 1, z), p, Index(x - 1, y, z));
                    }
            Mesh mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            Color[] colors = new Color[vertices.Count];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh AddWisps(Mesh body)
        {
            List<Vector3> v = new List<Vector3>(body.vertices);
            List<Vector3> n = new List<Vector3>(body.normals);
            List<Color> colors = new List<Color>(body.colors);
            List<int> triangles = new List<int>(body.triangles);
            for (int strand = 0; strand < 7; strand++)
            {
                int first = v.Count;
                float phase = strand * 2.4f;
                for (int j = 0; j <= 16; j++)
                {
                    float t = j / 16f;
                    float x = (strand - 3) * 0.022f;
                    Vector3 center = new Vector3(x + Mathf.Sin(t * 5 + phase) * 0.018f * t,
                        1.065f - t * (0.12f + strand % 3 * 0.018f), Mathf.Sin(phase) * 0.04f + Mathf.Cos(t * 4 + phase) * 0.015f);
                    float radius = Mathf.Lerp(0.013f, 0.0005f, t);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI * 0.25f;
                        Vector3 normal = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        v.Add(center + normal * radius);
                        n.Add(normal);
                        colors.Add(new Color(1, 1, 1, 1 - t * 0.45f));
                        if (j < 16)
                        {
                            int i = first + j * 8 + k, next = first + j * 8 + (k + 1) % 8;
                            triangles.Add(i); triangles.Add(next); triangles.Add(i + 8);
                            triangles.Add(next); triangles.Add(next + 8); triangles.Add(i + 8);
                        }
                    }
                }
            }
            body.indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            body.SetVertices(v); body.SetNormals(n); body.SetColors(colors); body.SetTriangles(triangles, 0);
            body.RecalculateBounds();
            return body;
        }

        private static Mesh CreateBubble()
        {
            return IsoSurface(p => p.magnitude - 0.5f, Vector3.one * -0.6f, Vector3.one * 0.6f, 0.22f, "Water droplet");
        }
    }
}
