using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.InferenceEngine;
using TMPro;

public class SentisEyeTracker : MonoBehaviour
{
    public static SentisEyeTracker Instance { get; private set; }

    [Header("Sentis Neural Models")]
    [SerializeField]
    private ModelAsset faceDetectorAsset;
    [SerializeField]
    private ModelAsset faceMeshAsset;

    [Header("Camera & Tracking Settings")]
    [SerializeField]
    private int requestedWidth = 640;
    [SerializeField]
    private int requestedHeight = 480;
    [SerializeField]
    private int requestedFPS = 30;

    [Header("Camera Background Feed Settings")]
    [SerializeField]
    private bool showCameraFeedBackground = true;

    [Header("Gaze Smoothing")]
    [SerializeField]
    [Range(2f, 30f)]
    private float gazeSmoothingSpeed = 8f;

    [Header("Gaze Visual Box Settings")]
    [SerializeField]
    private Color gazeBoxColor = new Color(0.2f, 0.9f, 1.0f, 0.95f);
    [SerializeField]
    private Color blinkPunchColor = new Color(1.0f, 0.95f, 0.2f, 1.0f);
    [SerializeField]
    private float punchScaleMultiplier = 1.25f;

    [Header("Blink Detection Settings")]
    [SerializeField]
    [Range(0.08f, 0.40f)]
    private float earBlinkThreshold = 0.18f;
    [SerializeField]
    private float minBlinkDuration = 0.03f; // Reduced 4x for ultra-responsive quick blinks
    [SerializeField]
    private float maxBlinkDuration = 0.60f;

    [Header("Live Debug Overlay Settings")]
    [SerializeField]
    private bool showDebugHUD = true;
    [SerializeField]
    private KeyCode toggleDebugKey = KeyCode.F1;

    private WebCamTexture webCamTexture;
    private Color32[] pixelBuffer;

    // Model 1: Face Detector (UltraFace)
    private Model runtimeFaceDetector;
    private Worker faceDetectorWorker;

    // Model 2: 468-Point FaceMesh (MediaPipe)
    private Model runtimeFaceMesh;
    private Worker faceMeshWorker;

    private bool isSentisInitialized = false;

    private Rect detectedFaceNormalizedRect = Rect.zero;
    private Rect detectedLeftEyeNormalizedRect = Rect.zero;
    private Rect detectedRightEyeNormalizedRect = Rect.zero;
    private Vector2 detectedNoseNormalizedPoint = new Vector2(0.5f, 0.5f);
    private Vector2 lastDetectedPupilPixel = Vector2.zero;

    private string lastWhackTileInfo = "None";

    private GameObject cameraFeedCanvasObj;
    private RawImage cameraFeedRawImage;
    private RectTransform faceDebugBoxRect;
    private RectTransform leftEyeDebugDotRect;
    private RectTransform rightEyeDebugDotRect;
    private TextMeshProUGUI pipStatusText;
    private RenderTexture faceCropRT;
    private RenderTexture faceDetectorRT;

    private float adaptiveOpenEAR = 0.28f;
    private Vector2 detectedLeftEyePos = new Vector2(0.35f, 0.35f);
    private Vector2 detectedRightEyePos = new Vector2(0.65f, 0.35f);
    private bool hasMeshEyes = false;

    private GameObject gazeBoxObj;
    private LineRenderer gazeLineRenderer;
    private Vector3 baseGazeBoxScale = Vector3.one;
    private Vector3 targetGazeBoxPosition;
    private Coroutine gazePunchCoroutine;

    private Ground groundScript;

    private Vector2 rawGazeNormalized = new Vector2(0.5f, 0.5f);
    private Vector2 smoothGazeNormalized = new Vector2(0.5f, 0.5f);

    private bool isEyeClosed = false;
    private float currentBlinkDuration = 0f;
    private float currentEstimatedEAR = 0.30f;

    public int GazeTargetRow { get; private set; } = -1;
    public int GazeTargetCol { get; private set; } = -1;
    public bool HasGazeTarget => GazeTargetRow >= 0 && GazeTargetCol >= 0;
    public Vector2 RawGazeNormalized => rawGazeNormalized;
    public Vector2 SmoothGazeNormalized => smoothGazeNormalized;
    public bool IsEyeClosed => isEyeClosed;
    public float CurrentEAR => currentEstimatedEAR;
    public bool IsWebCamRunning => webCamTexture != null && webCamTexture.isPlaying;
    public bool IsSentisModelActive => isSentisInitialized && (faceDetectorWorker != null || faceMeshWorker != null);
    public Rect DetectedFaceNormalizedRect => detectedFaceNormalizedRect;
    public Rect DetectedLeftEyeNormalizedRect => detectedLeftEyeNormalizedRect;
    public Rect DetectedRightEyeNormalizedRect => detectedRightEyeNormalizedRect;
    public Vector2 DetectedNoseNormalizedPoint => detectedNoseNormalizedPoint;
    public Vector2 LastDetectedPupilPixel => lastDetectedPupilPixel;
    public WebCamTexture WebCamTexture => webCamTexture;

