using System.Collections;
using System.Collections.Generic;
using Tutorials;
using UnityEngine;

public abstract class TutorialNextAfter : MonoBehaviour
{
    public TutorialScreen TutorialScreen;
    public virtual void Next()
    {
        TutorialScreen.MarkCompleted();
    }
}
