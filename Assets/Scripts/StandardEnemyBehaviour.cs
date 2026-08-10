using System;
using System.Collections;
using UnityEngine;

public class StandardEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 100;

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

            MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
            if (riseAnim != null)
            {
                riseAnim.TriggerFastRetract(0.15f, () => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
