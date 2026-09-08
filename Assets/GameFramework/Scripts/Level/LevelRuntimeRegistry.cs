using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡运行时对象登记与清理。
    /// </summary>
    public class LevelRuntimeRegistry
    {
        private GameObject _spawnedMap;
        private readonly List<GameObject> _spawnedMonsters = new List<GameObject>();
        private readonly List<GameObject> _spawnedItems = new List<GameObject>();
        private readonly List<GameObject> _runtimeTriggers = new List<GameObject>();

        public int ActiveMonsterCount => _spawnedMonsters.Count;
        public string LastDiedActorName { get; private set; } = "(none)";

        public void ResetTrackingState()
        {
            LastDiedActorName = "(none)";
        }

        public void SetSpawnedMap(GameObject map)
        {
            _spawnedMap = map;
        }

        public void RegisterItem(GameObject item)
        {
            if (item != null)
            {
                _spawnedItems.Add(item);
            }
        }

        public void RegisterMonster(GameObject monster)
        {
            if (monster != null)
            {
                _spawnedMonsters.Add(monster);
            }
        }

        public void RegisterTrigger(GameObject trigger)
        {
            if (trigger != null)
            {
                _runtimeTriggers.Add(trigger);
            }
        }

        public string GetMonsterDebugSnapshot()
        {
            if (_spawnedMonsters.Count == 0)
            {
                return "(empty)";
            }

            List<string> names = new List<string>(_spawnedMonsters.Count);
            for (int i = 0; i < _spawnedMonsters.Count; i++)
            {
                GameObject go = _spawnedMonsters[i];
                names.Add(go != null ? go.name : "null");
            }

            return string.Join(", ", names);
        }

        public bool TryTrackActorDeath(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            LastDiedActorName = actor.name;

            GameObject deadRoot = actor.transform.root.gameObject;
            bool removed = _spawnedMonsters.Remove(actor);
            if (!removed)
            {
                removed = _spawnedMonsters.Remove(deadRoot);
            }

            if (!removed)
            {
                for (int i = _spawnedMonsters.Count - 1; i >= 0; i--)
                {
                    GameObject monster = _spawnedMonsters[i];
                    if (monster == null)
                    {
                        _spawnedMonsters.RemoveAt(i);
                        continue;
                    }

                    if (actor.transform.IsChildOf(monster.transform) || deadRoot.transform.IsChildOf(monster.transform))
                    {
                        _spawnedMonsters.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            }

            return removed;
        }

        public void ClearAll()
        {
            if (_spawnedMap != null)
            {
                Object.Destroy(_spawnedMap);
                _spawnedMap = null;
            }

            DestroyAll(_spawnedMonsters);
            DestroyAll(_spawnedItems);
            DestroyAll(_runtimeTriggers);
            ResetTrackingState();
        }

        private static void DestroyAll(List<GameObject> objects)
        {
            foreach (GameObject go in objects)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }

            objects.Clear();
        }
    }
}
