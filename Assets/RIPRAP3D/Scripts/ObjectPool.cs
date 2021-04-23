using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// When we move to a new scene for continuous generation, we destroy all objects and create new. However, the destroy process may not catch up the frame refresh thus bring memory issue.
// Instead we use an object pooling technique, such that when frame refreshes, the existing objects are not destroyed but hided in the pool.

// Each instance is a GameObject with its prefab ID (prototype ID)
public class Instance
{
    public int prefabID;
    public GameObject obj;
}

public class ObjectPool : ScriptableObject
{
    private GameObject[] prefabs;
    private Dictionary<int, List<Instance>> pools;
    private int[] counter; // track how many object in pool is active

    public static ObjectPool Init(GameObject[] prefabs)
    {
        var p = ScriptableObject.CreateInstance<ObjectPool>();
        p.prefabs = prefabs;
        p.pools = new Dictionary<int, List<Instance>>();
        p.counter = new int[prefabs.Length];
        
        for (int i = 0; i < prefabs.Length; i++)
        {
            p.pools[i] = new List<Instance>();
            p.counter[i] = 0;
        }

        return p;
    }

    public Instance CreateObject(int prefabID)
    {
        var pool = this.pools[prefabID];
        int active = this.counter[prefabID];
        Instance ret;

        if (active < pool.Count)
        {   // fetch existing obj
            ret = pool[active];
            pool[active].obj.SetActive(true); // display
        }
        else
        {   // create new obj
            var obj = Instantiate(this.prefabs[prefabID]);
            ret = new Instance() { prefabID = prefabID, obj = obj };
            pool.Add(ret);
        }
        this.counter[prefabID]++; // counter always increment

        return ret;
    }

    public void ReclaimAll() 
    {
        foreach (var i in this.pools.Keys)
        {
            foreach (var instance in this.pools[i])
            {
                instance.obj.SetActive(false);
            }
            this.counter[i] = 0;
        }
    }
}
