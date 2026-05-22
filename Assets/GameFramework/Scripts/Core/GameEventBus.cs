using System;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 全局事件中心：用于关卡管理、任务系统、UI 等跨系统通信。
    /// 你可以把它看作一个轻量消息总线，避免系统间直接硬引用。
    /// </summary>
    public static class GameEventBus
    {
        /// <summary>
        /// 关卡开始事件（参数：关卡 ID）。
        /// </summary>
        public static event Action<string> OnLevelStarted;

        /// <summary>
        /// 关卡完成事件（参数：关卡 ID）。
        /// </summary>
        public static event Action<string> OnLevelCompleted;

        /// <summary>
        /// 关卡失败事件（参数：关卡 ID）。
        /// </summary>
        public static event Action<string> OnLevelFailed;

        /// <summary>
        /// 实体死亡事件（参数：实体对象）。
        /// </summary>
        public static event Action<GameObject> OnActorDied;

        /// <summary>
        /// 关卡触发器进入事件（参数：触发器 ID，进入者）。
        /// </summary>
        public static event Action<string, GameObject> OnLevelTriggerEntered;

        /// <summary>
        /// 怪物生成事件（参数：怪物对象）。
        /// </summary>
        public static event Action<GameObject> OnMonsterSpawned;

        public static void RaiseLevelStarted(string levelId) => OnLevelStarted?.Invoke(levelId);

        public static void RaiseLevelCompleted(string levelId) => OnLevelCompleted?.Invoke(levelId);

        public static void RaiseLevelFailed(string levelId) => OnLevelFailed?.Invoke(levelId);

        public static void RaiseActorDied(GameObject actor) => OnActorDied?.Invoke(actor);

        public static void RaiseLevelTriggerEntered(string triggerId, GameObject actor) => OnLevelTriggerEntered?.Invoke(triggerId, actor);

        public static void RaiseMonsterSpawned(GameObject monster) => OnMonsterSpawned?.Invoke(monster);
    }
}
