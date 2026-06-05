using GameFramework.AI;
using GameFramework.Combat;
using GameFramework.Stats;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 构建最小可用敌人预制体。
    /// </summary>
    public static class EnemyPrefabBuilder
    {
        private const string OutputFolder = "Assets/GameFramework/Generated";
        private const string EnemyPrefabPath = OutputFolder + "/GF_BasicEnemy.prefab";
        private const string EnemyMaterialPath = OutputFolder + "/GF_Enemy.mat";

        [MenuItem("Tools/GameFramework/Create Basic Enemy Prefab")]
        public static void CreateBasicEnemyPrefab()
        {
            EnsureFolder(OutputFolder);
            CombatLayerSetup.EnsureProjectLayers();

            Material mat = GetOrCreateEnemyMaterial();

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "GF_BasicEnemy";
            enemy.transform.position = Vector3.zero;

            CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.4f;

            Rigidbody rb = enemy.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            MeshRenderer renderer = enemy.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }

            ActorStatsComponent stats = enemy.AddComponent<ActorStatsComponent>();
            SerializedObject statsSo = new SerializedObject(stats);
            statsSo.FindProperty("baseAttributes.baseHealth").floatValue = 100f;
            statsSo.FindProperty("baseAttributes.moveSpeed").floatValue = 3.5f;
            statsSo.FindProperty("baseAttributes.armor").floatValue = 8f;
            statsSo.ApplyModifiedPropertiesWithoutUndo();
            enemy.AddComponent<EnemySimpleHealthBar>();

            FactionComponent faction = enemy.AddComponent<FactionComponent>();
            SerializedObject factionSo = new SerializedObject(faction);
            factionSo.FindProperty("faction").enumValueIndex = (int)FactionType.Enemy;
            factionSo.ApplyModifiedPropertiesWithoutUndo();

            enemy.AddComponent<BasicEnemyController>();

            if (CombatLayers.Enemy >= 0)
            {
                CombatLayerSetup.SetLayerRecursively(enemy, CombatLayers.Enemy);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            EditorUtility.DisplayDialog("GameFramework", $"已生成敌人预制体：{EnemyPrefabPath}", "OK");
        }

        private static Material GetOrCreateEnemyMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(EnemyMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                color = new Color(0.85f, 0.25f, 0.25f, 1f)
            };
            AssetDatabase.CreateAsset(material, EnemyMaterialPath);
            return material;
        }

        private static void EnsureFolder(string fullPath)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
            {
                return;
            }

            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
