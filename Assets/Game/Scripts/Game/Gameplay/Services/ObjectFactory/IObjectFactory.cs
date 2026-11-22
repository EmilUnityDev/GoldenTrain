using UnityEngine;

namespace Game.Scripts.Game.Gameplay.Services.ObjectFactory
{
    public interface IObjectFactory<T> where T : Object
    {
        T Create(Transform parent = null);
        void Destroy(T obj);
    }
}