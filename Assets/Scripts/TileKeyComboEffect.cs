using System.Collections;
using TMPro;
using UnityEngine;

public class TileKeyComboEffect : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Coroutine animCoroutine;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        TriggerPopIn();
    }

    public void TriggerPopIn()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
        }
        animCoroutine = StartCoroutine(PopInRoutine());
    }

    private IEnumerator PopInRoutine()
    {
        float duration = 0.22f;
        float elapsed = 0f;

        Vector3 punchScale = baseScale * 1.35f;

        Color neonGold = new Color(1.0f, 0.95f, 0.3f, 1.0f);
        Color flashWhite = Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float scaleT = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(baseScale, punchScale, scaleT);

            // Respect Screen Flashing Accessibility Setting
            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            if (textMesh != null)
            {
                textMesh.color = allowFlashes ? Color.Lerp(flashWhite, neonGold, t) : neonGold;
            }

            yield return null;
        }

        transform.localScale = baseScale;
        if (textMesh != null)
        {
            textMesh.color = neonGold;
        }

        // Subtle pulse loop while active (if flashes setting is enabled)
        while (gameObject.activeSelf)
        {
            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            if (allowFlashes && textMesh != null)
            {
                float pulseT = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                textMesh.color = Color.Lerp(neonGold, Color.white, pulseT * 0.3f);
            }
            else if (textMesh != null)
            {
                textMesh.color = neonGold;
            }
            yield return null;
        }
    }
}
