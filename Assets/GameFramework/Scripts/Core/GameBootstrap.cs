using GameFramework.Level;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 游戏启动入口（最小骨架）：
    /// 将其挂在一个常驻对象上，用于驱动首关加载。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private LevelDefinition firstLevel;

        private void Start()
        {
            if (levelManager != null && firstLevel != null)
            {
                levelManager.StartLevel(firstLevel);
            }
        }
    }
}
