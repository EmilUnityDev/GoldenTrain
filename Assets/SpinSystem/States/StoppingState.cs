using System;
using System.Collections;
using Game.Scripts.Utils;
using SpinSystem.Interfaces;
using UnityEngine;

namespace SpinSystem.States
{
    public class StoppingState : ISpinState
    {
        public event Action OnCompleted;
        private readonly float _duration;
        private readonly Coroutines _coroutines;

        public StoppingState(float duration, Coroutines coroutines)
        {
            _duration = duration;
            _coroutines = coroutines;
        }

        public void Enter()
        {
            _coroutines.StartCoroutine(Delay());
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(_duration);
            OnCompleted?.Invoke();
        }

        public void Exit() { }
    }
}