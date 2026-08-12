using System;
using System.Collections;
using UnityEngine;

public class NukeEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 200;

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

        MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
        if (riseAnim == null)
        {
            riseAnim = gameObject.AddComponent<MoleRiseAnimation>();
        }
        riseAnim.Initialize(lifetime);

        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime));
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
            GameManager.Instance.TriggerNuke();
            GameManager.Instance.TriggerCameraShake(0.35f, 0.25f);

            MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
            if (riseAnim != null)
            {
                riseAnim.TriggerExplosion(Color.yellow, 2.2f, 0.25f, () => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
