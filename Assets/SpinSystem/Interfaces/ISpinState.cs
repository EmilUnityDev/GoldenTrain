using System;

namespace SpinSystem.Interfaces
{
    public interface ISpinState
    {
        event Action OnCompleted;
        void Enter();
        void Exit();
    }
}