using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Game.Gameplay.Services.ObjectFactory
{
    public class ObjectFactory<T> : IObjectFactory<T> where T : Object
    {
        private readonly GameObject _prefab;
        private readonly Func<GameObject, T> _componentGetter;

        public ObjectFactory(GameObject prefab, Func<GameObject, T> componentGetter)
        {
            _prefab = prefab;
            _componentGetter = componentGetter;
        }

        public T Create(Transform parent = null)
        {
            var obj = Object.Instantiate(_prefab, parent);
            return _componentGetter(obj);
        }

        public void Destroy(T obj)
        {
            if (obj == null) return;
            if (obj is MonoBehaviour monoBehaviour && monoBehaviour.gameObject != null)
                Object.Destroy(monoBehaviour.gameObject);
            else if (obj != null)
                Object.Destroy(obj);
        }
    }
}