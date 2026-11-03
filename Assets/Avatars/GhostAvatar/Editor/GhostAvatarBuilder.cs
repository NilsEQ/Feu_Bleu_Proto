using System;
using AncientFuture.WaterAvatar;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FeuBleu.GhostAvatar.Editor
{
    /// <summary>Creates independent ghost prefabs while reusing the water avatar's pose rig.</summary>
    public static class GhostAvatarBuilder
    {
        public const string Root = "Assets/Avatars/GhostAvatar";
        public const string RemotePath = Root + "/Prefabs/GhostAvatar_Remote.prefab";
        public const string PresencePath = Root + "/Prefabs/GhostAvatar_Presence.prefab";
        private const string SourcePath = "Assets/Avatars/WaterAvatar/Prefabs/WaterAvatar_Remote.prefab";

        [MenuItem("Tools/Feu Bleu/Rebuild Ghost Avatars")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding ghost avatars.");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Meshes");
            EnsureFolder(Root + "/Prefabs");
            Shader shader = Shader.Find("Feu Bleu/Ghost Silhouette");
            if (shader == null) throw new InvalidOperationException("Ghost Silhouette shader has not imported.");

            Material remoteMaterial = SaveMaterial("GhostSilhouette", shader, CompareFunction.LessEqual);
            Material presenceMaterial = SaveMaterial("GhostPresence", shader, CompareFunction.Always);
            GameObject source = PrefabUtility.LoadPrefabContents(SourcePath);
            try
            {
                source.name = "GhostAvatar_Remote";
                WaterAvatarRig rig = source.GetComponent<WaterAvatarRig>();
                if (rig == null) throw new InvalidOperationException("The source prefab requires WaterAvatarRig.");
                if (rig.droplets != null)
                {
                    Object.DestroyImmediate(rig.droplets.gameObject);
                    rig.droplets = null;
                }
                foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
                    ConfigureRenderer(renderer, remoteMaterial);
                foreach (Collider collider in source.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);
                PrefabUtility.SaveAsPrefabAsset(source, RemotePath);
                BuildPresence(rig, presenceMaterial);
            }
            finally { PrefabUtility.UnloadPrefabContents(source); }
            AssetDatabase.SaveAssets();
            Debug.Log("Ghost remote and physical-presence prefabs rebuilt. Water avatar source assets preserved.");
        }

        private static void BuildPresence(WaterAvatarRig rig, Material material)
        {
            // HumanPresenceManager drives the root at physical HMD height, with yaw only.
            // It does not provide physical hand poses, so this prefab contains only the body.
            var mesh = Object.Instantiate(((SkinnedMeshRenderer)rig.upperBody).sharedMesh);
            mesh.name = "Ghost Presence Body";
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= WaterAvatarRig.RestHead;
            mesh.vertices = vertices;
            mesh.boneWeights = Array.Empty<BoneWeight>();
            mesh.bindposes = Array.Empty<Matrix4x4>();
            mesh.RecalculateBounds();
            string meshPath = Root + "/Meshes/GhostPresenceBody.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                mesh = existing;
            }

            var presence = new GameObject("GhostAvatar_Presence");
            try
            {
                presence.AddComponent<MeshFilter>().sharedMesh = mesh;
                ConfigureRenderer(presence.AddComponent<MeshRenderer>(), material);
                PrefabUtility.SaveAsPrefabAsset(presence, PresencePath);
            }
            finally { Object.DestroyImmediate(presence); }
        }

        private static Material SaveMaterial(string name, Shader shader, CompareFunction depthTest)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_Tint", new Color(0.58f, 0.82f, 0.94f, 1f));
            material.SetFloat("_FarOpacity", 0.07f);
            material.SetFloat("_NearOpacity", 0.22f);
            material.SetFloat("_RimOpacity", 0.16f);
            material.SetFloat("_RimPower", 2.5f);
            material.SetFloat("_NearDistance", 0.75f);
            material.SetFloat("_FarDistance", 2.5f);
            material.SetFloat("_Visibility", 1f);
            material.SetFloat("_ZTest", (float)depthTest);
            material.renderQueue = depthTest == CompareFunction.Always ? 3100 : 3000;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
