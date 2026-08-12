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

        float actualLifetime = lifetime * 1.5f;

        MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
        if (riseAnim == null)
        {
            riseAnim = gameObject.AddComponent<MoleRiseAnimation>();
        }
        riseAnim.Initialize(actualLifetime);

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
            currentClicks--;
            
            MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();

            if (currentClicks <= 0)
            {
                isHandled = true;
                if (lifetimeCoroutine != null)
                {
                    StopCoroutine(lifetimeCoroutine);
                }

                GameManager.Instance.KillEnemy(this);

                if (riseAnim != null)
                {
                    riseAnim.TriggerFastRetract(0.15f, () => Destroy(gameObject));
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // Intermediate hit: lower in stages
                float hiddenFraction = (float)(clicksRequired - currentClicks) / clicksRequired;
                if (riseAnim != null)
                {
                    riseAnim.LowerToStage(hiddenFraction);
                }
                GameManager.Instance.ShowToughHitMessage();
            }
        }
    }
}
