using GameFramework.AI;
using GameFramework.Combat;
using GameFramework.Core;
using GameFramework.Stats;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡内容生成：地图、物资、怪物、触发器。
    /// </summary>
    public class LevelSpawnService
    {
        public GameObject SpawnMap(LevelDefinition level, Transform root)
        {
            if (level == null || level.mapPrefab == null || root == null)
            {
                return null;
            }

            return Object.Instantiate(level.mapPrefab, root);
        }

        public void SpawnItems(LevelDefinition level, Transform root, LevelRuntimeRegistry registry)
        {
            if (level == null || root == null || registry == null)
            {
                return;
            }

            foreach (SpawnPointData spawn in level.itemSpawnPoints)
            {
                if (spawn.prefab == null)
                {
                    continue;
                }

                GameObject go = Object.Instantiate(
                    spawn.prefab,
                    spawn.position,
                    Quaternion.Euler(spawn.eulerAngles),
                    root);
                registry.RegisterItem(go);
            }
        }

        public void SpawnMonsters(LevelDefinition level, Transform root, LevelRuntimeRegistry registry)
        {
            if (level == null || root == null || registry == null)
            {
                return;
            }

            foreach (SpawnPointData spawn in level.monsterSpawnPoints)
            {
                if (spawn.prefab == null)
                {
                    continue;
                }

                GameObject go = Object.Instantiate(
                    spawn.prefab,
                    spawn.position,
                    Quaternion.Euler(spawn.eulerAngles),
                    root);
                EnsureMonsterRuntimeComponents(go);

                registry.RegisterMonster(go);
                GameEventBus.RaiseMonsterSpawned(go);
            }
        }

        public void CreateTriggers(LevelDefinition level, Transform root, LevelRuntimeRegistry registry)
        {
            if (level == null || root == null || registry == null)
            {
                return;
            }

            foreach (LevelTriggerData triggerData in level.triggers)
            {
                GameObject triggerGo = new GameObject($"Trigger_{triggerData.triggerId}");
                triggerGo.transform.SetParent(root);
                triggerGo.transform.position = triggerData.position;

                BoxCollider box = triggerGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = triggerData.size;

                LevelRuntimeTrigger runtimeTrigger = triggerGo.AddComponent<LevelRuntimeTrigger>();
                runtimeTrigger.Initialize(triggerData.triggerId);

                registry.RegisterTrigger(triggerGo);
            }
        }

        private static void EnsureMonsterRuntimeComponents(GameObject monster)
        {
            if (monster == null)
            {
                return;
            }

            if (monster.GetComponent<ActorStatsComponent>() == null)
            {
                monster.AddComponent<ActorStatsComponent>();
            }

            if (monster.GetComponent<EnemySimpleHealthBar>() == null)
            {
                monster.AddComponent<EnemySimpleHealthBar>();
            }

            if (monster.GetComponent<FactionComponent>() == null)
            {
                monster.AddComponent<FactionComponent>();
            }

            FactionComponent faction = monster.GetComponent<FactionComponent>();
            if (faction != null)
            {
                faction.SetFaction(FactionType.Enemy);
            }

            Animator animator = monster.GetComponent<Animator>();
            if (monster.GetComponent<BasicEnemyController>() == null)
            {
                monster.AddComponent<BasicEnemyController>();
            }

            if (animator == null)
            {
                // 无 Animator 的敌人沿用最小功能：仅有血量与阵营，不做动画驱动。
            }
        }
    }
}
