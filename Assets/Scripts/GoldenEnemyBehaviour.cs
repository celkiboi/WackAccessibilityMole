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

        float actualLifetime = lifetime * 0.6f;

        MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
        if (riseAnim == null)
        {
            riseAnim = gameObject.AddComponent<MoleRiseAnimation>();
        }
        riseAnim.Initialize(actualLifetime);

        // Golden enemies are very fast
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(actualLifetime));
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

    public void Hit()
    {
        ExecuteHit();
    }

    private void OnMouseDown()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.IsAimAssistEnabled)
        {
            return;
        }

        ExecuteHit();
    }

    private void ExecuteHit()
    {
        if (!isHandled && !GameManager.isGameFinished)
        {
            isHandled = true;
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
            }

            GameManager.Instance.KillEnemy(this);

            MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
            if (riseAnim != null)
            {
                riseAnim.TriggerFastRetract(0.12f, () => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
