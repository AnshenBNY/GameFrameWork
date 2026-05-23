using GameFramework.AI;
using GameFramework.Combat;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 确保战斗 Layer 已写入 TagManager，并为场景对象批量设置 Layer。
    /// </summary>
    public static class CombatLayerSetup
    {
        [MenuItem("Tools/GameFramework/Setup Combat Layers")]
        public static void SetupCombatLayersMenu()
        {
            EnsureProjectLayers();
            EditorUtility.DisplayDialog(
                "GameFramework",
                "已确保 Layer 存在：Player / Enemy / Environment",
                "OK");
        }

        public static void EnsureProjectLayers()
        {
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
            {
                return;
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            SetLayerName(layers, 6, CombatLayers.PlayerLayerName);
            SetLayerName(layers, 7, CombatLayers.EnemyLayerName);
            SetLayerName(layers, 8, CombatLayers.EnvironmentLayerName);
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null || layer < 0)
            {
                return;
            }

            go.layer = layer;
            Transform root = go.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i).gameObject, layer);
            }
        }

        public static void ApplySampleSceneLayers()
        {
            EnsureProjectLayers();

            int playerLayer = CombatLayers.Player;
            int enemyLayer = CombatLayers.Enemy;
            int environmentLayer = CombatLayers.Environment;

            GameObject player = GameObject.Find("Player");
            if (player != null && playerLayer >= 0)
            {
                SetLayerRecursively(player, playerLayer);
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground != null && environmentLayer >= 0)
            {
                SetLayerRecursively(ground, environmentLayer);
            }

            MonsterBrain[] monsters = Object.FindObjectsOfType<MonsterBrain>();
            for (int i = 0; i < monsters.Length; i++)
            {
                if (enemyLayer >= 0)
                {
                    SetLayerRecursively(monsters[i].gameObject, enemyLayer);
                }
            }
        }

        private static SerializedObject GetTagManager()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            return new SerializedObject(assets[0]);
        }

        private static void SetLayerName(SerializedProperty layers, int index, string layerName)
        {
            if (layers == null || index < 0 || index >= layers.arraySize)
            {
                return;
            }

            SerializedProperty element = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
            }
        }
    }
}
