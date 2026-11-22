using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Game.Scripts.Game.Gameplay.Services.ObjectFactory
{
    public class LimitedObjectPool<T> : IObjectPool<T> where T : Object
    {
        private readonly IObjectFactory<T> _factory;
        private readonly Queue<T> _inactivePool = new();
        private readonly HashSet<T> _activeObjects = new();
        private readonly int _maxActiveCount;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public LimitedObjectPool(IObjectFactory<T> factory, int maxActiveCount = 50,
            Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory;
            _maxActiveCount = maxActiveCount;
            _onGet = onGet;
            _onRelease = onRelease;
        }

        public T Get()
        {
            T obj;
            if (_inactivePool.Count > 0)
                obj = _inactivePool.Dequeue();
            else
                obj = _factory.Create();

            if (obj != null)
            {
                _activeObjects.Add(obj);
                _onGet?.Invoke(obj);
            }

            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null || !_activeObjects.Remove(obj)) return;

            _inactivePool.Enqueue(obj);
            _onRelease?.Invoke(obj);
        }

        public void Clear()
        {
            var activeToDestroy = _activeObjects.Where(o => o != null).ToList();
            foreach (var obj in activeToDestroy)
                _factory.Destroy(obj);
            _activeObjects.Clear();

            var inactiveToDestroy = new List<T>();
            while (_inactivePool.Count > 0)
            {
                var obj = _inactivePool.Dequeue();
                if (obj != null)
                    inactiveToDestroy.Add(obj);
            }

            foreach (var obj in inactiveToDestroy)
                _factory.Destroy(obj);
            _inactivePool.Clear();
        }


        public int ActiveCount => _activeObjects.Count(o => o != null);
        public int PoolSize => _activeObjects.Count(o => o != null) + _inactivePool.Count(o => o != null);

        public HashSet<T> GetActiveObjects()
        {
            return _activeObjects;
        }

        public Queue<T> GetInactiveObjects()
        {
            return _inactivePool;
        }
    }
}