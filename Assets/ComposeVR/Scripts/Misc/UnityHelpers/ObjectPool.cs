using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComposeVR
{

    public class ObjectPool : MonoBehaviour
    {
        public Poolable prefab;

        [Tooltip("The number of objects in the pool")]
        public int startSize;

        [Tooltip("How much to expand the pool by if more objects are needed")]
        public int expandAmount;

        private Stack<Poolable> pool;

        void Awake()
        {
            getPool();
        }

        private Stack<Poolable> getPool()
        {
            if (pool == null)
            {
                pool = new Stack<Poolable>(startSize);
                ExpandPool(startSize);
            }
            return pool;
        }

        public Poolable GetObject()
        {
            if (getPool().Count == 0)
            {
                ExpandPool(expandAmount);
            }

            Poolable obj = pool.Pop();
            obj.Initialize(this);
            return obj;
        }

        public Poolable GetObject(Vector3 position, Quaternion rotation)
        {
            Poolable obj = GetObject();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void ReturnObject(Poolable obj)
        {
            pool.Push(obj);
        }

        private void ExpandPool(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                pool.Push(NewObject());
            }
        }

        private Poolable NewObject()
        {
            Poolable newObj = Instantiate(prefab) as Poolable;
            newObj.gameObject.SetActive(false);
            return newObj;
        }
    }

}