    public static SentisEyeTracker EnsureInstance()
    {
        if (Instance != null) return Instance;

        SentisEyeTracker existing = FindFirstObjectByType<SentisEyeTracker>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("SentisEyeTracker_Helper");
        SentisEyeTracker tracker = go.AddComponent<SentisEyeTracker>();
        Instance = tracker;
        return tracker;
    }

    public static void Shutdown()
    {
        if (Instance != null)
        {
            Instance.CleanupCamera();
            Instance.CleanupSentis();
            if (Instance.gazeBoxObj != null)
            {
                Destroy(Instance.gazeBoxObj);
            }
            GameObject go = Instance.gameObject;
            Instance = null;
            Destroy(go);
        }
    }

    public static void ShutdownCamera()
    {
        Shutdown();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (ShouldCameraBeRunning())
        {
            CheckAndToggleCameraState();
        }
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;

        CleanupCamera();
        CleanupSentis();
    }

    private void OnDestroy()
    {
        CleanupSentis();
        CleanupCamera();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || scene.name == "Scoreboard")
        {
            if (!forceCameraActive)
            {
                Shutdown();
                return;
            }
        }

        if (SettingsManager.Instance == null || !SettingsManager.Instance.IsEyeTrackingEnabled)
        {
            if (!forceCameraActive)
            {
                Shutdown();
                return;
            }
        }

