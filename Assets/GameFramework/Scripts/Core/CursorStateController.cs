using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 运行时鼠标状态控制：
    /// - 左/右键按下：锁定并隐藏鼠标
    /// - Esc：释放并显示鼠标
    /// </summary>
    [DisallowMultipleComponent]
    public class CursorStateController : MonoBehaviour
    {
        [Header("输入配置")]
        [SerializeField] private KeyCode unlockKey = KeyCode.Escape;
        [SerializeField] private bool lockOnLeftClick = true;
        [SerializeField] private bool lockOnRightClick = true;

        [Header("启动状态")]
        [SerializeField] private bool lockCursorOnStart = true;

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
            else
            {
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(unlockKey))
            {
                SetCursorLocked(false);
                return;
            }

            bool leftPressed = lockOnLeftClick && Input.GetMouseButtonDown(0);
            bool rightPressed = lockOnRightClick && Input.GetMouseButtonDown(1);
            if (leftPressed || rightPressed)
            {
                SetCursorLocked(true);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
