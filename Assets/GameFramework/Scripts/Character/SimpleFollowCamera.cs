using UnityEngine;

namespace GameFramework.Character
{
    /// <summary>
    /// 简单第三人称跟随相机：
    /// 以目标角色为中心，保持固定偏移并平滑跟随。
    /// </summary>
    public class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float lookAtHeight = 1.5f;

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSpeed);

            Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        }
    }
}
