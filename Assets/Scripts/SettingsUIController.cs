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

    public void ResetDefaults()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetToDefaults();
            InitializeUI();
        }
    }
}
