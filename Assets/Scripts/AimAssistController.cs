using System.Collections;
using UnityEngine;

public class AimAssistController : MonoBehaviour
{
    private Ground groundScript;
    private GameObject aimBoxObj;
    private LineRenderer lineRenderer;

    private int currentTargetRow = -1;
    private int currentTargetCol = -1;
    private bool isTargetingTile = false;

    private Vector3 targetPosition;
    private Vector3 baseScale = Vector3.one;
    private Coroutine punchCoroutine;

    private readonly Color activeColor = new Color(0.2f, 0.95f, 1.0f, 0.95f);
    private readonly Color punchColor = new Color(1.0f, 0.9f, 0.2f, 1.0f);

    private void Awake()
    {
        groundScript = GetComponent<Ground>();
        CreateAimBoxVisual();
    }

    private void CreateAimBoxVisual()
    {
        aimBoxObj = new GameObject("AimAssistHighlightBox");
        aimBoxObj.transform.SetParent(transform, false);

        lineRenderer = aimBoxObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 5;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }

        lineRenderer.startColor = activeColor;
        lineRenderer.endColor = activeColor;
        lineRenderer.sortingOrder = 95;

        lineRenderer.SetPosition(0, new Vector3(-0.5f, 0.5f, -0.05f));
        lineRenderer.SetPosition(1, new Vector3(0.5f, 0.5f, -0.05f));  
        lineRenderer.SetPosition(2, new Vector3(0.5f, -0.5f, -0.05f));  
        lineRenderer.SetPosition(3, new Vector3(-0.5f, -0.5f, -0.05f)); 
        lineRenderer.SetPosition(4, new Vector3(-0.5f, 0.5f, -0.05f));

        aimBoxObj.SetActive(false);
    }

    private Vector2 GetMouseWorldOnTilePlane()
    {
        if (Camera.main == null) return Vector2.zero;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane zPlane = new Plane(Vector3.forward, Vector3.zero);

        if (zPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            return new Vector2(hitPoint.x, hitPoint.y);
        }

        Vector3 mouseInput = Input.mousePosition;
        mouseInput.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 fallbackPoint = Camera.main.ScreenToWorldPoint(mouseInput);
        return new Vector2(fallbackPoint.x, fallbackPoint.y);
    }

    private void Update()
    {
        bool isAimAssist = SettingsManager.Instance != null &&
                           SettingsManager.Instance.IsAimAssistEnabled &&
                           !GameManager.isGameFinished;

        bool isKeyboardCursor = SettingsManager.Instance != null &&
                                SettingsManager.Instance.IsNoMouseGameplayEnabled &&
                                SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.GridCursor &&
                                !GameManager.isGameFinished;

        if (!isAimAssist && !isKeyboardCursor)
        {
            if (aimBoxObj != null && aimBoxObj.activeSelf)
            {
                aimBoxObj.SetActive(false);
            }
            isTargetingTile = false;
            return;
        }

        if (Camera.main == null || groundScript == null) return;

        int row = -1;
        int col = -1;
        bool hasTile = false;

        if (isKeyboardCursor)
        {
            if (GameManager.Instance != null)
            {
                row = GameManager.Instance.KeyboardCursorRow;
                col = GameManager.Instance.KeyboardCursorCol;
                hasTile = row >= 0 && col >= 0;
            }
        }
        else if (isAimAssist)
        {
            Vector2 mouseWorldPos = GetMouseWorldOnTilePlane();
            hasTile = groundScript.GetTileAtWorldPosition(mouseWorldPos, out row, out col);
        }

        if (hasTile)
        {
            currentTargetRow = row;
            currentTargetCol = col;
            isTargetingTile = true;

            int tileIndex = row * Ground.NumberOfCols + col;
            GameObject[] tiles = groundScript.GroundTiles;

            if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length && tiles[tileIndex] != null)
            {
                Vector3 tileCenter = tiles[tileIndex].transform.position;
                targetPosition = new Vector3(tileCenter.x, tileCenter.y, -0.05f);

                float width = Ground.TileWidth * 0.95f;
                float height = Ground.TileHeight * 0.95f;
                baseScale = new Vector3(width, height, 1f);

                if (!aimBoxObj.activeSelf)
                {
                    aimBoxObj.SetActive(true);
                    aimBoxObj.transform.position = targetPosition;
                    aimBoxObj.transform.localScale = baseScale;
                }
                else
                {
                    aimBoxObj.transform.position = Vector3.Lerp(aimBoxObj.transform.position, targetPosition, Time.deltaTime * 30f);
                    if (punchCoroutine == null)
                    {
                        aimBoxObj.transform.localScale = Vector3.Lerp(aimBoxObj.transform.localScale, baseScale, Time.deltaTime * 20f);
                    }
                }
            }

            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            if (lineRenderer != null && punchCoroutine == null)
            {
                float pulse = allowFlashes ? (Mathf.Sin(Time.time * 8f) + 1f) * 0.15f : 0f;
                Color c = allowFlashes ? Color.Lerp(activeColor, Color.white, pulse) : activeColor;
                lineRenderer.startColor = c;
                lineRenderer.endColor = c;
            }

            if (isAimAssist && Input.GetMouseButtonDown(0))
            {
                TriggerClickPunch();
                groundScript.SmashTileAt(currentTargetRow, currentTargetCol);
            }
        }
        else
        {
            isTargetingTile = false;
            if (aimBoxObj != null && aimBoxObj.activeSelf)
            {
                aimBoxObj.SetActive(false);
            }
        }
    }

    public void TriggerClickPunch()
    {
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
        }
        punchCoroutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        if (aimBoxObj == null) yield break;

        bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;

        Vector3 punchScale = baseScale * 1.25f;
        float duration = 0.15f;
        float elapsed = 0f;

        if (lineRenderer != null)
        {
            Color c = allowFlashes ? punchColor : activeColor;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scaleT = Mathf.Sin(t * Mathf.PI);

            aimBoxObj.transform.localScale = Vector3.Lerp(baseScale, punchScale, scaleT);
            yield return null;
        }

        aimBoxObj.transform.localScale = baseScale;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = activeColor;
        }
        punchCoroutine = null;
    }

    private void OnDestroy()
    {
        if (aimBoxObj != null)
        {
            Destroy(aimBoxObj);
        }
    }
}
