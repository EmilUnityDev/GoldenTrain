using System;
using System.Collections.Generic;
using Game.Scripts.GameRoot.Services.DataStorage;
using UnityEngine;

public class CollectionUnlockService
{
    public event Action<string> OnItemUnlocked;

    private readonly List<CollectionItemData> _allItems = new();
    private readonly GameDataRepository _gameDataRepository;

    public CollectionUnlockService(GameDataRepository gameDataRepository, CollectionRegistryConfig registryConfig)
    {
        _gameDataRepository = gameDataRepository;

        foreach (var entry in registryConfig.Entries)
            _allItems.Add(new CollectionItemData(entry.tag, entry.itemId));

        Debug.Log($"[CollectionUnlock] Loaded {_allItems.Count} items from registry");
    }

    public void RegisterItem(CollectionTag tag, int itemId)
    {
        CollectionItemData item = new(tag, itemId);
        if (!_allItems.Contains(item))
            _allItems.Add(item);
    }

    public bool IsUnlocked(CollectionTag tag, int itemId)
    {
        return _gameDataRepository.Data.UnlockedCollections.Contains(GetKey(tag, itemId));
    }

    public void Unlock(CollectionTag tag, int itemId)
    {
        string key = GetKey(tag, itemId);
        if (_gameDataRepository.Data.UnlockedCollections.Contains(key))
            return;

        _gameDataRepository.Data.UnlockedCollections.Add(key);
        _gameDataRepository.Save();
        Debug.Log($"[CollectionUnlock] Unlocked: {key}, total={_gameDataRepository.Data.UnlockedCollections.Count}");

        OnItemUnlocked?.Invoke(key);
    }

    public void UnlockRandomCount(int count)
    {
        if (count <= 0 || _allItems.Count == 0)
        {
            Debug.LogWarning($"[CollectionUnlock] UnlockRandomCount skipped: count={count}, allItems={_allItems.Count}");
            return;
        }

        List<CollectionItemData> lockedItems = new();
        foreach (var item in _allItems)
        {
            if (!IsUnlocked(item.Tag, item.ItemId))
                lockedItems.Add(item);
        }

        Debug.Log($"[CollectionUnlock] lockedItems={lockedItems.Count}, unlocking={count}");

        for (int i = 0; i < count && lockedItems.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, lockedItems.Count);
            CollectionItemData item = lockedItems[randomIndex];
            Unlock(item.Tag, item.ItemId);
            lockedItems.RemoveAt(randomIndex);
        }
    }

    public void Lock(CollectionTag tag, int itemId)
    {
        string key = GetKey(tag, itemId);
        _gameDataRepository.Data.UnlockedCollections.Remove(key);
        _gameDataRepository.Save();
    }

    private string GetKey(CollectionTag tag, int itemId) => $"COLLECTION_{tag}_{itemId}";
}