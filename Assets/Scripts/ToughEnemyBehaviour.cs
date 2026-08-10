using System;
using System.Collections;
using UnityEngine;

public class ToughEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 300;
    [SerializeField]
    int clicksRequired = 3;

    public int ScoreValue => scoreValue;
    public bool PenalizeOnEscape => true;
    public GameObject GameObject => this.gameObject;

    public event Action<IEnemyBehaviour> OnEnemyEscaped;

    private Coroutine lifetimeCoroutine;
    private bool isHandled = false;
    private int currentClicks;

    private void Awake()
    {
        currentClicks = clicksRequired;
    }

    public void Initialize(float lifetime)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }

        // Tough enemies stay on screen 50% longer
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime * 1.5f));
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
            currentClicks--;
            
            // Visual feedback
            transform.localScale *= 0.9f;

            if (currentClicks <= 0)
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
}
