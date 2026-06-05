using GameFramework.Level;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// 生成最小可通关关卡配置模板。
    /// </summary>
    public static class LevelTemplateBuilder
    {
        private const string AssetRoot = "Assets/GameFramework/Generated";

        [MenuItem("Tools/GameFramework/Create Minimal Level Template")]
        public static void CreateMinimalLevelTemplate()
        {
            EnsureFolder(AssetRoot);
            GameObject defaultEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{AssetRoot}/GF_BasicEnemy.prefab");

            string levelAssetPath = $"{AssetRoot}/GF_MinimalLevel.asset";
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(levelAssetPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, levelAssetPath);
            }

            level.levelId = "gf_minimal_001";
            level.displayName = "GF Minimal";
            level.mapPrefab = null;

            level.triggers.Clear();
            level.triggers.Add(new LevelTriggerData
            {
                triggerId = "start_zone",
                position = new Vector3(0f, 1f, 0f),
                size = new Vector3(6f, 2f, 6f)
            });

            level.itemSpawnPoints.Clear();

            level.monsterSpawnPoints.Clear();
            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "enemy_a",
                position = new Vector3(0f, 0f, 14f),
                eulerAngles = Vector3.zero,
                prefab = defaultEnemyPrefab
            });
            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "enemy_b",
                position = new Vector3(4f, 0f, 18f),
                eulerAngles = Vector3.zero,
                prefab = defaultEnemyPrefab
            });
            level.monsterSpawnPoints.Add(new SpawnPointData
            {
                spawnId = "enemy_c",
                position = new Vector3(-4f, 0f, 18f),
                eulerAngles = Vector3.zero,
                prefab = defaultEnemyPrefab
            });

            level.phases.Clear();
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_reach_start",
                phaseType = LevelPhaseType.ReachTrigger,
                requiredTriggerId = "start_zone",
                requiredWaveCount = 1
            });
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_defend_two_waves",
                phaseType = LevelPhaseType.DefendWaves,
                requiredTriggerId = string.Empty,
                requiredWaveCount = 2
            });
            level.phases.Add(new LevelPhaseData
            {
                phaseId = "phase_complete",
                phaseType = LevelPhaseType.Complete,
                requiredTriggerId = string.Empty,
                requiredWaveCount = 1
            });

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = level;

            EditorUtility.DisplayDialog(
                "GameFramework",
                "已生成最小关卡模板：Assets/GameFramework/Generated/GF_MinimalLevel.asset\n\n如已生成 GF_BasicEnemy.prefab，将自动填入怪物刷新点。",
                "OK");
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
