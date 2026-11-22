using UnityEngine;

namespace Game.Scripts.Services.Analytics
{
    public static class LocalEventLogger
    {
        public static void SendEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return;
            Debug.Log($"[LocalEvent] {eventName}");
        }
    }
}
