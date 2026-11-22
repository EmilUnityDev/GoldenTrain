using UnityEngine;
using System.Collections;

public class RandomParticleSpawner : MonoBehaviour {
    [SerializeField] private ParticleSystem particleSystem;

    [Header("Delay (seconds)")]
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;

    private Coroutine routine;

    private void OnEnable() {
        routine = StartCoroutine(PlayRoutine());
    }

    private void OnDisable() {
        if (routine != null)
            StopCoroutine(routine);
    }

    private IEnumerator PlayRoutine() {
        while (true) {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            RestartParticle();
        }
    }

    private void RestartParticle() {
        if (particleSystem == null)
            return;

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystem.Play();
    }
}
