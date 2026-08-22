using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI Toggles")]
    [SerializeField]
    private Toggle screenShakeToggle;
    [SerializeField]
    private Toggle screenFlashesToggle;
    [SerializeField]
    private Toggle noMouseGameplayToggle;
    [SerializeField]
    private Toggle showMoleKeyCombosToggle;
    [SerializeField]
    private Toggle spawnAudioCuesToggle;
    [SerializeField]
    private Toggle aimAssistToggle;
    [SerializeField]
    private Toggle eyeTrackingToggle;

    [Header("Game Speed Settings")]
    [SerializeField]
    private Slider gameSpeedSlider;

    [Header("Keyboard Mode Settings")]
    [SerializeField]
    private Button keyboardModeCycleButton;
    [SerializeField]
    private TextMeshProUGUI keyboardModeText;

    [Header("Colorblind Settings")]
    [SerializeField]
    private Button colorblindCycleButton;
    [SerializeField]
    private TextMeshProUGUI colorblindModeText;

    [Header("Score Repository Settings")]
    [SerializeField]
    private Button resetScoresButton;

    [Header("Eye Calibration Settings")]
    [SerializeField]
    private Button calibrateEyeTrackingButton;
    [SerializeField]
    private Button testEyeCalibrationButton;
    [SerializeField]
    private Button resetEyeCalibrationButton;
    [SerializeField]
    private TextMeshProUGUI eyeCalibrationStatusText;
    [SerializeField]
    private EyeCalibrationController calibrationController;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += SyncToggleStates;
        }
        InitializeUI();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= SyncToggleStates;
        }
    }

    private void Start()
    {
        InitializeUI();
    }

    public void SyncToggleStates()
    {
        if (SettingsManager.Instance == null) return;

        if (screenShakeToggle != null) screenShakeToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsScreenShakeEnabled);
        if (screenFlashesToggle != null) screenFlashesToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsScreenFlashesEnabled);
        if (noMouseGameplayToggle != null) noMouseGameplayToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsNoMouseGameplayEnabled);
        if (spawnAudioCuesToggle != null) spawnAudioCuesToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsSpawnAudioCuesEnabled);
        if (aimAssistToggle != null) aimAssistToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsAimAssistEnabled);
        if (eyeTrackingToggle != null) eyeTrackingToggle.SetIsOnWithoutNotify(SettingsManager.Instance.IsEyeTrackingEnabled);

        bool isMatrixMode = SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.MatrixCombo;
        bool canEnableKeyCombos = SettingsManager.Instance.IsNoMouseGameplayEnabled && isMatrixMode;

        if (showMoleKeyCombosToggle != null)
        {
            showMoleKeyCombosToggle.interactable = canEnableKeyCombos;
            showMoleKeyCombosToggle.SetIsOnWithoutNotify(canEnableKeyCombos && SettingsManager.Instance.IsShowMoleKeyCombosEnabled);
            UpdateMoleKeyComboToggleVisuals(canEnableKeyCombos);
        }

        if (keyboardModeCycleButton != null)
        {
            keyboardModeCycleButton.interactable = SettingsManager.Instance.IsNoMouseGameplayEnabled;
        }

        UpdateKeyboardModeUI();
        UpdateColorblindUI();
        UpdateEyeCalibrationUI();
    }

    public void InitializeUI()
    {
        if (SettingsManager.Instance == null) return;

        if (screenShakeToggle != null)
        {
            screenShakeToggle.onValueChanged.RemoveAllListeners();
            screenShakeToggle.isOn = SettingsManager.Instance.IsScreenShakeEnabled;
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeToggleChanged);
        }

        if (screenFlashesToggle != null)
        {
            screenFlashesToggle.onValueChanged.RemoveAllListeners();
            screenFlashesToggle.isOn = SettingsManager.Instance.IsScreenFlashesEnabled;
            screenFlashesToggle.onValueChanged.AddListener(OnScreenFlashesToggleChanged);
        }

        if (noMouseGameplayToggle != null)
        {
            noMouseGameplayToggle.onValueChanged.RemoveAllListeners();
            noMouseGameplayToggle.isOn = SettingsManager.Instance.IsNoMouseGameplayEnabled;
            noMouseGameplayToggle.onValueChanged.AddListener(OnNoMouseGameplayToggleChanged);
        }

        if (showMoleKeyCombosToggle != null)
        {
            showMoleKeyCombosToggle.onValueChanged.RemoveAllListeners();
            bool canEnable = SettingsManager.Instance.IsNoMouseGameplayEnabled && 
                             SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.MatrixCombo;
            showMoleKeyCombosToggle.interactable = canEnable;
            showMoleKeyCombosToggle.isOn = canEnable && SettingsManager.Instance.IsShowMoleKeyCombosEnabled;
            UpdateMoleKeyComboToggleVisuals(canEnable);
            showMoleKeyCombosToggle.onValueChanged.AddListener(OnShowMoleKeyCombosToggleChanged);
        }

        if (spawnAudioCuesToggle != null)
        {
            spawnAudioCuesToggle.onValueChanged.RemoveAllListeners();
            spawnAudioCuesToggle.isOn = SettingsManager.Instance.IsSpawnAudioCuesEnabled;
            spawnAudioCuesToggle.onValueChanged.AddListener(OnSpawnAudioCuesToggleChanged);
        }

        if (aimAssistToggle != null)
        {
            aimAssistToggle.onValueChanged.RemoveAllListeners();
            aimAssistToggle.isOn = SettingsManager.Instance.IsAimAssistEnabled;
            aimAssistToggle.onValueChanged.AddListener(OnAimAssistToggleChanged);
        }

        if (eyeTrackingToggle != null)
        {
            eyeTrackingToggle.onValueChanged.RemoveAllListeners();
            eyeTrackingToggle.isOn = SettingsManager.Instance.IsEyeTrackingEnabled;
            eyeTrackingToggle.onValueChanged.AddListener(OnEyeTrackingToggleChanged);
        }

        if (gameSpeedSlider != null)
        {
            gameSpeedSlider.onValueChanged.RemoveAllListeners();
            gameSpeedSlider.value = SettingsManager.Instance.GameSpeedMultiplier;
            gameSpeedSlider.onValueChanged.AddListener(OnGameSpeedSliderChanged);
        }

        if (colorblindCycleButton != null)
        {
            colorblindCycleButton.onClick.RemoveAllListeners();
            colorblindCycleButton.onClick.AddListener(OnColorblindCycleClicked);
        }

        if (keyboardModeCycleButton != null)
        {
            keyboardModeCycleButton.onClick.RemoveAllListeners();
            keyboardModeCycleButton.onClick.AddListener(OnKeyboardModeCycleClicked);
        }

        if (resetScoresButton != null)
        {
            resetScoresButton.onClick.RemoveAllListeners();
            resetScoresButton.onClick.AddListener(OnResetScoresClicked);
        }

        if (calibrateEyeTrackingButton != null)
        {
            calibrateEyeTrackingButton.onClick.RemoveAllListeners();
            calibrateEyeTrackingButton.onClick.AddListener(OnCalibrateEyeTrackingClicked);
        }

        if (testEyeCalibrationButton != null)
        {
            testEyeCalibrationButton.onClick.RemoveAllListeners();
            testEyeCalibrationButton.onClick.AddListener(OnTestEyeCalibrationClicked);
        }

        if (resetEyeCalibrationButton != null)
        {
            resetEyeCalibrationButton.onClick.RemoveAllListeners();
            resetEyeCalibrationButton.onClick.AddListener(OnResetEyeCalibrationClicked);
        }

        if (calibrationController != null)
        {
            calibrationController.OnCalibrationCompleted -= UpdateEyeCalibrationUI;
            calibrationController.OnCalibrationCompleted += UpdateEyeCalibrationUI;
        }

        UpdateColorblindUI();
        UpdateKeyboardModeUI();
        UpdateEyeCalibrationUI();
    }

    private void OnScreenShakeToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetScreenShakeEnabled(enabled);
        }
    }

    private void OnScreenFlashesToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetScreenFlashesEnabled(enabled);
        }
    }

    private void OnNoMouseGameplayToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetNoMouseGameplayEnabled(enabled);
        }

        bool isMatrixMode = SettingsManager.Instance != null &&
                            SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.MatrixCombo;
        bool canEnable = enabled && isMatrixMode;

        if (showMoleKeyCombosToggle != null)
        {
            showMoleKeyCombosToggle.interactable = canEnable;
            if (!canEnable)
            {
                showMoleKeyCombosToggle.isOn = false;
            }
            UpdateMoleKeyComboToggleVisuals(canEnable);
        }

        if (keyboardModeCycleButton != null)
        {
            keyboardModeCycleButton.interactable = enabled;
        }

        UpdateKeyboardModeUI();
    }

    private void OnShowMoleKeyCombosToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetShowMoleKeyCombosEnabled(enabled);
        }
    }

    private void OnSpawnAudioCuesToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSpawnAudioCuesEnabled(enabled);
        }
    }

    private void OnAimAssistToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetAimAssistEnabled(enabled);
        }
    }

    private void OnEyeTrackingToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetEyeTrackingEnabled(enabled);
        }
    }

    private void UpdateMoleKeyComboToggleVisuals(bool interactable)
    {
        if (showMoleKeyCombosToggle == null) return;
        CanvasGroup cg = showMoleKeyCombosToggle.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = showMoleKeyCombosToggle.gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = interactable ? 1.0f : 0.5f;
    }

    private void OnGameSpeedSliderChanged(float value)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetGameSpeedMultiplier(value);
        }
    }

    public void OnColorblindCycleClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.CycleColorblindMode();
            UpdateColorblindUI();
        }
    }

    public void OnKeyboardModeCycleClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.CycleKeyboardControlMode();
            UpdateKeyboardModeUI();

            bool canEnable = SettingsManager.Instance.IsNoMouseGameplayEnabled && 
                             SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.MatrixCombo;
            if (showMoleKeyCombosToggle != null)
            {
                showMoleKeyCombosToggle.interactable = canEnable;
                if (!canEnable)
                {
                    showMoleKeyCombosToggle.isOn = false;
                }
                UpdateMoleKeyComboToggleVisuals(canEnable);
            }
        }
    }

    private void UpdateKeyboardModeUI()
    {
        if (SettingsManager.Instance == null) return;

        KeyboardControlMode mode = SettingsManager.Instance.CurrentKeyboardControlMode;
        if (keyboardModeText != null)
        {
            switch (mode)
            {
                case KeyboardControlMode.MatrixCombo:
                    keyboardModeText.text = "Keyboard Mode: Matrix Combos (W/A/S/D + Arrows)";
                    break;
                case KeyboardControlMode.GridCursor:
                    keyboardModeText.text = "Keyboard Mode: Grid Cursor (Arrows/WASD + Space/Enter)";
                    break;
            }
        }
    }

    private void UpdateColorblindUI()
    {
        if (SettingsManager.Instance == null) return;

        ColorblindMode currentMode = SettingsManager.Instance.CurrentColorblindMode;

        if (colorblindModeText != null)
        {
            switch (currentMode)
            {
                case ColorblindMode.Off:
                    colorblindModeText.text = "Colorblind Mode: Off";
                    break;
                case ColorblindMode.Protanopia:
                    colorblindModeText.text = "Colorblind Mode: Protanopia (Red)";
                    break;
                case ColorblindMode.Deuteranopia:
                    colorblindModeText.text = "Colorblind Mode: Deuteranopia (Green)";
                    break;
                case ColorblindMode.Tritanopia:
                    colorblindModeText.text = "Colorblind Mode: Tritanopia (Blue)";
                    break;
            }
        }
    }

    public void OnResetScoresClicked()
    {
        ScoreRepository.ClearAllScores();
        ScoreUIController scoreUI = FindFirstObjectByType<ScoreUIController>();
        if (scoreUI != null)
        {
            scoreUI.RefreshUI();
        }
    }

    public void OnCalibrateEyeTrackingClicked()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsEyeTrackingEnabled)
        {
            SettingsManager.Instance.SetEyeTrackingEnabled(true);
        }

        if (calibrationController == null)
        {
            calibrationController = FindFirstObjectByType<EyeCalibrationController>();
            if (calibrationController == null)
            {
                calibrationController = gameObject.AddComponent<EyeCalibrationController>();
            }
        }

        if (calibrationController != null)
        {
            calibrationController.OnCalibrationCompleted -= UpdateEyeCalibrationUI;
            calibrationController.OnCalibrationCompleted += UpdateEyeCalibrationUI;
            calibrationController.StartCalibration();
        }
    }

    public void OnTestEyeCalibrationClicked()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsEyeTrackingEnabled)
        {
            SettingsManager.Instance.SetEyeTrackingEnabled(true);
        }

        if (calibrationController == null)
        {
            calibrationController = FindFirstObjectByType<EyeCalibrationController>();
            if (calibrationController == null)
            {
                calibrationController = gameObject.AddComponent<EyeCalibrationController>();
            }
        }

        if (calibrationController != null)
        {
            calibrationController.StartFreeTestMode();
        }
    }

    public void OnResetEyeCalibrationClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetEyeCalibration();
            UpdateEyeCalibrationUI();
        }
    }

    public void UpdateEyeCalibrationUI()
    {
        if (SettingsManager.Instance == null || eyeCalibrationStatusText == null) return;

        var calib = SettingsManager.Instance.CurrentEyeCalibration;
        if (calib.isCalibrated)
        {
            eyeCalibrationStatusText.text = $"Status: <color=#00FF88>Calibrated</color> ({calib.calibrationDate})";
        }
        else
        {
            eyeCalibrationStatusText.text = "Status: <color=#AAAAAA>Not Calibrated (Default Bounds)</color>";
        }
    }

    public void ResetDefaults()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetToDefaults();
        }
        InitializeUI();
    }
}
