using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 运行时关卡触发器：
    /// 挂在由 LevelManager 动态创建的触发器对象上，
    /// 在玩家/实体进入后向事件总线广播。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LevelRuntimeTrigger : MonoBehaviour
    {
        [SerializeField] private string triggerId;
        [SerializeField] private string requiredTag = "Player";

        public void Initialize(string id)
        {
            triggerId = id;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            GameEventBus.RaiseLevelTriggerEntered(triggerId, other.gameObject);
        }
    }
}
