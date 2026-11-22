using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CollectionRegistryConfig", menuName = "Configs/CollectionRegistryConfig")]
public class CollectionRegistryConfig : ScriptableObject
{
    [System.Serializable]
    public struct CollectionEntry
    {
        public CollectionTag tag;
        public int itemId;
    }

    [SerializeField] private List<CollectionEntry> _entries = new();
    public IReadOnlyList<CollectionEntry> Entries => _entries;
}

