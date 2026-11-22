using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.GameRoot.Services.Analytics
{
    public class LocalAnalyticEventService : IAnalyticEventService
    {
        public void SendEvent(AnalyticEventType eventType)
        {
            SendEvent(eventType.ToString());
        }

        public void SendEvent(AnalyticEventType eventType, Dictionary<string, string> extraParams)
        {
            SendEvent(eventType.ToString(), extraParams);
        }

        public void SendEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return;
            Debug.Log($"[LocalAnalytics] {eventName}");
        }

        public void SendEvent(string eventName, Dictionary<string, string> extraParams)
        {
            SendEvent(eventName);
        }
    }
}
