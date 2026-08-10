using System;
using System.Collections;
using UnityEngine;

public class BombEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 0; // Bombs shouldn't give score if clicked!

    public int ScoreValue => scoreValue;
    public bool PenalizeOnEscape => false; // Safe to let it despawn!
    public GameObject GameObject => this.gameObject;

    public event Action<IEnemyBehaviour> OnEnemyEscaped;

    private Coroutine lifetimeCoroutine;
    private bool isHandled = false;

    public void Initialize(float lifetime)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }

        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime));
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (!isHandled && !GameManager.isGameFinished)
        {
            isHandled = true;
            OnEnemyEscaped?.Invoke(this); // Wont penalize because PenalizeOnEscape is false
            Destroy(gameObject);
        }
    }

    private void OnMouseDown()
    {
        if (!isHandled && !GameManager.isGameFinished)
        {
            isHandled = true;
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
            }

            // Clicked a bomb! Penalize!
            GameManager.Instance.HitBomb();
            Destroy(gameObject);
        }
    }
}
