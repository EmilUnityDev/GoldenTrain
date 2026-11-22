using UnityEngine;

namespace SlotsLogic.Match3Slots.Scripts.Core
{
    [CreateAssetMenu(fileName = "SymbolData", menuName = "Match3Slot/SymbolData")]
    public class SymbolData : ScriptableObject
    {
        public string symbolID;
        public GameObject SymbolPrefab;
        public bool isWild;
        public bool isScatter;
    }
}