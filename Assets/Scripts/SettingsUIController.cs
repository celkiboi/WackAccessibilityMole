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

    [Header("Game Speed Settings")]
    [SerializeField]
    private Slider gameSpeedSlider;

    [Header("Colorblind Settings")]
    [SerializeField]
    private Button colorblindCycleButton;
    [SerializeField]
    private TextMeshProUGUI colorblindModeText;

    private void OnEnable()
    {
        InitializeUI();
    }

    private void Start()
    {
        InitializeUI();
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

        UpdateColorblindUI();
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

    public void ResetDefaults()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetToDefaults();
            InitializeUI();
        }
    }
}
