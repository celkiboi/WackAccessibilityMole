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

        if (noMouseGameplayToggle != null)
        {
            noMouseGameplayToggle.onValueChanged.RemoveAllListeners();
            noMouseGameplayToggle.isOn = SettingsManager.Instance.IsNoMouseGameplayEnabled;
            noMouseGameplayToggle.onValueChanged.AddListener(OnNoMouseGameplayToggleChanged);
        }

        if (showMoleKeyCombosToggle != null)
        {
            showMoleKeyCombosToggle.onValueChanged.RemoveAllListeners();
            bool canEnable = SettingsManager.Instance.IsNoMouseGameplayEnabled;
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

    private void OnNoMouseGameplayToggleChanged(bool enabled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetNoMouseGameplayEnabled(enabled);
        }

        if (showMoleKeyCombosToggle != null)
        {
            showMoleKeyCombosToggle.interactable = enabled;
            if (!enabled)
            {
                showMoleKeyCombosToggle.isOn = false;
            }
            UpdateMoleKeyComboToggleVisuals(enabled);
        }
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
