using System.Collections;
using UnityEngine;

public class MoleRiseAnimation : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField]
    private float startYOffset = 1.0f;
    [SerializeField]
    private float targetY = 0f;

    [Header("Animation Ratio Settings")]
    [SerializeField]
    [Range(0.1f, 0.4f)]
    private float riseFraction = 0.25f;
    [SerializeField]
    [Range(0.1f, 0.4f)]
    private float retractFraction = 0.25f;

    private Coroutine animateCoroutine;

    private void Awake()
    {
        SetupMasking();
    }

    public void SetupMasking(int sortingOrder = -1)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (sortingOrder >= 0)
            {
                sr.sortingOrder = sortingOrder;
            }
        }
    }

    public void Initialize(float lifetime, float yOffset = 1.0f, int sortingOrder = -1)
    {
        startYOffset = yOffset;
        SetupMasking(sortingOrder);

        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }

        animateCoroutine = StartCoroutine(AnimateRoutine(lifetime));
    }

    private IEnumerator AnimateRoutine(float lifetime)
    {
        float riseDuration = Mathf.Min(0.35f, lifetime * riseFraction);
        float retractDuration = Mathf.Min(0.35f, lifetime * retractFraction);
        float stayDuration = Mathf.Max(0.1f, lifetime - riseDuration - retractDuration);

        Vector3 hiddenPos = new Vector3(0, -startYOffset, 0);
        Vector3 peekPos = new Vector3(0, targetY, 0);

        transform.localPosition = hiddenPos;

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(hiddenPos, peekPos, smoothT);
            yield return null;
        }

        transform.localPosition = peekPos;
        yield return new WaitForSeconds(stayDuration);

        elapsed = 0f;
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / retractDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(peekPos, hiddenPos, smoothT);
            yield return null;
        }

        transform.localPosition = hiddenPos;
    }

    private void OnDisable()
    {
        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }
    }

    public void TriggerFastRetract(float duration = 0.15f, System.Action onComplete = null)
    {
        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }

        animateCoroutine = StartCoroutine(FastRetractRoutine(duration, onComplete));
    }

    private IEnumerator FastRetractRoutine(float duration, System.Action onComplete)
    {
        Vector3 currentPos = transform.localPosition;
        Vector3 hiddenPos = new Vector3(0, -startYOffset, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(currentPos, hiddenPos, smoothT);
            yield return null;
        }

        transform.localPosition = hiddenPos;
        onComplete?.Invoke();
    }

    public void LowerToStage(float hiddenFraction, float duration = 0.12f)
    {
        float stageY = Mathf.Lerp(targetY, -startYOffset, Mathf.Clamp01(hiddenFraction));

        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }

        animateCoroutine = StartCoroutine(LowerToStageRoutine(stageY, duration));
    }

    private IEnumerator LowerToStageRoutine(float stageY, float duration)
    {
        Vector3 currentPos = transform.localPosition;
        Vector3 newTargetPos = new Vector3(0, stageY, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(currentPos, newTargetPos, smoothT);
            yield return null;
        }

        transform.localPosition = newTargetPos;
    }

    public void TriggerExplosion(Color flashColor, float burstScaleMultiplier = 1.8f, float duration = 0.25f, System.Action onComplete = null)
    {
        if (animateCoroutine != null)
        {
            StopCoroutine(animateCoroutine);
        }

        animateCoroutine = StartCoroutine(ExplosionRoutine(flashColor, burstScaleMultiplier, duration, onComplete));
    }

    private IEnumerator ExplosionRoutine(Color flashColor, float burstScaleMultiplier, float duration, System.Action onComplete)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].maskInteraction = SpriteMaskInteraction.None;
        }

        Vector3 initialScale = transform.localScale;
        Vector3 targetScale = initialScale * burstScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.SmoothStep(0f, 1f, t));

            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    Color baseColor = allowFlashes ? Color.Lerp(flashColor, originalColors[i], t * 0.5f) : originalColors[i];
                    baseColor.a = Mathf.Lerp(1f, 0f, t);
                    renderers[i].color = baseColor;
                }
            }

            yield return null;
        }

        onComplete?.Invoke();
    }
}
