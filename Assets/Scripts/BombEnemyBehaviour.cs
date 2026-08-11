using System;
using System.Collections;
using UnityEngine;

public class BombEnemyBehaviour : MonoBehaviour, IEnemyBehaviour
{
    [SerializeField]
    int scoreValue = 0;

    public int ScoreValue => scoreValue;
    public bool PenalizeOnEscape => false; 
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
        OnMouseDown();
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

            GameManager.Instance.HitBomb();
            GameManager.Instance.TriggerCameraShake(0.2f, 0.15f);

            MoleRiseAnimation riseAnim = GetComponent<MoleRiseAnimation>();
            if (riseAnim != null)
            {
                riseAnim.TriggerExplosion(Color.red, 1.8f, 0.2f, () => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
