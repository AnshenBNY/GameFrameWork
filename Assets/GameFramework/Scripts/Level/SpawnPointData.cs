using System;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 通用刷新点数据（用于怪物与物资）。
    /// </summary>
    [Serializable]
    public class SpawnPointData
    {
        public string spawnId;
        public Vector3 position;
        public Vector3 eulerAngles;

        [Tooltip("可选预制体，怪物点可放怪物，物资点可放道具箱。")]
        public GameObject prefab;
    }
}
