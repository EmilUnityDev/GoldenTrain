using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Scripts.GameRoot.Services.Popups
{
    public class PopupService : IPopupService
    {
        private readonly UIRootView _uiRoot;
        private readonly IObjectResolver _rootResolver;
        private IObjectResolver _activeSceneResolver;

        private class PopupEntry
        {
            public IPopup Popup;
            public Action OnClosedCallback;
            public Action OnCloseHandler;
        }

        private readonly List<PopupEntry> _popupStack = new();

        [Inject]
        public PopupService(UIRootView uiRoot, IObjectResolver resolver)
        {
            _uiRoot = uiRoot;
            _rootResolver = resolver;
            _activeSceneResolver = resolver;
        }

        public void RegisterSceneResolver(IObjectResolver sceneResolver)
        {
            _activeSceneResolver = sceneResolver ?? _rootResolver;
        }

        public void UnregisterSceneResolver()
        {
            _activeSceneResolver = _rootResolver;
        }

        public void ShowPopup<TPopup>() where TPopup : IPopup =>
            ShowPopupInternal<TPopup>(null, null);

        public void ShowPopup<TPopup>(Action onPopupClosed) where TPopup : IPopup =>
            ShowPopupInternal<TPopup>(null, onPopupClosed);

        public void ShowPopup<TPopup, TData>(TData popupData) where TPopup : IPopup where TData : IPopupData =>
            ShowPopupInternal<TPopup>(popupData, null);

        public void ShowPopup<TPopup, TData>(TData popupData, Action onPopupClosed) where TPopup : IPopup where TData : IPopupData =>
            ShowPopupInternal<TPopup>(popupData, onPopupClosed);

        private void ShowPopupInternal<TPopup>(IPopupData popupData, Action onPopupClosed) where TPopup : IPopup
        {
            CreatePopup(typeof(TPopup).Name, popupData, onPopupClosed);
        }

        private void CreatePopup(string prefabName, IPopupData popupData = null, Action onPopupClosed = null)
        {
            if (_popupStack.Count > 0)
            {
                var current = _popupStack[^1];
                _popupStack.RemoveAt(_popupStack.Count - 1);
                current.Popup.OnClose -= current.OnCloseHandler;
                current.OnClosedCallback?.Invoke();
                current.Popup.ClosePopup();
            }

            var prefab = FindPrefabInResources("Popups", prefabName);

            if (prefab == null)
            {
                Debug.LogError($"[PopupService] Prefab '{prefabName}' not found.");
                return;
            }

            var popupObject = _activeSceneResolver.Instantiate(prefab);
            var popup = popupObject.GetComponent<IPopup>();

            if (popup == null)
            {
                Object.Destroy(popupObject);
                return;
            }

            if (popupObject.TryGetComponent<MonoBehaviour>(out var viewMono))
                _uiRoot.AttachPopupUI(viewMono.gameObject);

            var entry = new PopupEntry
            {
                Popup = popup,
                OnClosedCallback = onPopupClosed
            };
            entry.OnCloseHandler = () => OnPopupClosed(popup);
            popup.OnClose += entry.OnCloseHandler;

            _popupStack.Add(entry);
            popup.Bind(popupData);
        }

        private void OnPopupClosed(IPopup popup)
        {
            var entry = _popupStack.FirstOrDefault(e => e.Popup == popup);
            if (entry == null) return;

            _popupStack.Remove(entry);
            entry.OnClosedCallback?.Invoke();
        }

        public void CloseCurrentPopup()
        {
            if (_popupStack.Count == 0) return;
            _popupStack[^1].Popup.ClosePopup();
        }

        private GameObject FindPrefabInResources(string basePath, string prefabName)
        {
            var directLoad = Resources.Load<GameObject>($"{basePath}/{prefabName}");
            if (directLoad != null) return directLoad;

            var allPrefabs = Resources.LoadAll<GameObject>(basePath);
            return allPrefabs.FirstOrDefault(p => p.name.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
        }
    }
}