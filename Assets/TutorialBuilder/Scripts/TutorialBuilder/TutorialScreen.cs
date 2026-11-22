using UnityEngine;

namespace Tutorials
{
    public sealed class TutorialScreen : MonoBehaviour
    {
        [SerializeField] private TutorialPlayer _player;
        private bool _completed;

        private void Awake()
        {
            if (_player == null) _player = GetComponentInParent<TutorialPlayer>(true);
        }

        public void MarkCompleted()
        {
            if (_completed) return;
            _completed = true;
            _player?.ReportScreenCompleted(this);
        }

        public void ResetCompletion() => _completed = false;
    }
}