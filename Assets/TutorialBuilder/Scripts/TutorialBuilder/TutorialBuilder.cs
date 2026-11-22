using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorials
{
    public sealed class TutorialBuilder : MonoBehaviour
    {
        [SerializeField] private TutorialHierarchy _hierarchy;
        [SerializeField] private List<TutorialScreen> _screens = new List<TutorialScreen>();
        [SerializeField, Min(0)] private int _currentIndex;

        public IReadOnlyList<TutorialScreen> Screens => _screens;
        public int CurrentIndex => _currentIndex;
        public TutorialHierarchy Hierarchy => _hierarchy;

        public bool CanGoPrev => _screens.Count > 0 && _currentIndex > 0;
        public bool CanGoNext => _screens.Count > 0 && _currentIndex < _screens.Count - 1;

        public void SetActiveOnly(int index)
        {
            if (_screens.Count == 0) { _currentIndex = 0; return; }
            index = Mathf.Clamp(index, 0, _screens.Count - 1);
            _currentIndex = index;

            for (int i = 0; i < _screens.Count; i++)
            {
                var s = _screens[i];
                if (s != null) s.gameObject.SetActive(i == _currentIndex);
            }
        }

        public void GoFirst() => SetActiveOnly(0);
        public void GoLast()  => SetActiveOnly(_screens.Count - 1);
        public void GoPrev()  { if (CanGoPrev) SetActiveOnly(_currentIndex - 1); }
        public void GoNext()  { if (CanGoNext) SetActiveOnly(_currentIndex + 1); }
        
        public void InsertAt(int index, TutorialScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            index = Mathf.Clamp(index, 0, _screens.Count);
            _screens.Insert(index, screen);
            SetActiveOnly(index);
        }
        
        public void RemoveAt(int index)
        {
            if (_screens.Count == 0) return;
            index = Mathf.Clamp(index, 0, _screens.Count - 1);
            _screens.RemoveAt(index);
            SetActiveOnly(Mathf.Clamp(index, 0, _screens.Count - 1));
        }
        
        public void Compact()
        {
            _screens.RemoveAll(s => s == null);
            SetActiveOnly(Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _screens.Count - 1)));
        }
    }
}
