using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    [CreateAssetMenu(fileName = "SlotGameConfig", menuName = "Match3Slot/SlotGameConfig")]
    public class SlotGameConfig : ScriptableObject
    {
        [Header("Board Dimensions")]
        [Tooltip("Количество строк на доске")]
        public int Rows = 6;

        [Tooltip("Количество колонок на доске")]
        public int Columns = 5;

        [Tooltip("Размер одной ячейки (используется для позиционирования объектов в мире)")]
        public float CellSizeX = 1.0f;
    
        [Tooltip("Размер одной ячейки (используется для позиционирования объектов в мире)")]
        public float CellSizeY = 1.0f;

        [Header("Drop (Падение)")]
        [Tooltip("Длительность падения символа на свою позицию")]
        public float DropDuration = 0.25f;

        [Tooltip("Дополнительная задержка после падения перед следующим действием")]
        public float DropExtraWait = 0.05f;

        [Header("Spawn (Появление новых символов)")]
        [Tooltip("Длительность появления новых символов (например, с верхней части доски)")]
        public float SpawnDuration = 0.3f;

        [Header("Destruction (Уничтожение символов)")]
        [Tooltip("Задержка перед началом уничтожения символов")]
        public float DestructionWaitTime = 0.5f;

        [Tooltip("Время затухания символов (анимация исчезновения)")]
        public float SymbolDestructionFadeDuration = 0.4f;

        [Header("Symbol Animations (Idle и прочее)")]
        [Tooltip("Минимальная скорость анимации для символа (например idle loop)")]
        public float SymbolAnimationMinSpeed = 0.7f;

        [Tooltip("Максимальная скорость анимации для символа")]
        public float SymbolAnimationMaxSpeed = 1.3f;

        [Header("Spin (Начальный сброс и респавн символов)")]
        [Tooltip("Длительность вылета символов с доски при начале спина")]
        public float SpinFlyOutDuration = 0.5f;

        [Tooltip("Задержка между стартом столбцов при вылете символов")]
        public float SpinColumnStaggerDelay = 0.1f;

        [Tooltip("Длительность анимации перемещения символа при респавне")]
        public float SpinSymbolMoveDuration = 0.3f;

        [Tooltip("Дополнительная задержка после полного появления доски")]
        public float SpinExtraWait = 0.1f;

        [Header("Matching Logic")]
        [Tooltip("Минимальная длина матча (например 3 для классического match-3)")]
        public int BasicMinMatchLength = 3;

        [Tooltip("Пороговое количество одинаковых символов на поле, чтобы вызвать матч (используется в SymbolCountMatchChecker)")]
        public int SymbolCountThreshold = 12;

        [Header("Clear Board")]
        [Tooltip("Задержка между очисткой каждого столбца при полной очистке поля")]
        public float ClearBoardColumnDelay = 0.3f;

        [Tooltip("Дополнительная задержка перед уничтожением символов")]
        public float DelayBeforeDestroy;
        
        [Header("Spin")]
        public float SpinDuration = 2.0f;
        public float StoppingDuration = 0.5f;
        public float ReelSpinSpeed = 10f;
        public float SpinStepDelay = 0f;
    }
}
