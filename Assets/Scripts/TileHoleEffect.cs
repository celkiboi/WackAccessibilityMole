using System.Collections;
using UnityEngine;

public class TileHoleEffect : MonoBehaviour
{
    [SerializeField]
    private Vector3 targetScale = new Vector3(0.85f, 0.55f, 1f);

    private Coroutine holeCoroutine;
    private SpriteRenderer holeRenderer;

    private void Awake()
    {
        holeRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    public void OpenHole(float duration = 0.20f)
    {
        if (holeCoroutine != null)
        {
            StopCoroutine(holeCoroutine);
        }
        holeCoroutine = StartCoroutine(AnimateHole(targetScale, 1.0f, duration, true));
    }

    public void CloseHole(float duration = 0.30f)
    {
        if (holeCoroutine != null)
        {
            StopCoroutine(holeCoroutine);
        }
        holeCoroutine = StartCoroutine(AnimateHole(Vector3.zero, 0.0f, duration, false));
    }

    private IEnumerator AnimateHole(Vector3 targetS, float targetAlpha, float duration, bool withPunch)
    {
        Vector3 startS = transform.localScale;
        Color startCol = holeRenderer != null ? holeRenderer.color : Color.white;
        Color targetCol = startCol;
        targetCol.a = targetAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scaleFactor = t;
            if (withPunch)
            {
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                scaleFactor = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            }
            else
            {
                scaleFactor = Mathf.SmoothStep(0f, 1f, t);
            }

            transform.localScale = Vector3.LerpUnclamped(startS, targetS, scaleFactor);

            if (holeRenderer != null)
            {
                holeRenderer.color = Color.Lerp(startCol, targetCol, t);
            }

            yield return null;
        }

        transform.localScale = targetS;
        if (holeRenderer != null)
        {
            holeRenderer.color = targetCol;
        }
    }
}
