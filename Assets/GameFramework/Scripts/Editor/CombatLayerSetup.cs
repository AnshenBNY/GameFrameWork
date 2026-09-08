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
            SerializedProperty tags = tagManager.FindProperty("tags");

            EnsureTagExists(tags, "Player");
            EnsureTagExists(tags, "Enemy");

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

        private static void EnsureTagExists(SerializedProperty tags, string tagName)
        {
            if (tags == null || string.IsNullOrEmpty(tagName))
            {
                return;
            }

            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty element = tags.GetArrayElementAtIndex(i);
                if (element.stringValue == tagName)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(0);
            tags.GetArrayElementAtIndex(0).stringValue = tagName;
        }
    }
}
