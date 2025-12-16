using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Queue<T> pool;

    public ObjectPool(T prefab, int initialCount, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;

        pool = new Queue<T>();
        for (int i = 0; i < initialCount; i++)
        {
            var obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            return obj;
        }
        else
        {
            var obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }

    public List<T> Get(int n)
    {
        List<T> objs = new List<T>();
        for (int i=0; i<n; i++)
        {
            if (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                objs.Add(obj);
            }
            else
            {
                var obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
                obj.gameObject.SetActive(false);
                objs.Add(obj);
            }   
        }
        return objs;
    }

    public void Return(T obj)
    {
        pool.Enqueue(obj);
    }
}
