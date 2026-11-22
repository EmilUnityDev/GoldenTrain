using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public enum CollectionTag { Postcards, Luggage, Tickets, Watches, Badges }

public class CollectionItemView : MonoBehaviour {
    [SerializeField] private CollectionTag collectionTag;
    [SerializeField] private int itemId;
    [SerializeField] private Image image;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    [Header("Shader Animation")]
    [SerializeField] private Material fillMaterialPrefab;
    [SerializeField] private float appearanceDuration = 0.5f;

    private CollectionUnlockService _collectionUnlockService;
    private Material _localMaterial;
    private Coroutine _fillCoroutine;

    private static readonly int FillAmountProperty = Shader.PropertyToID("_FillAmount");

    public CollectionTag CollectionTag => collectionTag;
    public int ItemId => itemId;
    public bool IsUnlocked => _collectionUnlockService.IsUnlocked(collectionTag, itemId);
    private string SaveKey => $"COLLECTION_{collectionTag}_{itemId}";

    [Inject]
    private void Construct(CollectionUnlockService collectionUnlockService) {
        _collectionUnlockService = collectionUnlockService;
        _collectionUnlockService.RegisterItem(collectionTag, itemId);
    }

    // Awake убран намеренно — материал создаётся лениво в EnsureMaterial(),
    // потому что порядок Awake между PageSwitcher и ItemView не гарантирован.
    // PageSwitcher.Awake вызывает PrepareForAnimation до того как ItemView.Awake
    // успел создать _localMaterial — и Prepare падал в null.

    private void OnEnable() {
        if (_collectionUnlockService != null)
            _collectionUnlockService.OnItemUnlocked += HandleUnlocked;
    }

    private void OnDisable() {
        if (_collectionUnlockService != null)
            _collectionUnlockService.OnItemUnlocked -= HandleUnlocked;

        StopFillAnimation();
    }

    private void OnDestroy() {
        if (_localMaterial != null)
            Destroy(_localMaterial);
    }

    // Ленивое создание материала — вызывается перед любым использованием
    private void EnsureMaterial() {
        if (_localMaterial == null && image != null && fillMaterialPrefab != null)
            _localMaterial = new Material(fillMaterialPrefab);
    }

    public void PrepareForAnimation() {
        if (_collectionUnlockService == null || image == null) return;

        EnsureMaterial();

        if (IsUnlocked) {
            image.sprite = unlockedSprite;
            image.SetNativeSize();
            if (_localMaterial != null) {
                image.material = _localMaterial;
                _localMaterial.SetFloat(FillAmountProperty, 0f);
            }
        } else {
            image.sprite = lockedSprite;
            image.SetNativeSize();
            image.material = null;
        }
    }

    public void Refresh(bool playAnimation = false) {
        if (image == null || _collectionUnlockService == null) return;

        EnsureMaterial();

        bool unlocked = IsUnlocked;
        image.sprite = unlocked ? unlockedSprite : lockedSprite;
        image.SetNativeSize();

        if (unlocked && _localMaterial != null) {
            image.material = _localMaterial;

            if (playAnimation && gameObject.activeInHierarchy) {
                StopFillAnimation();
                _fillCoroutine = StartCoroutine(PlayFillAnimation());
            } else {
                _localMaterial.SetFloat(FillAmountProperty, 1f);
            }
        } else {
            image.material = null;
        }
    }

    private IEnumerator PlayFillAnimation() {
        _localMaterial.SetFloat(FillAmountProperty, 0f);

        float timer = 0f;
        while (timer < appearanceDuration) {
            timer += Time.deltaTime;
            _localMaterial.SetFloat(FillAmountProperty, timer / appearanceDuration);
            yield return null;
        }

        _localMaterial.SetFloat(FillAmountProperty, 1f);
    }

    private void StopFillAnimation() {
        if (_fillCoroutine != null) {
            StopCoroutine(_fillCoroutine);
            _fillCoroutine = null;
        }
    }

    private void HandleUnlocked(string key) {
        if (key == SaveKey)
            Refresh(playAnimation: true);
    }
}