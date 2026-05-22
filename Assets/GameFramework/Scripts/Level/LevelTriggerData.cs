using System;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡触发器预设数据。
    /// </summary>
    [Serializable]
    public class LevelTriggerData
    {
        public string triggerId;
        public Vector3 position;
        public Vector3 size = new Vector3(3f, 3f, 3f);
    }
}