        groundScript = FindFirstObjectByType<Ground>();
        CheckAndToggleCameraState();
    }

    private void HandleSettingsChanged()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsEyeTrackingEnabled && !forceCameraActive)
        {
            Shutdown();
            return;
        }
        CheckAndToggleCameraState();
    }

    private bool forceCameraActive = false;

    public void ForceCameraStart()
    {
        forceCameraActive = true;
        this.enabled = true;
        StartWebCam();
    }

    public void ReleaseCameraForce()
    {
        forceCameraActive = false;
        if (SettingsManager.Instance == null || !SettingsManager.Instance.IsEyeTrackingEnabled)
        {
            Shutdown();
        }
        else
        {
            CheckAndToggleCameraState();
        }
    }

    public void RequestCameraStopIfIdle()
    {
        ReleaseCameraForce();
    }

    private bool ShouldCameraBeRunning()
    {
        if (forceCameraActive) return true;

        if (SettingsManager.Instance != null && SettingsManager.Instance.IsEyeTrackingEnabled)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool isMenuScene = (sceneName == "MainMenu" || sceneName == "Scoreboard");
            return !isMenuScene;
        }

        return false;
    }

    private void CheckAndToggleCameraState()
    {
        bool shouldRun = ShouldCameraBeRunning();

        if (shouldRun)
        {
            if (webCamTexture == null || !webCamTexture.isPlaying)
            {
                StartWebCam();
            }
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "MainMenu" && sceneName != "Scoreboard")
            {
                if (gazeBoxObj == null)
                {
                    CreateGazeBoxVisual();
                }
            }
        }
        else if (!forceCameraActive)
        {
            Shutdown();
        }
    }

    private void StartWebCam()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("[SentisEyeTracker] No webcam devices found!");
            return;
        }

        try
        {
            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying) webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }

            string deviceName = WebCamTexture.devices[0].name;
            webCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
            webCamTexture.Play();

            SetupCameraFeedBackground();

            Debug.Log($"[SentisEyeTracker] Started WebCam: {deviceName} ({webCamTexture.width}x{webCamTexture.height}@{requestedFPS}fps)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SentisEyeTracker] Failed to start WebCam: {ex.Message}");
        }
    }

    [Header("Top-Right PIP Camera Feed Size")]
    [SerializeField]
    private Vector2 pipSize = new Vector2(260f, 180f);

    private void SetupCameraFeedBackground()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isGameplayScene = (sceneName != "MainMenu" && sceneName != "Scoreboard");
        if (!isGameplayScene || webCamTexture == null) return;

        if (cameraFeedCanvasObj == null)
        {
            cameraFeedCanvasObj = new GameObject("WebCamFeedOverlayCanvas");
            Canvas canvas = cameraFeedCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = cameraFeedCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Container frame with dark border
            GameObject frameObj = new GameObject("WebCamPIPFrame");
            frameObj.transform.SetParent(cameraFeedCanvasObj.transform, false);

            Image frameImg = frameObj.AddComponent<Image>();
            frameImg.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);

            RectTransform frameRt = frameObj.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(1f, 1f);
            frameRt.anchorMax = new Vector2(1f, 1f);
            frameRt.pivot = new Vector2(1f, 1f);
            frameRt.anchoredPosition = new Vector2(-20f, -20f);
            frameRt.sizeDelta = new Vector2(pipSize.x + 8f, pipSize.y + 8f);

            // Video Feed Image
            GameObject rawImageObj = new GameObject("WebCamRawImage");
            rawImageObj.transform.SetParent(frameObj.transform, false);

            cameraFeedRawImage = rawImageObj.AddComponent<RawImage>();
            cameraFeedRawImage.uvRect = new Rect(1, 0, -1, 1); // Mirrored horizontally
            cameraFeedRawImage.color = Color.white;

            RectTransform rt = rawImageObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(-6f, -6f);
            rt.anchoredPosition = Vector2.zero;

            // Debug Face Rectangle Box (Hollow Wireframe - 4 Edge Lines)
            GameObject faceBoxObj = new GameObject("FaceDebugBox");
            faceBoxObj.transform.SetParent(rawImageObj.transform, false);

            faceDebugBoxRect = faceBoxObj.AddComponent<RectTransform>();

            Color lineColor = new Color(0f, 1f, 0.3f, 0.95f);
            float thickness = 2f;

            // Top edge
            CreateBorderEdge(faceBoxObj, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -thickness * 0.5f), new Vector2(0, thickness), lineColor);
            // Bottom edge
            CreateBorderEdge(faceBoxObj, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, thickness * 0.5f), new Vector2(0, thickness), lineColor);
            // Left edge
            CreateBorderEdge(faceBoxObj, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(thickness * 0.5f, 0), new Vector2(thickness, 0), lineColor);
            // Right edge
            CreateBorderEdge(faceBoxObj, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(-thickness * 0.5f, 0), new Vector2(thickness, 0), lineColor);

            faceBoxObj.SetActive(false);

            // Debug Dots for Left and Right Eyes
            leftEyeDebugDotRect = CreateEyeDebugDot(rawImageObj, "LeftEyeDot", new Color(0f, 1f, 0.9f, 1f));
            rightEyeDebugDotRect = CreateEyeDebugDot(rawImageObj, "RightEyeDot", new Color(0f, 1f, 0.9f, 1f));

            // Live EAR & Blink Status Text on PIP
            GameObject statusTextObj = new GameObject("PIPStatusText");
            statusTextObj.transform.SetParent(frameObj.transform, false);

            pipStatusText = statusTextObj.AddComponent<TextMeshProUGUI>();
            pipStatusText.fontSize = 14f;
            pipStatusText.alignment = TextAlignmentOptions.BottomLeft;
            pipStatusText.color = new Color(0f, 1f, 0.3f, 0.95f);
            pipStatusText.text = "EYES: TRACKING";

            RectTransform textRt = statusTextObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0f, 0f);
            textRt.sizeDelta = new Vector2(-12f, 22f);
            textRt.anchoredPosition = new Vector2(6f, 4f);
        }

        if (cameraFeedRawImage != null)
        {
            cameraFeedRawImage.texture = webCamTexture;
        }
    }

    private RectTransform CreateEyeDebugDot(GameObject parent, string name, Color dotColor)
    {
        GameObject dotObj = new GameObject(name);
        dotObj.transform.SetParent(parent.transform, false);

        RectTransform rt = dotObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10f, 10f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = dotObj.AddComponent<Image>();
        img.color = dotColor;

        Outline outline = dotObj.AddComponent<Outline>();
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        dotObj.SetActive(false);
        return rt;
    }

    private void CreateBorderEdge(GameObject parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        GameObject edge = new GameObject("Edge");
        edge.transform.SetParent(parent.transform, false);

        Image img = edge.AddComponent<Image>();
        img.color = color;

        RectTransform rt = edge.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private void CreateGazeBoxVisual()
    {
        if (gazeBoxObj != null) return;

        gazeBoxObj = new GameObject("Gaze_Selection_Box");
        gazeLineRenderer = gazeBoxObj.AddComponent<LineRenderer>();

        Shader lineShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (lineShader != null)
        {
            gazeLineRenderer.material = new Material(lineShader);
        }

        gazeLineRenderer.positionCount = 5;
        gazeLineRenderer.startWidth = 0.08f;
        gazeLineRenderer.endWidth = 0.08f;
        gazeLineRenderer.startColor = gazeBoxColor;
        gazeLineRenderer.endColor = gazeBoxColor;
        gazeLineRenderer.useWorldSpace = false;
        gazeLineRenderer.loop = true;
        gazeLineRenderer.sortingOrder = 100;

        float hw = 0.5f;
        float hh = 0.5f;
        gazeLineRenderer.SetPosition(0, new Vector3(-hw, -hh, 0));
        gazeLineRenderer.SetPosition(1, new Vector3(hw, -hh, 0));
        gazeLineRenderer.SetPosition(2, new Vector3(hw, hh, 0));
        gazeLineRenderer.SetPosition(3, new Vector3(-hw, hh, 0));
        gazeLineRenderer.SetPosition(4, new Vector3(-hw, -hh, 0));

        gazeBoxObj.SetActive(false);
    }

    private void CleanupCamera()
    {
        if (cameraFeedCanvasObj != null)
        {
            Destroy(cameraFeedCanvasObj);
            cameraFeedCanvasObj = null;
            cameraFeedRawImage = null;
            faceDebugBoxRect = null;
            leftEyeDebugDotRect = null;
            rightEyeDebugDotRect = null;
            pipStatusText = null;
        }

        if (faceCropRT != null)
        {
            faceCropRT.Release();
            Destroy(faceCropRT);
            faceCropRT = null;
        }

        if (faceDetectorRT != null)
        {
            faceDetectorRT.Release();
            Destroy(faceDetectorRT);
            faceDetectorRT = null;
        }

        if (webCamTexture != null)
        {
            try
            {
                if (webCamTexture.isPlaying) webCamTexture.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SentisEyeTracker] Exception during WebCam Stop: {ex.Message}");
            }
            finally
            {
                Destroy(webCamTexture);
                webCamTexture = null;
            }
        }

        pixelBuffer = null;
    }

    private void CleanupSentis()
    {
        if (faceDetectorWorker != null)
        {
            faceDetectorWorker.Dispose();
            faceDetectorWorker = null;
        }
        if (faceMeshWorker != null)
        {
            faceMeshWorker.Dispose();
            faceMeshWorker = null;
        }
        isSentisInitialized = false;
    }

    private void Update()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu" || currentScene == "Scoreboard")
        {
            if (!forceCameraActive)
            {
                GazeTargetRow = -1;
                GazeTargetCol = -1;
                this.enabled = false;
                return;
            }
        }

        bool shouldRun = ShouldCameraBeRunning();

        if (!shouldRun || webCamTexture == null || !webCamTexture.isPlaying)
        {
            GazeTargetRow = -1;
            GazeTargetCol = -1;
            if (gazeBoxObj != null && gazeBoxObj.activeSelf)
            {
                gazeBoxObj.SetActive(false);
            }
            return;
        }

        if (Input.GetKeyDown(toggleDebugKey))
        {
            showDebugHUD = !showDebugHUD;
        }

        if (webCamTexture.isPlaying)
        {
            if (cameraFeedCanvasObj == null)
            {
                SetupCameraFeedBackground();
            }
            ProcessTrackingPipeline();
        }

        Vector2 calibratedGaze = SettingsManager.Instance != null 
            ? SettingsManager.Instance.ApplyEyeCalibration(rawGazeNormalized) 
            : rawGazeNormalized;

        // Smooth gaze trajectory
        smoothGazeNormalized = Vector2.Lerp(smoothGazeNormalized, calibratedGaze, Time.deltaTime * gazeSmoothingSpeed);

        UpdateTileTargeting();
        UpdateGazeBoxVisual();
        UpdateFaceDebugBoxVisual();
        ProcessBlinkTiming();

        // Manual Keyboard Whack for Gaze Target (Space & Enter)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TriggerBlinkWhack();
        }
    }

    private void UpdateFaceDebugBoxVisual()
    {
        if (faceDebugBoxRect != null)
        {
            if (detectedFaceNormalizedRect.width > 0.05f)
            {
                if (!faceDebugBoxRect.gameObject.activeSelf) faceDebugBoxRect.gameObject.SetActive(true);

                // In mirrored space: horizontal X flips
                float mx = 1f - (detectedFaceNormalizedRect.x + detectedFaceNormalizedRect.width);
                float my = 1f - (detectedFaceNormalizedRect.y + detectedFaceNormalizedRect.height);

                faceDebugBoxRect.anchorMin = new Vector2(mx, my);
                faceDebugBoxRect.anchorMax = new Vector2(mx + detectedFaceNormalizedRect.width, my + detectedFaceNormalizedRect.height);
                faceDebugBoxRect.offsetMin = Vector2.zero;
                faceDebugBoxRect.offsetMax = Vector2.zero;
            }
            else
            {
                if (faceDebugBoxRect.gameObject.activeSelf) faceDebugBoxRect.gameObject.SetActive(false);
            }
        }

        if (leftEyeDebugDotRect != null && rightEyeDebugDotRect != null)
        {
            if (hasMeshEyes)
            {
                if (!leftEyeDebugDotRect.gameObject.activeSelf) leftEyeDebugDotRect.gameObject.SetActive(true);
                if (!rightEyeDebugDotRect.gameObject.activeSelf) rightEyeDebugDotRect.gameObject.SetActive(true);

                float lmx = 1f - detectedLeftEyePos.x;
                float lmy = 1f - detectedLeftEyePos.y;
                leftEyeDebugDotRect.anchorMin = new Vector2(lmx, lmy);
                leftEyeDebugDotRect.anchorMax = new Vector2(lmx, lmy);
                leftEyeDebugDotRect.anchoredPosition = Vector2.zero;

                float rmx = 1f - detectedRightEyePos.x;
                float rmy = 1f - detectedRightEyePos.y;
                rightEyeDebugDotRect.anchorMin = new Vector2(rmx, rmy);
                rightEyeDebugDotRect.anchorMax = new Vector2(rmx, rmy);
                rightEyeDebugDotRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                if (leftEyeDebugDotRect.gameObject.activeSelf) leftEyeDebugDotRect.gameObject.SetActive(false);
                if (rightEyeDebugDotRect.gameObject.activeSelf) rightEyeDebugDotRect.gameObject.SetActive(false);
            }
        }
    }

    private void InitSentis()
    {
        if (isSentisInitialized && (faceDetectorWorker != null || faceMeshWorker != null)) return;

        // 1. Load Face Detector (UltraFace)
        try
        {
            if (faceDetectorAsset == null)
            {
                faceDetectorAsset = Resources.Load<ModelAsset>("ultraface");
            }

            if (faceDetectorAsset != null)
            {
                Model rawDetector = ModelLoader.Load(faceDetectorAsset);
                if (rawDetector != null)
                {
                    FunctionalGraph graph = new FunctionalGraph();
                    FunctionalTensor input = graph.AddInput(rawDetector, 0);
                    FunctionalTensor[] outputs = Functional.Forward(rawDetector, 2.0f * input - 1.0f);
                    runtimeFaceDetector = graph.Compile(outputs);

                    try
                    {
                        faceDetectorWorker = new Worker(runtimeFaceDetector, BackendType.GPUCompute);
                    }
                    catch
                    {
                        faceDetectorWorker = new Worker(runtimeFaceDetector, BackendType.CPU);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SentisEyeTracker] Face Detector loading warning: {ex.Message}");
        }

        // 2. Load 468-Point FaceMesh (MediaPipe)
        try
        {
            if (faceMeshAsset == null)
            {
                faceMeshAsset = Resources.Load<ModelAsset>("facemesh");
            }

            if (faceMeshAsset != null)
            {
                Model rawMesh = ModelLoader.Load(faceMeshAsset);
                if (rawMesh != null)
                {
                    runtimeFaceMesh = rawMesh;
                    try
                    {
                        faceMeshWorker = new Worker(runtimeFaceMesh, BackendType.GPUCompute);
                    }
                    catch
                    {
                        faceMeshWorker = new Worker(runtimeFaceMesh, BackendType.CPU);
                    }
                    Debug.Log($"[SentisEyeTracker] FaceMesh Worker created successfully (Inputs: {rawMesh.inputs.Count}, Outputs: {rawMesh.outputs.Count})");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SentisEyeTracker] FaceMesh loading error: {ex.Message}");
        }

        isSentisInitialized = (faceDetectorWorker != null || faceMeshWorker != null);
        if (isSentisInitialized)
        {
            Debug.Log("[SentisEyeTracker] 2-Stage Neural Pipeline Initialized (UltraFace + FaceMesh 468) on GPUCompute!");
        }
    }

    private struct UltraFacePrior { public float cx, cy, w, h; }
    private static UltraFacePrior[] ultraFacePriors = null;

    private static void GenerateUltraFacePriors()
    {
        if (ultraFacePriors != null && ultraFacePriors.Length == 4420) return;

        List<UltraFacePrior> priors = new List<UltraFacePrior>(4420);
        int[][] featureMaps = new int[][] { new int[] { 30, 40 }, new int[] { 15, 20 }, new int[] { 8, 10 }, new int[] { 4, 5 } };
        float[][] minSizes = new float[][] {
            new float[] { 10f, 16f, 24f },
            new float[] { 32f, 48f },
            new float[] { 64f, 96f },
            new float[] { 128f, 192f, 256f }
        };

        for (int k = 0; k < featureMaps.Length; k++)
        {
            int fH = featureMaps[k][0];
            int fW = featureMaps[k][1];
            float[] sizes = minSizes[k];

            for (int i = 0; i < fH; i++)
            {
                for (int j = 0; j < fW; j++)
                {
                    float cx = (j + 0.5f) / fW;
                    float cy = (i + 0.5f) / fH;

                    for (int s = 0; s < sizes.Length; s++)
                    {
                        float w = sizes[s] / 320f;
                        float h = sizes[s] / 240f;
                        priors.Add(new UltraFacePrior { cx = cx, cy = cy, w = w, h = h });
                    }
                }
            }
        }

        ultraFacePriors = priors.ToArray();
    }

    private bool RunFaceDetector(out Rect faceRect)
    {
        faceRect = Rect.zero;
        if (faceDetectorWorker == null || webCamTexture == null) return false;

        GenerateUltraFacePriors();

        try
        {
            if (faceDetectorRT == null || faceDetectorRT.width != 320 || faceDetectorRT.height != 240)
            {
                if (faceDetectorRT != null) { faceDetectorRT.Release(); Destroy(faceDetectorRT); }
                faceDetectorRT = new RenderTexture(320, 240, 0, RenderTextureFormat.ARGB32);
            }
            Graphics.Blit(webCamTexture, faceDetectorRT);

            TextureTransform transform = new TextureTransform().SetDimensions(width: 320, height: 240, channels: 3).SetTensorLayout(TensorLayout.NCHW);
            using Tensor<float> inputTensor = TextureConverter.ToTensor(faceDetectorRT, transform);
            faceDetectorWorker.Schedule(inputTensor);

            Tensor<float> scores = faceDetectorWorker.PeekOutput(0) as Tensor<float> ?? faceDetectorWorker.PeekOutput("scores") as Tensor<float>;
            Tensor<float> boxes = faceDetectorWorker.PeekOutput(1) as Tensor<float> ?? faceDetectorWorker.PeekOutput("boxes") as Tensor<float>;

            if (scores == null || boxes == null) return false;

            using var scoresRead = scores.ReadbackAndClone();
            using var boxesRead = boxes.ReadbackAndClone();

            float bestScore = 0.25f;
            int bestIdx = -1;

            int count = Mathf.Min(scores.shape[1], ultraFacePriors != null ? ultraFacePriors.Length : 4420);
            for (int i = 0; i < count; i++)
            {
                float score = scoresRead[0, i, 1];
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0 && ultraFacePriors != null && bestIdx < ultraFacePriors.Length)
            {
                UltraFacePrior prior = ultraFacePriors[bestIdx];

                float cx = prior.cx + boxesRead[0, bestIdx, 0] * 0.1f * prior.w;
                float cy = prior.cy + boxesRead[0, bestIdx, 1] * 0.1f * prior.h;
                float w = prior.w * Mathf.Exp(boxesRead[0, bestIdx, 2] * 0.2f);
                float h = prior.h * Mathf.Exp(boxesRead[0, bestIdx, 3] * 0.2f);

                float x1 = Mathf.Clamp01(cx - w * 0.5f);
                float y1 = Mathf.Clamp01(cy - h * 0.5f);
                float x2 = Mathf.Clamp01(cx + w * 0.5f);
                float y2 = Mathf.Clamp01(cy + h * 0.5f);

                faceRect = new Rect(x1, y1, Mathf.Max(0.08f, x2 - x1), Mathf.Max(0.08f, y2 - y1));
                return true;
            }
        }
        catch
        {
            // Silently skip
        }

        return false;
    }

    private void ProcessTrackingPipeline()
    {
        int width = webCamTexture.width;
        int height = webCamTexture.height;
        if (width <= 16 || height <= 16) return;

        if (pixelBuffer == null || pixelBuffer.Length != width * height)
        {
            pixelBuffer = new Color32[width * height];
        }
        webCamTexture.GetPixels32(pixelBuffer);

        InitSentis();

        bool hasFace = RunFaceDetector(out Rect face);
        if (hasFace && face.width > 0.05f)
        {
            detectedFaceNormalizedRect = face;

            float fX = face.x;
            float fY = face.y;
            float fW = face.width;
            float fH = face.height;

            // 1. MediaPipe FaceMesh with 20% Face Padding Crop
            float padX = fW * 0.20f;
            float padY = fH * 0.20f;
            float cropX = Mathf.Clamp01(fX - padX * 0.5f);
            float cropY = Mathf.Clamp01(fY - padY * 0.5f);
            float cropW = Mathf.Clamp01(fW + padX);
            float cropH = Mathf.Clamp01(fH + padY);

            bool meshSuccess = false;
            if (faceMeshWorker != null)
            {
                try
                {
                    if (faceCropRT == null || faceCropRT.width != 192 || faceCropRT.height != 192)
                    {
                        if (faceCropRT != null) { faceCropRT.Release(); Destroy(faceCropRT); }
                        faceCropRT = new RenderTexture(192, 192, 0, RenderTextureFormat.ARGB32);
                    }

                    Vector2 scale = new Vector2(cropW, cropH);
                    Vector2 offset = new Vector2(cropX, 1f - (cropY + cropH));
                    Graphics.Blit(webCamTexture, faceCropRT, scale, offset);

                    TextureTransform meshTransform = new TextureTransform()
                        .SetDimensions(width: 192, height: 192, channels: 3)
                        .SetTensorLayout(TensorLayout.NHWC);

                    using Tensor<float> meshInput = TextureConverter.ToTensor(faceCropRT, meshTransform);
                    faceMeshWorker.Schedule(meshInput);

                    Tensor<float> meshOutput = faceMeshWorker.PeekOutput() as Tensor<float> ?? faceMeshWorker.PeekOutput(0) as Tensor<float>;
                    if (meshOutput != null)
                    {
                        using var meshRead = meshOutput.ReadbackAndClone();

                        // Nose Tip Landmark #1
                        float noseX_norm = Mathf.Clamp01(meshRead[0, 1 * 3 + 0] / 192.0f);
                        float noseY_norm = Mathf.Clamp01(meshRead[0, 1 * 3 + 1] / 192.0f);
                        Vector2 noseInFull = new Vector2(cropX + noseX_norm * cropW, cropY + noseY_norm * cropH);
                        detectedNoseNormalizedPoint = noseInFull;
                        rawGazeNormalized = new Vector2(1.0f - noseInFull.x, noseInFull.y);

                        // Neural Eyelid Landmarks for Left Eye: #33, #133, #160, #144, #158, #153
                        Vector2 p33 = new Vector2(meshRead[0, 33 * 3], meshRead[0, 33 * 3 + 1]);
                        Vector2 p133 = new Vector2(meshRead[0, 133 * 3], meshRead[0, 133 * 3 + 1]);
                        Vector2 p160 = new Vector2(meshRead[0, 160 * 3], meshRead[0, 160 * 3 + 1]);
                        Vector2 p144 = new Vector2(meshRead[0, 144 * 3], meshRead[0, 144 * 3 + 1]);
                        Vector2 p158 = new Vector2(meshRead[0, 158 * 3], meshRead[0, 158 * 3 + 1]);
                        Vector2 p153 = new Vector2(meshRead[0, 153 * 3], meshRead[0, 153 * 3 + 1]);

                        float leftH = Vector2.Distance(p33, p133);
                        float leftV = Vector2.Distance(p160, p144) + Vector2.Distance(p158, p153);
                        float leftEAR = (leftH > 0.01f) ? (leftV / (2.0f * leftH)) : 0.28f;

                        // Neural Eyelid Landmarks for Right Eye: #362, #263, #385, #380, #387, #373
                        Vector2 p362 = new Vector2(meshRead[0, 362 * 3], meshRead[0, 362 * 3 + 1]);
                        Vector2 p263 = new Vector2(meshRead[0, 263 * 3], meshRead[0, 263 * 3 + 1]);
                        Vector2 p385 = new Vector2(meshRead[0, 385 * 3], meshRead[0, 385 * 3 + 1]);
                        Vector2 p380 = new Vector2(meshRead[0, 380 * 3], meshRead[0, 380 * 3 + 1]);
                        Vector2 p387 = new Vector2(meshRead[0, 387 * 3], meshRead[0, 387 * 3 + 1]);
                        Vector2 p373 = new Vector2(meshRead[0, 373 * 3], meshRead[0, 373 * 3 + 1]);

                        float rightH = Vector2.Distance(p362, p263);
                        float rightV = Vector2.Distance(p385, p380) + Vector2.Distance(p387, p373);
                        float rightEAR = (rightH > 0.01f) ? (rightV / (2.0f * rightH)) : 0.28f;

                        currentEstimatedEAR = Mathf.Clamp((leftEAR + rightEAR) * 0.5f, 0.05f, 0.50f);

                        // Eye center positions for live visual debug dots
                        Vector2 leftInCrop = (p33 + p133) * 0.5f / 192f;
                        detectedLeftEyePos = new Vector2(cropX + leftInCrop.x * cropW, cropY + leftInCrop.y * cropH);

                        Vector2 rightInCrop = (p362 + p263) * 0.5f / 192f;
                        detectedRightEyePos = new Vector2(cropX + rightInCrop.x * cropW, cropY + rightInCrop.y * cropH);

                        detectedLeftEyeNormalizedRect = new Rect(cropX + (p33.x / 192f) * cropW, cropY + (p160.y / 192f) * cropH, (Mathf.Abs(p133.x - p33.x) / 192f) * cropW, (Mathf.Abs(p144.y - p160.y) / 192f) * cropH * 2.0f);
                        detectedRightEyeNormalizedRect = new Rect(cropX + (p362.x / 192f) * cropW, cropY + (p385.y / 192f) * cropH, (Mathf.Abs(p263.x - p362.x) / 192f) * cropW, (Mathf.Abs(p380.y - p385.y) / 192f) * cropH * 2.0f);

                        hasMeshEyes = true;
                        meshSuccess = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SentisEyeTracker] FaceMesh PeekOutput returned null tensor!");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SentisEyeTracker] FaceMesh inference exception: {ex.Message}");
                    meshSuccess = false;
                }
            }

            if (!meshSuccess)
            {
                hasMeshEyes = false;
                Vector2 baseNose = new Vector2(fX + fW * 0.50f, fY + fH * 0.55f);
                detectedNoseNormalizedPoint = baseNose;
                rawGazeNormalized = new Vector2(1.0f - baseNose.x, baseNose.y);
            }
        }
        else
        {
            hasMeshEyes = false;
        }
    }

    private void UpdateTileTargeting()
    {
        if (groundScript == null) return;

        int numCols = Ground.NumberOfCols > 0 ? Ground.NumberOfCols : 5;
        int numRows = Ground.NumberOfRows > 0 ? Ground.NumberOfRows : 4;

        int col = Mathf.Clamp(Mathf.FloorToInt(smoothGazeNormalized.x * numCols), 0, numCols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt(smoothGazeNormalized.y * numRows), 0, numRows - 1);

        GazeTargetRow = row;
        GazeTargetCol = col;
    }

    private void UpdateGazeBoxVisual()
    {
        if (gazeBoxObj == null || groundScript == null) return;

        if (HasGazeTarget)
        {
            int tileIndex = GazeTargetRow * Ground.NumberOfCols + GazeTargetCol;
            GameObject[] tiles = groundScript.GroundTiles;

            if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length && tiles[tileIndex] != null)
            {
                Vector3 tileCenter = tiles[tileIndex].transform.position;
                targetGazeBoxPosition = new Vector3(tileCenter.x, tileCenter.y, -0.05f);

                float width = Ground.TileWidth;
                float height = Ground.TileHeight;
                baseGazeBoxScale = new Vector3(width * 1.05f, height * 1.05f, 1f);

                if (!gazeBoxObj.activeSelf)
                {
                    gazeBoxObj.transform.position = targetGazeBoxPosition;
                    gazeBoxObj.transform.localScale = baseGazeBoxScale;
                    gazeBoxObj.SetActive(true);
                }
                else
                {
                    gazeBoxObj.transform.position = Vector3.Lerp(gazeBoxObj.transform.position, targetGazeBoxPosition, Time.deltaTime * 30f);
                    if (gazePunchCoroutine == null)
                    {
                        gazeBoxObj.transform.localScale = Vector3.Lerp(gazeBoxObj.transform.localScale, baseGazeBoxScale, Time.deltaTime * 20f);
                    }
                }
            }

            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            if (gazeLineRenderer != null && gazePunchCoroutine == null)
            {
                float pulse = allowFlashes ? (Mathf.Sin(Time.time * 8f) + 1f) * 0.15f : 0f;
                Color c = allowFlashes ? Color.Lerp(gazeBoxColor, Color.white, pulse) : gazeBoxColor;
                gazeLineRenderer.startColor = c;
                gazeLineRenderer.endColor = c;
            }
        }
        else
        {
            if (gazeBoxObj.activeSelf)
            {
                gazeBoxObj.SetActive(false);
            }
        }
    }

    private void ProcessBlinkTiming()
    {
        // Continuously adapt open baseline EAR while eyes are open
        if (currentEstimatedEAR > 0.20f)
        {
            adaptiveOpenEAR = Mathf.Lerp(adaptiveOpenEAR, currentEstimatedEAR, Time.deltaTime * 2.5f);
        }

        float dynamicThreshold = Mathf.Clamp(adaptiveOpenEAR * 0.70f, 0.13f, 0.21f);
        bool isClosedNow = currentEstimatedEAR < dynamicThreshold;

        if (isClosedNow)
        {
            currentBlinkDuration += Time.deltaTime;
            isEyeClosed = true;
        }
        else
        {
            if (isEyeClosed)
            {
                if (currentBlinkDuration >= minBlinkDuration && currentBlinkDuration <= maxBlinkDuration)
                {
                    TriggerBlinkWhack();
                }
                currentBlinkDuration = 0f;
            }
            isEyeClosed = false;
        }

        if (pipStatusText != null)
        {
            string state = isEyeClosed ? "<color=#FF4444>[BLINK / CLOSED]</color>" : "<color=#00FF66>[EYES OPEN]</color>";
            pipStatusText.text = $"EAR: {currentEstimatedEAR:F2} (Open: {adaptiveOpenEAR:F2}) {state}";
        }
    }

    private void TriggerBlinkWhack()
    {
        if (!HasGazeTarget || groundScript == null) return;

        int row = GazeTargetRow;
        int col = GazeTargetCol;
        lastWhackTileInfo = $"Row {row}, Col {col} @ {Time.time:F2}s";

        if (gazePunchCoroutine != null)
        {
            StopCoroutine(gazePunchCoroutine);
        }
        gazePunchCoroutine = StartCoroutine(AnimateGazePunchVisual());

        int tileIndex = row * Ground.NumberOfCols + col;
        GameObject[] tiles = groundScript.GroundTiles;
        if (tiles != null && tileIndex >= 0 && tileIndex < tiles.Length && tiles[tileIndex] != null)
        {
            Vector2 tilePos = tiles[tileIndex].transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(tilePos, 1.2f);
            foreach (var hit in hits)
            {
                hit.SendMessage("ExecuteHit", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private IEnumerator AnimateGazePunchVisual()
    {
        if (gazeBoxObj == null || gazeLineRenderer == null) yield break;

        gazeLineRenderer.startColor = blinkPunchColor;
        gazeLineRenderer.endColor = blinkPunchColor;

        Vector3 startScale = baseGazeBoxScale;
        Vector3 peakScale = baseGazeBoxScale * punchScaleMultiplier;

        float elapsed = 0f;
        float duration = 0.12f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            gazeBoxObj.transform.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        gazeBoxObj.transform.localScale = baseGazeBoxScale;
        gazeLineRenderer.startColor = gazeBoxColor;
        gazeLineRenderer.endColor = gazeBoxColor;
        gazePunchCoroutine = null;
    }

}
