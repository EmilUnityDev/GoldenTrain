using SpinSystem.Interfaces;
using System;

namespace SpinSystem.States
{
    public class IdleState : ISpinState
    {
        public event Action OnCompleted;

        public void Enter()
        {
        }
        public void Exit() { } 
    }
}