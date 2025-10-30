//=====================================
// PoolingManager.cs
//=====================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class PoolingManager : MonoBehaviour
    {
        private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, GameObject> activeObjects = new Dictionary<GameObject, GameObject>();
        private Transform poolRoot;

        private void Awake()
        {
            poolRoot = new GameObject("_PooledObjects").transform;
            poolRoot.SetParent(transform);
            poolRoot.gameObject.SetActive(false);
            
            Log(Category.PoolingManager, "PoolingManager initialized");
        }

        public GameObject Get(GameObject prefab, Transform parent = null, bool activateOnGet = false)
        {
            if (prefab == null)
            {
                LogError(Category.PoolingManager, "Cannot get object from null prefab");
                return null;
            }

            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
                Log(Category.PoolingManager, $"Created new pool for: {prefab.name}");
            }

            GameObject obj;

            if (pools[prefab].Count > 0)
            {
                obj = pools[prefab].Dequeue();
                Log(Category.PoolingManager, $"Reused pooled object: {prefab.name} (Pool size: {pools[prefab].Count})");
                
                if (parent != null)
                {
                    obj.transform.SetParent(parent, false);
                }

                // Call OnReuse for objects implementing IPoolableResource
                NotifyPoolableReuse(obj);
            }
            else
            {
                obj = Instantiate(prefab, parent);
                Log(Category.PoolingManager, $"Instantiated new object: {prefab.name}");
                
                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject == null)
                {
                    pooledObject = obj.AddComponent<PooledObject>();
                }
                pooledObject.SetPrefab(prefab);
            }

            activeObjects[obj] = prefab;

            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            if (activateOnGet)
            {
                obj.SetActive(true);
            }

            return obj;
        }

        public void Recycle(GameObject obj)
        {
            if (obj == null) return;

            var pooledObject = obj.GetComponent<PooledObject>();
            if (pooledObject == null || pooledObject.Prefab == null)
            {
                LogWarning(Category.PoolingManager, $"Cannot recycle {obj.name} - no PooledObject component");
                return;
            }

            GameObject prefab = pooledObject.Prefab;

            if (activeObjects.ContainsKey(obj))
            {
                activeObjects.Remove(obj);
            }

            // Call OnRecycle for objects implementing IPoolableResource
            NotifyPoolableRecycle(obj);

            // Clear remaining dynamic content (text, button listeners)
            ClearDynamicContent(obj);

            obj.SetActive(false);
            obj.transform.SetParent(poolRoot, false);

            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }
            
            pools[prefab].Enqueue(obj);
            
            Log(Category.PoolingManager, $"Recycled: {prefab.name} (Pool: {pools[prefab].Count})");
        }

        /// <summary>
        /// Notify all IPoolableResource components on an object that it's being recycled
        /// </summary>
        private void NotifyPoolableRecycle(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolableResource>();
            if (poolables != null && poolables.Length > 0)
            {
                foreach (var poolable in poolables)
                {
                    poolable.OnRecycle();
                }
                Log(Category.PoolingManager, $"Notified {poolables.Length} IPoolableResource components of recycle");
            }
        }

        /// <summary>
        /// Notify all IPoolableResource components on an object that it's being reused
        /// </summary>
        private void NotifyPoolableReuse(GameObject obj)
        {
            var poolables = obj.GetComponents<IPoolableResource>();
            if (poolables != null && poolables.Length > 0)
            {
                foreach (var poolable in poolables)
                {
                    poolable.OnReuse();
                }
                Log(Category.PoolingManager, $"Notified {poolables.Length} IPoolableResource components of reuse");
            }
        }

        /// <summary>
        /// Only clears truly dynamic content (text, listeners)
        /// Does NOT touch Image.sprite - that's handled by IPoolableResource
        /// </summary>
        private void ClearDynamicContent(GameObject obj)
        {
            // Clear TextMeshProUGUI to release string memory
            var textComponents = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in textComponents)
            {
                text.text = string.Empty;
            }

            // Clear button listeners
            var button = obj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }

            // ❌ DO NOT touch Image.sprite here - IPoolableResource handles it
            // ❌ DO NOT touch CanvasGroup alpha here - IPoolableResource handles it
        }

        public void RecycleAll(List<GameObject> objects)
        {
            if (objects == null) return;

            int count = objects.Count;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                Recycle(objects[i]);
            }
            objects.Clear();
            
            Log(Category.PoolingManager, $"Recycled {count} objects");
        }

        public void RecycleAllOfType(GameObject prefab)
        {
            if (prefab == null) return;

            var objectsToRecycle = new List<GameObject>();
            
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == prefab)
                {
                    objectsToRecycle.Add(kvp.Key);
                }
            }

            foreach (var obj in objectsToRecycle)
            {
                Recycle(obj);
            }
            
            Log(Category.PoolingManager, $"Recycled all of type: {prefab.name} ({objectsToRecycle.Count})");
        }

        public void PreWarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            Log(Category.PoolingManager, $"Pre-warming: {prefab.name} x{count}");

            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, poolRoot);
                obj.SetActive(false);

                var pooledObject = obj.GetComponent<PooledObject>();
                if (pooledObject == null)
                {
                    pooledObject = obj.AddComponent<PooledObject>();
                }
                pooledObject.SetPrefab(prefab);

                pools[prefab].Enqueue(obj);
            }
        }

        public void ClearAllPools()
        {
            Log(Category.PoolingManager, "Clearing all pools");
            
            int totalDestroyed = 0;
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        // Notify poolable resources before destruction
                        NotifyPoolableRecycle(obj);
                        Destroy(obj);
                        totalDestroyed++;
                    }
                }
            }
            
            pools.Clear();
            activeObjects.Clear();
            
            Log(Category.PoolingManager, $"Destroyed {totalDestroyed} pooled objects");
        }

        /// <summary>
        /// Clears ALL pools and active objects, ensuring complete reset.
        /// Use this for story resets to prevent stale references.
        /// </summary>
        public void HardReset()
        {
            Log(Category.PoolingManager, "=== HARD RESET: Destroying all pooled and active objects ===");

            int totalDestroyed = 0;

            // Destroy all pooled objects
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        NotifyPoolableRecycle(obj);
                        Destroy(obj);
                        totalDestroyed++;
                    }
                }
            }

            // Destroy all active objects
            var activeList = new List<GameObject>(activeObjects.Keys);
            foreach (var obj in activeList)
            {
                if (obj != null)
                {
                    NotifyPoolableRecycle(obj);
                    Destroy(obj);
                    totalDestroyed++;
                }
            }

            pools.Clear();
            activeObjects.Clear();

            Log(Category.PoolingManager, $"Hard reset complete: Destroyed {totalDestroyed} total objects");
        }

        public void ClearPool(GameObject prefab)
        {
            if (prefab == null || !pools.ContainsKey(prefab)) return;

            var pool = pools[prefab];
            int count = pool.Count;
            
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            pools.Remove(prefab);
            
            Log(Category.PoolingManager, $"Cleared pool: {prefab.name} ({count} destroyed)");
        }

        public PoolStats GetStats(GameObject prefab)
        {
            var stats = new PoolStats();
            
            if (prefab != null && pools.ContainsKey(prefab))
            {
                stats.pooledCount = pools[prefab].Count;
            }

            stats.activeCount = 0;
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == prefab)
                {
                    stats.activeCount++;
                }
            }

            stats.totalCount = stats.pooledCount + stats.activeCount;
            return stats;
        }

        public struct PoolStats
        {
            public int pooledCount;
            public int activeCount;
            public int totalCount;

            public override string ToString()
            {
                return $"Active: {activeCount}, Pooled: {pooledCount}, Total: {totalCount}";
            }
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }

    public class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }

        public void SetPrefab(GameObject prefab)
        {
            Prefab = prefab;
        }
    }
}