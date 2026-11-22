namespace Game.Scripts.Game.Gameplay.Services.ObjectFactory
{
    public interface IObjectPool<T> where T : UnityEngine.Object
    {
        T Get();
        void Return(T obj);
        void Clear();
        int ActiveCount { get; }
        int PoolSize { get; }
    }
}