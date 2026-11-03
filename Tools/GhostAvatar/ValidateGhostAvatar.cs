using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AncientFuture.WaterAvatar;
using FeuBleu.GhostAvatar.Editor;
using Newtonsoft.Json;
using Styly.NetSync;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Run with Unity Pipeline on the Editor main thread; no live server is required.</summary>
public static class ValidateGhostAvatar
{
    private const string Output = "Documentation/GhostAvatar";
    private const string PreviewPath = GhostAvatarBuilder.Root + "/GhostAvatar_Preview.unity";
    private const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly List<string> Passed = new List<string>();

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("Ghost validation: " + label);
        Passed.Add(label);
    }

    public static string Assets()
    {
        Passed.Clear();
        Check(!EditorUtility.scriptCompilationFailed, "C# compilation succeeds");
        Shader shader = Shader.Find("Feu Bleu/Ghost Silhouette");
        Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "Ghost shader supported and error-free");
        foreach (string path in new[] { GhostAvatarBuilder.RemotePath, GhostAvatarBuilder.PresencePath })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null, path + " exists");
            Check(prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), prefab.name + " has no missing scripts");
            Check(prefab.GetComponentsInChildren<Collider>(true).Length == 0, prefab.name + " is visual only");
            Check(prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0, prefab.name + " has no distracting particles");
            Check(prefab.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterial != null && r.sharedMaterial.shader == shader), prefab.name + " uses ghost materials");
        }
        var remote = AssetDatabase.LoadAssetAtPath<GameObject>(GhostAvatarBuilder.RemotePath);
        var net = remote.GetComponent<NetSyncAvatar>();
        var rig = remote.GetComponent<WaterAvatarRig>();
        Check(net != null && rig != null && remote.GetComponent<WaterAvatarRemoteForm>() != null, "Remote retains NetSync rig and tracking fades");
        Check(net._head != null && net._leftHand != null && net._rightHand != null && rig.avatar == net, "Remote tracking references are intact");
        Check(((SkinnedMeshRenderer)rig.upperBody).bones.All(b => b != null), "Skinning bones are intact");
        var presence = AssetDatabase.LoadAssetAtPath<GameObject>(GhostAvatarBuilder.PresencePath);
        Mesh mesh = presence.GetComponent<MeshFilter>().sharedMesh;
        Check(presence.GetComponent<NetSyncAvatar>() == null && presence.GetComponent<WaterAvatarRig>() == null, "Presence does not fight the physical-position driver");
        Check(mesh.bounds.max.y < 0.2f && mesh.bounds.min.y < -1f, "Presence mesh is centred at HMD height");
        Check(mesh.colors.Where((c, i) => mesh.vertices[i].y < -1.06f).All(c => c.a < 0.04f), "Lower body dissolves to transparent");
        Check(presence.GetComponent<Renderer>().sharedMaterial.GetFloat("_ZTest") == 8f, "Physical presence remains visible through virtual scenery");
        return Save("asset-validation.json", new { passed = Passed, unity = Application.unityVersion, graphics = SystemInfo.graphicsDeviceType.ToString() });
    }

    public static string CreatePreview()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            var remote = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GhostAvatarBuilder.RemotePath), scene);
            remote.name = "Ghost Remote Preview";
            var rig = remote.GetComponent<WaterAvatarRig>();
            rig.avatar._leftHand.localPosition = new Vector3(-0.34f, 1.19f, 0.06f);
            rig.avatar._rightHand.localPosition = new Vector3(0.36f, 1.12f, 0.02f);
            Camera camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0.8f, 1.5f, 2.3f);
            camera.transform.LookAt(new Vector3(0, 1.27f, 0));
            camera.fieldOfView = 37f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.038f, 0.055f, 1);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 20f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            EditorSceneManager.SaveScene(scene, PreviewPath);
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        return PreviewPath;
    }

    public static string Runtime()
    {
        Passed.Clear();
        Check(Application.isPlaying && Time.frameCount > 5, "Play mode is advancing");
        var managerObject = new GameObject("Validation Packet Manager");
        managerObject.SetActive(false);
        NetSyncManager manager = managerObject.AddComponent<NetSyncManager>();
        GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(GhostAvatarBuilder.RemotePath));
        try
        {
            var net = go.GetComponent<NetSyncAvatar>();
            var rig = go.GetComponent<WaterAvatarRig>();
            Type type = typeof(NetSyncAvatar);
            type.GetMethod("InitializeRemote", Hidden).Invoke(net, new object[] { 701, manager });
            Type packetType = type.Assembly.GetType("Styly.NetSync.ClientTransformData", true);
            Type poseType = type.Assembly.GetType("Styly.NetSync.TransformData", true);
            object packet = Activator.CreateInstance(packetType);
            void Set(string name, object value) { packetType.GetField(name).SetValue(packet, value); }
            void Pose(string name, Vector3 position)
            {
                object pose = Activator.CreateInstance(poseType);
                poseType.GetField("position").SetValue(pose, position);
                poseType.GetField("rotation").SetValue(pose, Quaternion.identity);
                Set(name, pose);
            }
            void Flags(int flags)
            {
                FieldInfo field = packetType.GetField("flags");
                field.SetValue(packet, Enum.ToObject(field.FieldType, flags));
            }
            Type clock = type.Assembly.GetTypes().First(t => t.Name == "NetSyncClock");
            double now = (double)clock.GetMethod("NowSeconds", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, null);
            Vector3 head = new Vector3(2f, 1.6f, 3f);
            Vector3 left = head + new Vector3(-0.35f, -0.35f, 0.1f);
            Pose("head", head); Pose("leftHand", left); Pose("rightHand", head + new Vector3(0.35f, -0.35f, 0.1f));
            Set("clientNo", 701); Set("poseTime", now - 0.5); Set("relayBroadcastTime", now - 0.5); Set("poseSeq", (ushort)1); Flags(4 | 8 | 16);
            MethodInfo receive = type.GetMethod("SetTransformData", Hidden);
            receive.Invoke(net, new[] { packet });
            type.GetMethod("Update", Hidden).Invoke(net, null);
            rig.ApplyPose(1f);
            Check(Vector3.Distance(rig.headBone.position, head) < 0.001f && Vector3.Distance(rig.leftPalm.position, left) < 0.001f, "Received NetSync packet drives ghost head and hand: " + rig.headBone.position + ", " + rig.leftPalm.position);
            Check(rig.upperBody.enabled && rig.leftHandRenderer.enabled && rig.rightHandRenderer.enabled, "Remote body and hands are visible");
            Flags(4 | 8); Set("poseSeq", (ushort)2); receive.Invoke(net, new[] { packet });
            rig.ApplyPose(0.06f);
            var block = new MaterialPropertyBlock(); rig.leftHandRenderer.GetPropertyBlock(block);
            Check(block.GetFloat("_Visibility") > 0f && block.GetFloat("_Visibility") < 1f, "Hand tracking loss fades gradually");
            rig.ApplyPose(1f);
            Check(!rig.leftHandRenderer.enabled && rig.rightHandRenderer.enabled, "Tracking loss hides only the affected hand");
            Flags(4 | 8 | 16); Set("poseSeq", (ushort)3); receive.Invoke(net, new[] { packet }); rig.ApplyPose(1f);
            Check(rig.leftHandRenderer.enabled, "Recovered tracking restores the hand");
            for (int i = 0; i < 40; i++) rig.ApplyPose(0.1f);
            Check(rig.leftHandRenderer.enabled, "Stationary people remain visible");
            net._head.gameObject.SetActive(false); rig.ApplyPose(1f);
            Check(!rig.upperBody.enabled && !rig.rightHandRenderer.enabled, "Inactive head anchor dissolves the ghost");
            net._head.gameObject.SetActive(true);
            net._head.SetPositionAndRotation(WaterAvatarRig.RestHead, Quaternion.identity);
            net._leftHand.position = new Vector3(-0.34f, 1.19f, 0.06f);
            net._rightHand.position = new Vector3(0.36f, 1.12f, 0.02f);
            rig.ApplyPose(1f); rig.ApplyPose(1f);
            RenderChecks(go);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(managerObject); }
        PresenceLifecycle();
        return Save("runtime-validation.json", new { passed = Passed, frame = Time.frameCount, note = "Injected NetSync packets and physical-presence lifecycle; no live network or HMD test." });
    }

    private static void PresenceLifecycle()
    {
        var managerObject = new GameObject("Validation Manager");
        managerObject.SetActive(false);
        NetSyncManager manager = managerObject.AddComponent<NetSyncManager>();
        Type type = typeof(NetSyncManager).Assembly.GetTypes().First(t => t.Name == "HumanPresenceManager");
        object driver = Activator.CreateInstance(type, new object[] { manager, false });
        try
        {
            typeof(NetSyncManager).GetField("_humanPresencePrefab", Hidden).SetValue(manager, AssetDatabase.LoadAssetAtPath<GameObject>(GhostAvatarBuilder.PresencePath));
            type.GetMethod("HandleAvatarConnected").Invoke(driver, new object[] { 701 });
            var instances = (System.Collections.IDictionary)type.GetField("_presenceByClient", Hidden).GetValue(driver);
            Check(instances.Count == 1, "NetSync spawns the physical-presence prefab");
            var instance = (GameObject)instances[701];
            Vector3 physical = new Vector3(3f, 1.55f, -2f);
            type.GetMethod("SetTransformImmediate").Invoke(driver, new object[] { 701, physical, Quaternion.Euler(0, 65, 0) });
            Check(Vector3.Distance(instance.transform.position, physical) < 0.001f && Mathf.Abs(instance.transform.eulerAngles.y - 65f) < 0.01f, "Presence follows the physical HMD pose and yaw");
            type.GetMethod("HandleAvatarConnected").Invoke(driver, new object[] { manager.ClientNo });
            Check(instances.Count == 1, "Presence is not spawned for the local user");
            type.GetMethod("HandleAvatarDisconnected").Invoke(driver, new object[] { 701 });
            Check(instances.Count == 0, "Presence is removed on disconnect");
        }
        finally
        {
            type.GetMethod("CleanupAll").Invoke(driver, null);
            Object.DestroyImmediate(managerObject);
        }
    }

    private static void RenderChecks(GameObject go)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        Camera camera = new GameObject("Validation Camera").AddComponent<Camera>();
        camera.enabled = false; camera.cullingMask = 1 << 31;
        camera.orthographic = true; camera.orthographicSize = 0.75f;
        camera.nearClipPlane = 0.01f; camera.farClipPlane = 20f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var texture = new RenderTexture(700, 900, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(700, 900, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = texture;
            Color[] Capture(float distance, string name)
            {
                camera.transform.SetPositionAndRotation(new Vector3(0, 1.25f, distance), Quaternion.Euler(0, 180, 0));
                camera.Render(); RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 700, 900), 0, 0); pixels.Apply();
                Directory.CreateDirectory(Output); File.WriteAllBytes(Output + "/" + name, pixels.EncodeToPNG());
                return pixels.GetPixels();
            }
            Color[] far = Capture(3f, "ghost-far-alpha.png");
            Color[] near = Capture(0.6f, "ghost-near-alpha.png");
            Check(far.Count(c => c.a > 0.01f && c.a < 0.5f) > 1000, "Distant ghost has translucent pixel coverage");
            Check(near.Sum(c => (double)c.a) > far.Sum(c => (double)c.a) * 1.4, "Nearby ghost is visibly stronger than distant ghost");
            // Small facial surfaces overlap; their composited alpha exceeds one surface's opacity.
            Check(near.Max(c => c.a) < 0.8f && near[0].a < 0.01f, "MR render preserves transparent background without opaque ghost pixels");
            camera.backgroundColor = new Color(0.025f, 0.038f, 0.055f, 1);
            camera.orthographic = false; camera.fieldOfView = 37f;
            camera.transform.position = new Vector3(0.8f, 1.5f, 2.3f);
            camera.transform.LookAt(new Vector3(0, 1.27f, 0));
            camera.Render(); RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 700, 900), 0, 0); pixels.Apply();
            File.WriteAllBytes(Output + "/ghost-avatar-preview.png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(camera.gameObject); texture.Release();
            Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels);
        }
    }

    private static string Save(string name, object report)
    {
        Directory.CreateDirectory(Output);
        string json = JsonConvert.SerializeObject(report, Formatting.Indented);
        File.WriteAllText(Output + "/" + name, json);
        return json;
    }
}
