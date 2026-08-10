using System;
using System.Collections;
using UnityEngine;

public class GoldenEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 500;

    public int ScoreValue => scoreValue;
    public bool PenalizeOnEscape => true;
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

        // Golden enemies are very fast
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime * 0.6f));
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (!isHandled && !GameManager.isGameFinished)
        {
            isHandled = true;
            OnEnemyEscaped?.Invoke(this);
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

            GameManager.Instance.KillEnemy(this);
            Destroy(gameObject);
        }
    }
}
