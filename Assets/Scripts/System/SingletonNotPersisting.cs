﻿using UnityEngine;

namespace Scripts.System
{
    public class SingletonNotPersisting<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        
        public static T Instance
        {
            get
            {
                if (instance) return instance;
                
                instance = FindObjectOfType<T> ();
                    
                if (instance) return instance;
                    
                GameObject obj = new()
                {
                    name = nameof(T)
                };
                
                instance = obj.AddComponent<T> ();
                return instance;
            }
        }
    }
}