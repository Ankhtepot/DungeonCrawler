﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Fireball Games * * * PetrZavodny.com

namespace Scripts.System.ObjectPool
{
    public class ObjectPool : MonoBehaviour
    {
#pragma warning disable 649
        public List<PreSpawnSetItem> preSpawnSetItems;
        public Transform storeParent;
        private static readonly Dictionary<string, Queue<GameObject>> Pool = new Dictionary<string, Queue<GameObject>>();
#pragma warning restore 649

        private void Start()
        {
            if (preSpawnSetItems.Any())
            {
                SpawnPreSpawnItems();
            }
        }

        public GameObject GetFromPool(GameObject go, Vector3 position, Quaternion rotation, GameObject parent = null) 
        {
            if (Pool.ContainsKey(go.name) && Pool[go.name].Any())
            {
                var removedObject = Pool[go.name].Dequeue();
                // ReSharper disable once PossibleNullReferenceException
                var removedObjectTransform = removedObject.transform;
                removedObjectTransform.position = position;
                removedObjectTransform.rotation = rotation;
                removedObjectTransform.parent = parent != null ? parent.transform : storeParent;
                removedObject.gameObject.SetActive(true);
                return ProcessInterfaces(removedObject);
            }
        
            return InstantiateNewPoolObject(go, position, rotation, parent);
        }

        public void ReturnToPool(GameObject returningObject)
        {
            if (!Pool.ContainsKey(returningObject.name))
            {
                Pool.Add(returningObject.name, new Queue<GameObject>());
            }

            var returningObjectTransform = returningObject.transform;
            returningObjectTransform.position = transform.position;
            returningObjectTransform.parent = storeParent;
            returningObject.gameObject.SetActive(false);
        
            Pool[returningObject.name].Enqueue(returningObject);
        }

        private GameObject InstantiateNewPoolObject(GameObject requestedObject, Vector3 position, Quaternion rotation, GameObject parent, bool instantiateToDisabled = false) 
        {
            var newObject = Instantiate(requestedObject, position, rotation, parent != null ? parent.transform : storeParent);
            newObject.name = requestedObject.name;
            newObject = ProcessInterfaces(newObject);
            
            if (instantiateToDisabled)
            {
                newObject.SetActive(false);
            }
                
            return newObject;
        }
        
        private GameObject InstantiateNewPoolObject(GameObject objectToInstantiate, bool instantiateToDisabled = false) 
        {
            var objectToInstantiateTransform = objectToInstantiate.transform;
            return InstantiateNewPoolObject(
                objectToInstantiate, objectToInstantiateTransform.position, objectToInstantiateTransform.rotation, gameObject, instantiateToDisabled
            );
        }
        
        private GameObject ProcessInterfaces(GameObject target)
        {
            var needy = target.GetComponents<IPoolNeedy>();
            if (needy.Any())
            { 
                foreach (var poolNeedy in needy)
                {
                    if (!poolNeedy.pool)
                    {
                        poolNeedy.pool = this;
                    }
                }
            }

            var initializable = target.GetComponent<IPoolInitializable>();
            initializable?.Initialize();

            return target;
        }
        
        private void SpawnPreSpawnItems()
        {
            preSpawnSetItems.ForEach(item =>
            {
                if (!item.prefabGameObject || item.howMany <= 0) return;

                for (int i = 0; i < item.howMany; i++)
                {
                    var newObject = InstantiateNewPoolObject(item.prefabGameObject, true);
                    ReturnToPool(newObject);
                }
            });
        }

        [Serializable]
        public class PreSpawnSetItem
        {
            public GameObject prefabGameObject;
            public int howMany;
        }
    }
}
