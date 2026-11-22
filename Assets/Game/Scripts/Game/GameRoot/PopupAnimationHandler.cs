using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public static class PopupAnimationHandler {
        private const string AnimPrefix = "Anim_"; // все что начинается с этого - анимируем
        private const string NoAnimBGName = "NoAnimBG";

        public static void PlaySpawnAnimation(GameObject popup, Action onComplete = null) {
            var allChildren = GetAllChildren(popup.transform, skipNoAnimBGChildren: true)
                .Where(IsAnimatable) // фильтрация по префиксу
                .ToList();

            var sequence = DOTween.Sequence();

            // === Анимация NoAnimBG ===
            var noAnimBG = popup.transform
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == NoAnimBGName);

            if (noAnimBG != null) {
                noAnimBG.localScale = Vector3.one * 0.4f;

                sequence.Append(noAnimBG.DOScale(1.2f, 0.3f).SetEase(Ease.OutQuad));
                sequence.Append(noAnimBG.DOScale(0.9f, 0.2f).SetEase(Ease.InOutQuad));
                sequence.Append(noAnimBG.DOScale(1.0f, 0.1f).SetEase(Ease.OutQuad));
            }

            // === Анимации детей ===
            float duration = 0.18f; // Длительность анимации одного ребенка
            float overlap = 0.08f;  // Насколько раньше следующий начинает

            for (int i = 0; i < allChildren.Count; i++) {
                var child = allChildren[i];
                var originalScale = child.localScale;
                child.localScale = Vector3.zero;


                float startTime = 0.3f + i * (duration - overlap); // сдвиг на 0.5с после старта

                sequence.Insert(startTime,
                    child.DOScale(originalScale, duration)
                         .SetEase(Ease.OutQuad)
                );
            }

            sequence.OnComplete(() => onComplete?.Invoke());
            sequence.Play();
        }


        public static void PlayCloseAnimation(GameObject popup, Action onComplete = null) {
            var allChildren = GetAllChildren(popup.transform, skipNoAnimBGChildren: true)
                .Where(IsAnimatable) // фильтрация по префиксу
                .Reverse()           // закрываем в обратном порядке
                .ToList();

            var sequence = DOTween.Sequence();

            float duration = 0.16f; // длительность одной анимации
            float overlap = 0.1f;  // насколько раньше следующий стартует

            for (int i = 0; i < allChildren.Count; i++) {
                var child = allChildren[i];
                float startTime = i * (duration - overlap);

                sequence.Insert(startTime,
                    child.DOScale(Vector3.zero, duration)
                         .SetEase(Ease.InBack)
                );
            }

            sequence.OnComplete(() => onComplete?.Invoke());
            sequence.Play();
        }

        private static IEnumerable<Transform> GetAllChildren(Transform parent, bool skipNoAnimBGChildren = false) {
            var children = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++) {
                var child = parent.GetChild(i);

                children.Add(child);

                if (skipNoAnimBGChildren && child.name == NoAnimBGName) {

                    continue;
                }

                children.AddRange(GetAllChildren(child, skipNoAnimBGChildren));
            }
            return children;
        }

        private static bool IsAnimatable(Transform t) {
            return t.name.StartsWith(AnimPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
