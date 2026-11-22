using System;
using UnityEngine;

namespace Game.Scripts.Services.DailyQuests
{
    [Serializable]
    public class DailyQuestProgress : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public DailyQuestType Type;
        
        public string TypeName;

        public int CurrentProgress;
        public int RequiredProgress;

        [NonSerialized]
        public DailyQuestState State;
        
        public string StateName;
        public int RewardCoins;

        public float NormalizedProgress => RequiredProgress > 0
            ? Mathf.Clamp01((float)CurrentProgress / RequiredProgress)
            : 0f;

        public DailyQuestProgress() { }

        public DailyQuestProgress(DailyQuestType type, int required, int rewardCoins)
        {
            Type = type;
            RequiredProgress = required;
            CurrentProgress = 0;
            State = DailyQuestState.InProgress;
            RewardCoins = rewardCoins;
            SyncToStrings();
        }

        public void OnBeforeSerialize()
        {
            SyncToStrings();
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(TypeName) && Enum.TryParse(TypeName, out DailyQuestType parsedType))
                Type = parsedType;

            if (!string.IsNullOrEmpty(StateName) && Enum.TryParse(StateName, out DailyQuestState parsedState))
                State = parsedState;
        }

        private void SyncToStrings()
        {
            TypeName = Type.ToString();
            StateName = State.ToString();
        }
    }
}
