using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EyeCalibrationController : MonoBehaviour
{
    public static EyeCalibrationController Instance { get; private set; }

    [Header("UI Elements (Optional Canvas)")]
    [SerializeField]
    private GameObject calibrationCanvasPanel;
    [SerializeField]
    private RectTransform targetDotTransform;
    [SerializeField]
    private Image progressFillImage;
    [SerializeField]
    private TextMeshProUGUI instructionText;
    [SerializeField]
    private TextMeshProUGUI stepCountText;
    [SerializeField]
    private Button cancelButton;

    [Header("Calibration Parameters")]
    [SerializeField]
    private float pointDuration = 1.5f;

    private readonly Vector2[] calibrationScreenPoints = new Vector2[]
    {
        new Vector2(0.15f, 0.85f), // 1. Top-Left
        new Vector2(0.85f, 0.85f), // 2. Top-Right
        new Vector2(0.50f, 0.50f), // 3. Center
        new Vector2(0.15f, 0.15f), // 4. Bottom-Left
        new Vector2(0.85f, 0.15f)  // 5. Bottom-Right
    };

    private readonly string[] pointNames = new string[]
    {
        "Top-Left",
        "Top-Right",
        "Center",
        "Bottom-Left",
        "Bottom-Right"
    };

    private bool isCalibrating = false;
    private bool isTestingMode = false;
    private int currentStep = 0;
    private float currentStepProgress = 0f;
    private string statusMessage = "";

    private List<Vector2>[] collectedRawGazeSamples;
    private Coroutine calibrationRoutine;

    public bool IsActive => isCalibrating || isTestingMode;
    public bool IsCalibrating => isCalibrating;
    public bool IsTestingMode => isTestingMode;
    public event Action OnCalibrationCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (calibrationCanvasPanel != null)
        {
            calibrationCanvasPanel.SetActive(false);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelCalibration);
        }
    }

    private void Update()
    {
        if (isTestingMode && Input.GetKeyDown(KeyCode.Escape))
        {
            StopFreeTestMode();
        }
    }

    public void StartCalibration()
    {
        isTestingMode = false;
        isCalibrating = true;
        currentStep = 0;
        currentStepProgress = 0f;

        HideBackgroundSettingsUI();

        SentisEyeTracker tracker = SentisEyeTracker.EnsureInstance();
        if (tracker != null)
        {
            tracker.ForceCameraStart();
        }

        if (calibrationCanvasPanel != null)
        {
            calibrationCanvasPanel.SetActive(true);
        }

        collectedRawGazeSamples = new List<Vector2>[calibrationScreenPoints.Length];
        for (int i = 0; i < collectedRawGazeSamples.Length; i++)
        {
            collectedRawGazeSamples[i] = new List<Vector2>();
        }

        if (calibrationRoutine != null)
        {
            StopCoroutine(calibrationRoutine);
        }
        calibrationRoutine = StartCoroutine(RunCalibrationSequence());
    }

    public void StartFreeTestMode()
    {
        isCalibrating = false;
        isTestingMode = true;

        HideBackgroundSettingsUI();

        SentisEyeTracker tracker = SentisEyeTracker.EnsureInstance();
        if (tracker != null)
        {
            tracker.ForceCameraStart();
        }

        if (calibrationCanvasPanel != null)
        {
            calibrationCanvasPanel.SetActive(false);
        }
    }

    public void StopFreeTestMode()
    {
        isTestingMode = false;
        RestoreBackgroundSettingsUI();

        if (SentisEyeTracker.Instance != null)
        {
            SentisEyeTracker.Instance.RequestCameraStopIfIdle();
        }
    }

    private CanvasGroup hiddenSettingsCanvasGroup = null;

    private void HideBackgroundSettingsUI()
    {
        SettingsUIController settingsUI = FindFirstObjectByType<SettingsUIController>();
        if (settingsUI != null)
        {
            CanvasGroup cg = settingsUI.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = settingsUI.gameObject.AddComponent<CanvasGroup>();
            }
            hiddenSettingsCanvasGroup = cg;
            hiddenSettingsCanvasGroup.alpha = 0f;
            hiddenSettingsCanvasGroup.blocksRaycasts = false;
            hiddenSettingsCanvasGroup.interactable = false;
        }
    }

    private void RestoreBackgroundSettingsUI()
    {
        if (hiddenSettingsCanvasGroup != null)
        {
            hiddenSettingsCanvasGroup.alpha = 1f;
            hiddenSettingsCanvasGroup.blocksRaycasts = true;
            hiddenSettingsCanvasGroup.interactable = true;
            hiddenSettingsCanvasGroup = null;
        }
    }

    private IEnumerator RunCalibrationSequence()
    {
        statusMessage = "Get ready... Look at each target dot steadily.";
        if (instructionText != null)
        {
            instructionText.text = statusMessage;
        }

        yield return new WaitForSeconds(1.0f);

        for (int step = 0; step < calibrationScreenPoints.Length; step++)
        {
            currentStep = step;
            Vector2 screenPoint = calibrationScreenPoints[step];
            string pointName = pointNames[step];

            statusMessage = $"Point {step + 1} of 5: Keep your gaze on the {pointName} dot";
            if (stepCountText != null)
            {
                stepCountText.text = $"Point {step + 1} of 5: {pointName}";
            }
            if (instructionText != null)
            {
                instructionText.text = statusMessage;
            }

            PositionTargetDot(screenPoint);

            float elapsed = 0f;
            while (elapsed < pointDuration)
            {
                elapsed += Time.deltaTime;
                currentStepProgress = Mathf.Clamp01(elapsed / pointDuration);

                if (progressFillImage != null)
                {
                    progressFillImage.fillAmount = currentStepProgress;
                }

                // Sample raw gaze
                if (SentisEyeTracker.Instance != null && SentisEyeTracker.Instance.IsWebCamRunning)
                {
                    collectedRawGazeSamples[step].Add(SentisEyeTracker.Instance.RawGazeNormalized);
                }

                yield return null;
            }

            currentStepProgress = 1.0f;
            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = 1.0f;
            }

            yield return new WaitForSeconds(0.25f);
        }

        // Process Collected Calibration Points
        ComputeAndSaveCalibration();

        statusMessage = "CALIBRATION SUCCESSFUL!";
        if (instructionText != null)
        {
            instructionText.text = "<color=#00FF88><b>CALIBRATION SUCCESSFUL!</b></color>";
        }
        if (stepCountText != null)
        {
            stepCountText.text = "Eye tracking is now calibrated to your screen.";
        }

        yield return new WaitForSeconds(1.2f);

        CloseCalibrationUI();
        OnCalibrationCompleted?.Invoke();
    }

    private void PositionTargetDot(Vector2 normalizedScreenPos)
    {
        if (targetDotTransform == null) return;

        RectTransform parentRect = targetDotTransform.parent as RectTransform;
        if (parentRect != null)
        {
            float width = parentRect.rect.width;
            float height = parentRect.rect.height;

            targetDotTransform.anchoredPosition = new Vector2(
                (normalizedScreenPos.x - 0.5f) * width,
                (normalizedScreenPos.y - 0.5f) * height
            );
        }
    }

    private void ComputeAndSaveCalibration()
    {
        Vector2[] avgPoints = new Vector2[calibrationScreenPoints.Length];
        for (int i = 0; i < calibrationScreenPoints.Length; i++)
        {
            var samples = collectedRawGazeSamples[i];
            if (samples.Count > 0)
            {
                Vector2 sum = Vector2.zero;
                foreach (var s in samples) sum += s;
                avgPoints[i] = sum / samples.Count;
            }
            else
            {
                avgPoints[i] = calibrationScreenPoints[i];
            }
        }

        // Top-Left (0), Top-Right (1), Center (2), Bottom-Left (3), Bottom-Right (4)
        float rawMinX = Mathf.Min(avgPoints[0].x, avgPoints[3].x);
        float rawMaxX = Mathf.Max(avgPoints[1].x, avgPoints[4].x);
        float rawMinY = Mathf.Min(avgPoints[0].y, avgPoints[1].y);
        float rawMaxY = Mathf.Max(avgPoints[3].y, avgPoints[4].y);

        float rangeX = Mathf.Max(0.015f, rawMaxX - rawMinX);
        float rangeY = Mathf.Max(0.015f, rawMaxY - rawMinY);

        float centerX = (rawMinX + rawMaxX) * 0.5f;
        float centerY = (rawMinY + rawMaxY) * 0.5f;

        float halfW = rangeX * 0.5f * 0.70f;
        float halfH = rangeY * 0.5f * 0.70f;

        float minX = centerX - halfW;
        float maxX = centerX + halfW;
        float minY = centerY - halfH;
        float maxY = centerY + halfH;

        EyeCalibrationData calib = new EyeCalibrationData
        {
            isCalibrated = true,
            minX = minX,
            maxX = maxX,
            minY = minY,
            maxY = maxY,
            calibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveEyeCalibration(calib);
            Debug.Log($"[EyeCalibrationController] Calibration Profile Saved: X=[{minX:F3}..{maxX:F3}], Y=[{minY:F3}..{maxY:F3}]");
        }
    }

    public void CancelCalibration()
    {
        if (calibrationRoutine != null)
        {
            StopCoroutine(calibrationRoutine);
            calibrationRoutine = null;
        }
        CloseCalibrationUI();
    }

    private void CloseCalibrationUI()
    {
        isCalibrating = false;
        if (calibrationCanvasPanel != null)
        {
            calibrationCanvasPanel.SetActive(false);
        }

        RestoreBackgroundSettingsUI();

        if (SentisEyeTracker.Instance != null)
        {
            SentisEyeTracker.Instance.RequestCameraStopIfIdle();
        }
    }

    private void OnGUI()
    {
        if (isCalibrating)
        {
            DrawCalibrationOverlay();
        }
        else if (isTestingMode)
        {
            DrawTestFreeMoveOverlay();
        }
    }

    private void DrawCalibrationOverlay()
    {
        // Dark focus backdrop
        GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Header Instructions
        GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.cyan;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        Rect bannerRect = new Rect(Screen.width * 0.10f, 20, Screen.width * 0.55f, 45);
        GUI.Box(bannerRect, $"🎯 5-POINT CALIBRATION — {statusMessage.ToUpper()}", titleStyle);

        // Progress Bar
        Rect progRect = new Rect(Screen.width * 0.10f, 75, Screen.width * 0.55f, 16);
        GUI.color = Color.gray;
        GUI.DrawTexture(progRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.1f, 0.9f, 0.4f, 1f);
        GUI.DrawTexture(new Rect(progRect.x, progRect.y, progRect.width * currentStepProgress, progRect.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Draw PiP Camera Window with Neural Tracking Face & Eye Rectangles
        DrawPiPCameraWindow(Screen.width - 245, 20, 220, 165);

        // Draw Active Target Dot
        if (currentStep >= 0 && currentStep < calibrationScreenPoints.Length)
        {
            Vector2 pt = calibrationScreenPoints[currentStep];
            float screenX = pt.x * Screen.width;
            float screenY = (1.0f - pt.y) * Screen.height;

            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 6f;
            float dotRadius = 24f + pulse;

            GUI.color = new Color(0.2f, 0.9f, 1.0f, 0.5f);
            GUI.DrawTexture(new Rect(screenX - dotRadius - 6, screenY - dotRadius - 6, (dotRadius + 6) * 2, (dotRadius + 6) * 2), Texture2D.whiteTexture);

            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(screenX - dotRadius, screenY - dotRadius, dotRadius * 2, dotRadius * 2), Texture2D.whiteTexture);

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(screenX - 8, screenY - 8, 16, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Cancel Button
        if (GUI.Button(new Rect(Screen.width * 0.42f, Screen.height - 70, Screen.width * 0.16f, 40), "Cancel Calibration"))
        {
            CancelCalibration();
        }
    }

    private void DrawTestFreeMoveOverlay()
    {
        GUI.color = new Color(0.05f, 0.07f, 0.12f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.cyan;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        Rect bannerRect = new Rect(Screen.width * 0.10f, 20, Screen.width * 0.55f, 45);
        GUI.Box(bannerRect, "🔍 FREE-MOVE CALIBRATION TEST (Move Head & Blink to Test)", titleStyle);

        // Read Live Normalized Gaze and Blink status
        Vector2 gaze = SentisEyeTracker.Instance != null 
            ? SentisEyeTracker.Instance.SmoothGazeNormalized 
            : new Vector2(0.5f, 0.5f);

        bool isBlinking = SentisEyeTracker.Instance != null && SentisEyeTracker.Instance.IsEyeClosed;

        // Draw PiP Camera Window with Neural Tracking Face & Eye Rectangles
        DrawPiPCameraWindow(Screen.width - 245, 20, 220, 165);

        int numCols = 5;
        int numRows = 4;
        int activeCol = Mathf.Clamp(Mathf.FloorToInt(gaze.x * numCols), 0, numCols - 1);
        int activeRow = Mathf.Clamp(Mathf.FloorToInt(gaze.y * numRows), 0, numRows - 1);

        float gridMarginX = Screen.width * 0.10f;
        float gridMarginY = Screen.height * 0.18f;
        float gridWidth = Screen.width * 0.60f;
        float gridHeight = Screen.height * 0.62f;

        float cellW = gridWidth / numCols;
        float cellH = gridHeight / numRows;

        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < numCols; c++)
            {
                Rect cellRect = new Rect(gridMarginX + c * cellW, gridMarginY + r * cellH, cellW - 8, cellH - 8);
                bool isHovered = (r == activeRow && c == activeCol);

                if (isHovered)
                {
                    GUI.color = isBlinking ? new Color(1f, 0.9f, 0.1f, 0.85f) : new Color(0.2f, 0.85f, 1f, 0.70f);
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);

                    GUI.color = isBlinking ? Color.yellow : Color.cyan;
                    DrawBorder(cellRect, 4);
                }
                else
                {
                    GUI.color = new Color(0.15f, 0.20f, 0.30f, 0.60f);
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);

                    GUI.color = new Color(0.3f, 0.4f, 0.5f, 0.5f);
                    DrawBorder(cellRect, 1);
                }

                GUI.color = isHovered ? Color.black : Color.white;
                GUIStyle cellTextStyle = new GUIStyle(GUI.skin.label);
                cellTextStyle.alignment = TextAnchor.MiddleCenter;
                cellTextStyle.fontStyle = isHovered ? FontStyle.Bold : FontStyle.Normal;
                cellTextStyle.fontSize = 14;

                string text = isHovered 
                    ? (isBlinking ? $"💥 HIT!\n[{r},{c}]" : $"🎯 TARGET\n[{r},{c}]")
                    : $"[{r},{c}]";

                GUI.Label(cellRect, text, cellTextStyle);
            }
        }
        GUI.color = Color.white;

        float reticleX = gridMarginX + gaze.x * gridWidth;
        float reticleY = gridMarginY + gaze.y * gridHeight;

        float reticleSize = isBlinking ? 36f : 24f;
        GUI.color = isBlinking ? Color.yellow : Color.cyan;
        GUI.DrawTexture(new Rect(reticleX - reticleSize / 2, reticleY - reticleSize / 2, reticleSize, reticleSize), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string blinkLabel = isBlinking ? "<color=#FFFF00><b>BLINKING (CLICK!)</b></color>" : "<color=#00FF88>Eyes Open</color>";
        GUIStyle bottomStyle = new GUIStyle(GUI.skin.label);
        bottomStyle.alignment = TextAnchor.MiddleCenter;
        bottomStyle.fontSize = 15;
        GUI.Label(new Rect(0, Screen.height - 110, Screen.width, 30), 
            $"Gaze: ({gaze.x:F2}, {gaze.y:F2}) | Target Tile: <b>Row {activeRow}, Col {activeCol}</b> | State: {blinkLabel}", bottomStyle);

        if (GUI.Button(new Rect(Screen.width * 0.40f, Screen.height - 65, Screen.width * 0.20f, 40), "Done (Back to Settings)"))
        {
            StopFreeTestMode();
        }
    }

    private void DrawPiPCameraWindow(float x, float y, float w, float h)
    {
        if (SentisEyeTracker.Instance == null || SentisEyeTracker.Instance.WebCamTexture == null || !SentisEyeTracker.Instance.WebCamTexture.isPlaying)
            return;

        WebCamTexture cam = SentisEyeTracker.Instance.WebCamTexture;
        Rect pipRect = new Rect(x, y, w, h);
        bool isBlinking = SentisEyeTracker.Instance.IsEyeClosed;

        GUI.color = Color.white;
        // Mirror the webcam texture horizontally (texCoords: x=1, y=0, width=-1, height=1)
        GUI.DrawTextureWithTexCoords(pipRect, cam, new Rect(1, 0, -1, 1));

        Rect face = SentisEyeTracker.Instance.DetectedFaceNormalizedRect;
        if (face.width > 0.05f)
        {
            float faceMirroredX = 1.0f - (face.x + face.width);
            Rect scaledFace = new Rect(
                pipRect.x + faceMirroredX * pipRect.width,
                pipRect.y + face.y * pipRect.height,
                face.width * pipRect.width,
                face.height * pipRect.height
            );
            GUI.color = Color.magenta;
            DrawBorder(scaledFace, 2);

            Rect leftEye = SentisEyeTracker.Instance.DetectedLeftEyeNormalizedRect;
            float leftEyeMirroredX = 1.0f - (leftEye.x + leftEye.width);
            Rect scaledLeftEye = new Rect(
                pipRect.x + leftEyeMirroredX * pipRect.width,
                pipRect.y + leftEye.y * pipRect.height,
                leftEye.width * pipRect.width,
                leftEye.height * pipRect.height
            );
            GUI.color = isBlinking ? Color.yellow : Color.cyan;
            DrawBorder(scaledLeftEye, 2);

            Rect rightEye = SentisEyeTracker.Instance.DetectedRightEyeNormalizedRect;
            float rightEyeMirroredX = 1.0f - (rightEye.x + rightEye.width);
            Rect scaledRightEye = new Rect(
                pipRect.x + rightEyeMirroredX * pipRect.width,
                pipRect.y + rightEye.y * pipRect.height,
                rightEye.width * pipRect.width,
                rightEye.height * pipRect.height
            );
            DrawBorder(scaledRightEye, 2);

            // Draw Yellow Nose Tip Dot
            Vector2 nose = SentisEyeTracker.Instance.DetectedNoseNormalizedPoint;
            float noseScreenX = pipRect.x + (1.0f - nose.x) * pipRect.width;
            float noseScreenY = pipRect.y + nose.y * pipRect.height;
            GUI.color = Color.yellow;
            GUI.DrawTexture(new Rect(noseScreenX - 5, noseScreenY - 5, 10, 10), Texture2D.whiteTexture);
        }

        Vector2 pupil = SentisEyeTracker.Instance.LastDetectedPupilPixel;
        if (pupil != Vector2.zero && cam.width > 0 && cam.height > 0)
        {
            float px = pipRect.x + (1.0f - (pupil.x / cam.width)) * pipRect.width;
            float py = pipRect.y + (pupil.y / cam.height) * pipRect.height;
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(px - 4, py - 4, 8, 8), Texture2D.whiteTexture);
        }

        GUI.color = Color.cyan;
        DrawBorder(pipRect, 2);
        GUI.color = Color.white;
        GUIStyle subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
        GUI.Label(new Rect(pipRect.x, pipRect.yMax + 2, pipRect.width, 20), "👁 Neural Face & Eyes Feed", subStyle);
    }

    private void DrawBorder(Rect rect, int thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}
