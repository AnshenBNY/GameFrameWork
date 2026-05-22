using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡定义资产：
    /// - 地图（场景内根对象或地图预制体）
    /// - 触发器信息
    /// - 物资刷新点
    /// - 怪物刷新点
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Level/Level Definition", fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelId = "level_001";
        public string displayName = "Tutorial";

        [Header("地图")]
        [Tooltip("可选：将地图作为一个预制体统一实例化。")]
        public GameObject mapPrefab;

        [Header("触发器")]
        public List<LevelTriggerData> triggers = new List<LevelTriggerData>();

        [Header("物资刷新点")]
        public List<SpawnPointData> itemSpawnPoints = new List<SpawnPointData>();

        [Header("怪物刷新点")]
        public List<SpawnPointData> monsterSpawnPoints = new List<SpawnPointData>();

        [Header("关卡阶段（线性流程）")]
        public List<LevelPhaseData> phases = new List<LevelPhaseData>();
    }
}
