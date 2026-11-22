using System;
using SpinSystem.Interfaces;
using System.Collections;
using Game.Scripts.Utils;
using UnityEngine;

namespace SpinSystem.States
{
    public class WinState : ISpinState
    {
        public event Action OnCompleted;
        private readonly int _totalWin;
        private readonly Coroutines _coroutines;

        public WinState(int totalWin, Coroutines coroutines)
        {
            _totalWin = totalWin;
            _coroutines = coroutines;
        }

        public void Enter()
        {
            Debug.Log($"TOTAL WIN: {_totalWin}");
            _coroutines.StartCoroutine(Delay());
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(0.1f);
            OnCompleted?.Invoke();
        }

        public void Exit() { }
    }
}