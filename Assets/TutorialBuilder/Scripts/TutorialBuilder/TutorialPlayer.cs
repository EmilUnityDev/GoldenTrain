using System;
using UnityEngine;

namespace Tutorials
{
    public sealed class TutorialPlayer : MonoBehaviour
    {
        [SerializeField] private TutorialBuilder _builder;
        [SerializeField] private bool _autoStart = true;
        [SerializeField, Min(0)] private int _startIndex;

        public event Action<TutorialPlayer> TutorialStarted;
        public event Action<TutorialPlayer> TutorialCompleted;
        public event Action<int> PageChanged;

        public TutorialBuilder Builder => _builder;

        private void Awake()
        {
            if (_builder == null) _builder = GetComponent<TutorialBuilder>();
        }

        private void OnEnable()
        {
            if (_autoStart) StartTutorial(_startIndex);
        }

        public void StartTutorial(int index = 0)
        {
            RequireBuilder();
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _builder.Screens.Count - 1));
            _builder.SetActiveOnly(index);
            PageChanged?.Invoke(_builder.CurrentIndex);
            TutorialStarted?.Invoke(this);
        }

        public void Restart() => StartTutorial(0);

        public void Next()
        {
            RequireBuilder();
            if (!_builder.CanGoNext) return;
            _builder.GoNext();
            PageChanged?.Invoke(_builder.CurrentIndex);
        }

        public void Prev()
        {
            RequireBuilder();
            if (!_builder.CanGoPrev) return;
            _builder.GoPrev();
            PageChanged?.Invoke(_builder.CurrentIndex);
        }

        public void GoFirst()
        {
            RequireBuilder();
            _builder.GoFirst();
            PageChanged?.Invoke(_builder.CurrentIndex);
        }

        public void GoLast()
        {
            RequireBuilder();
            _builder.GoLast();
            PageChanged?.Invoke(_builder.CurrentIndex);
        }

        public void SkipAll()
        {
            RequireBuilder();
            _builder.SetActiveOnly(_builder.Screens.Count - 1);
            PageChanged?.Invoke(_builder.CurrentIndex);
            TutorialCompleted?.Invoke(this);
        }

        public TutorialScreen CurrentScreen()
        {
            RequireBuilder();
            var i = _builder.CurrentIndex;
            if (i < 0 || i >= _builder.Screens.Count) return null;
            return _builder.Screens[i] as TutorialScreen;
        }

        public void ReportScreenCompleted(TutorialScreen screen)
        {
            RequireBuilder();
            if (screen == null) return;
            if (screen != CurrentScreen()) return;
            if (_builder.CanGoNext)
            {
                _builder.GoNext();
                PageChanged?.Invoke(_builder.CurrentIndex);
            }
            else
            {
                TutorialCompleted?.Invoke(this);
            }
        }

        private void RequireBuilder()
        {
            if (_builder == null) throw new InvalidOperationException("TutorialPlayer: TutorialBuilder is not assigned.");
        }
    }
}
