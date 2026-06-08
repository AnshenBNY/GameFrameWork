using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 监听关卡关键事件并驱动最小胜负逻辑。
    /// 目前实现：玩家死亡 -> 关卡失败。
    /// </summary>
    public class LevelResultListener : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private GameObject player;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void ResolveDependencies()
        {
            if (levelManager == null)
            {
                levelManager = GetComponent<LevelManager>();
            }

            RuntimeContext context = RuntimeContext.Instance;
            if (context == null)
            {
                return;
            }

            if (levelManager == null)
            {
                context.TryGetLevelManager(out levelManager);
            }

            if (player == null)
            {
                player = context.Player;
            }
        }

        private void OnEnable()
        {
            GameEventBus.OnActorDied += HandleActorDied;
        }

        private void OnDisable()
        {
            GameEventBus.OnActorDied -= HandleActorDied;
        }

        private void HandleActorDied(GameObject actor)
        {
            if (actor == null || levelManager == null || player == null)
            {
                return;
            }

            // 支持在根节点或子节点上抛死亡事件。
            Transform actorRoot = actor.transform.root;
            Transform playerRoot = player.transform.root;
            if (actorRoot != playerRoot)
            {
                return;
            }

            levelManager.FailLevel();
        }
    }
}
