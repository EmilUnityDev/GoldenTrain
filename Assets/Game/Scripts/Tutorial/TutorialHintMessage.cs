using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.Game.MiniGame;
using UnityEngine;

public class TutorialHintMessage : MonoBehaviour
{
    
    [SerializeField] private TutorialTreasuryofOlympusManager _tutorialManager;

    public void ClosePanel(CanvasGroup canvasGroup)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0, 0.25f).OnComplete((() =>
        {
            _tutorialManager.StartTutorGame();
        }));
    }
}
