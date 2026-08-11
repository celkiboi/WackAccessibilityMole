using System;
using UnityEngine;

public enum ColorblindMode
{
    Off = 0,
    Protanopia = 1,    // Red-Blind
    Deuteranopia = 2,  // Green-Blind
    Tritanopia = 3     // Blue/Yellow-Blind
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string SCREEN_SHAKE_KEY = "Accessibility_ScreenShake";
    private const string SCREEN_FLASHES_KEY = "Accessibility_ScreenFlashes";
    private const string GAME_SPEED_KEY = "Accessibility_GameSpeed";
    private const string COLORBLIND_MODE_KEY = "Accessibility_ColorblindMode";
    private const string COLORBLIND_INTENSITY_KEY = "Accessibility_ColorblindIntensity";

    [Header("Default Settings")]
    [SerializeField]
    private bool defaultScreenShake = true;
    [SerializeField]
    private bool defaultScreenFlashes = true;
    [SerializeField]
    private float defaultGameSpeed = 1.0f;
    [SerializeField]
    private ColorblindMode defaultColorblindMode = ColorblindMode.Off;
    [SerializeField]
    private float defaultColorblindIntensity = 1.0f;

    public bool IsScreenShakeEnabled { get; private set; }
    public bool IsScreenFlashesEnabled { get; private set; }
    public float GameSpeedMultiplier { get; private set; }
    public ColorblindMode CurrentColorblindMode { get; private set; }
    public float ColorblindIntensity { get; private set; }

    public event Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    public void LoadSettings()
    {
        IsScreenShakeEnabled = PlayerPrefs.GetInt(SCREEN_SHAKE_KEY, defaultScreenShake ? 1 : 0) == 1;
        IsScreenFlashesEnabled = PlayerPrefs.GetInt(SCREEN_FLASHES_KEY, defaultScreenFlashes ? 1 : 0) == 1;
        GameSpeedMultiplier = PlayerPrefs.GetFloat(GAME_SPEED_KEY, defaultGameSpeed);
        
        int modeInt = PlayerPrefs.GetInt(COLORBLIND_MODE_KEY, (int)defaultColorblindMode);
        CurrentColorblindMode = Enum.IsDefined(typeof(ColorblindMode), modeInt) ? (ColorblindMode)modeInt : ColorblindMode.Off;
        ColorblindIntensity = PlayerPrefs.GetFloat(COLORBLIND_INTENSITY_KEY, defaultColorblindIntensity);
    }

    public void SetScreenShakeEnabled(bool enabled)
    {
        IsScreenShakeEnabled = enabled;
        PlayerPrefs.SetInt(SCREEN_SHAKE_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetScreenFlashesEnabled(bool enabled)
    {
        IsScreenFlashesEnabled = enabled;
        PlayerPrefs.SetInt(SCREEN_FLASHES_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetGameSpeedMultiplier(float speed)
    {
        GameSpeedMultiplier = Mathf.Clamp(speed, 0.5f, 1.5f);
        PlayerPrefs.SetFloat(GAME_SPEED_KEY, GameSpeedMultiplier);
        PlayerPrefs.Save();
        
        if (!GameManager.isGameFinished)
        {
            Time.timeScale = GameSpeedMultiplier;
        }

        OnSettingsChanged?.Invoke();
    }

    public void SetColorblindMode(ColorblindMode mode)
    {
        CurrentColorblindMode = mode;
        PlayerPrefs.SetInt(COLORBLIND_MODE_KEY, (int)mode);
        PlayerPrefs.Save();
        EnsureCameraEffect();
        OnSettingsChanged?.Invoke();
    }

    public void CycleColorblindMode()
    {
        int nextMode = ((int)CurrentColorblindMode + 1) % Enum.GetValues(typeof(ColorblindMode)).Length;
        SetColorblindMode((ColorblindMode)nextMode);
    }

    public void SetColorblindIntensity(float intensity)
    {
        ColorblindIntensity = Mathf.Clamp01(intensity);
        PlayerPrefs.SetFloat(COLORBLIND_INTENSITY_KEY, ColorblindIntensity);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void EnsureCameraEffect()
    {
        if (Camera.main != null)
        {
            ColorblindCameraEffect effect = Camera.main.GetComponent<ColorblindCameraEffect>();
            if (effect == null)
            {
                effect = Camera.main.gameObject.AddComponent<ColorblindCameraEffect>();
            }
            effect.UpdateShaderProperties();
        }
    }

    public void ResetToDefaults()
    {
        SetScreenShakeEnabled(defaultScreenShake);
        SetScreenFlashesEnabled(defaultScreenFlashes);
        SetGameSpeedMultiplier(defaultGameSpeed);
        SetColorblindMode(defaultColorblindMode);
        SetColorblindIntensity(defaultColorblindIntensity);
    }
}
