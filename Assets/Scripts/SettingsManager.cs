using System;
using UnityEngine;

public enum ColorblindMode
{
    Off = 0,
    Protanopia = 1,    // Red-Blind
    Deuteranopia = 2,  // Green-Blind
    Tritanopia = 3     // Blue/Yellow-Blind
}

public enum KeyboardControlMode
{
    MatrixCombo = 0,   // Key Combos (Column Key + Row Key)
    GridCursor = 1     // Grid Cursor Navigation (Arrow Keys / WASD move highlight, Space/Enter smashes)
}

[System.Serializable]
public struct EyeCalibrationData
{
    public bool isCalibrated;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public string calibrationDate;

    public static EyeCalibrationData Default => new EyeCalibrationData
    {
        isCalibrated = false,
        minX = 0.25f,
        maxX = 0.75f,
        minY = 0.25f,
        maxY = 0.75f,
        calibrationDate = "None"
    };
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string SCREEN_SHAKE_KEY = "Accessibility_ScreenShake";
    private const string SCREEN_FLASHES_KEY = "Accessibility_ScreenFlashes";
    private const string GAME_SPEED_KEY = "Accessibility_GameSpeed";
    private const string COLORBLIND_MODE_KEY = "Accessibility_ColorblindMode";
    private const string COLORBLIND_INTENSITY_KEY = "Accessibility_ColorblindIntensity";
    private const string NO_MOUSE_GAMEPLAY_KEY = "Accessibility_NoMouseGameplay";
    private const string KEYBOARD_CONTROL_MODE_KEY = "Accessibility_KeyboardControlMode";
    private const string SHOW_MOLE_KEY_COMBOS_KEY = "Accessibility_ShowMoleKeyCombos";
    private const string SPAWN_AUDIO_CUES_KEY = "Accessibility_SpawnAudioCues";
    private const string AIM_ASSIST_KEY = "Accessibility_AimAssist";
    private const string EYE_TRACKING_KEY = "Accessibility_EyeTracking";
    private const string EYE_CALIBRATION_KEY = "Accessibility_EyeCalibration";

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
    [SerializeField]
    private bool defaultNoMouseGameplay = false;
    [SerializeField]
    private KeyboardControlMode defaultKeyboardControlMode = KeyboardControlMode.MatrixCombo;
    [SerializeField]
    private bool defaultShowMoleKeyCombos = false;
    [SerializeField]
    private bool defaultSpawnAudioCues = false;
    [SerializeField]
    private bool defaultAimAssist = false;
    [SerializeField]
    private bool defaultEyeTracking = false;

    public bool IsScreenShakeEnabled { get; private set; }
    public bool IsScreenFlashesEnabled { get; private set; }
    public float GameSpeedMultiplier { get; private set; }
    public ColorblindMode CurrentColorblindMode { get; private set; }
    public float ColorblindIntensity { get; private set; }
    public bool IsNoMouseGameplayEnabled { get; private set; }
    public KeyboardControlMode CurrentKeyboardControlMode { get; private set; }
    public bool IsShowMoleKeyCombosEnabled { get; private set; }
    public bool IsSpawnAudioCuesEnabled { get; private set; }
    public bool IsAimAssistEnabled { get; private set; }
    public bool IsEyeTrackingEnabled { get; private set; }
    public EyeCalibrationData CurrentEyeCalibration { get; private set; }

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
        IsNoMouseGameplayEnabled = PlayerPrefs.GetInt(NO_MOUSE_GAMEPLAY_KEY, defaultNoMouseGameplay ? 1 : 0) == 1;
        
        int kbModeInt = PlayerPrefs.GetInt(KEYBOARD_CONTROL_MODE_KEY, (int)defaultKeyboardControlMode);
        CurrentKeyboardControlMode = Enum.IsDefined(typeof(KeyboardControlMode), kbModeInt) ? (KeyboardControlMode)kbModeInt : KeyboardControlMode.MatrixCombo;

        IsShowMoleKeyCombosEnabled = PlayerPrefs.GetInt(SHOW_MOLE_KEY_COMBOS_KEY, defaultShowMoleKeyCombos ? 1 : 0) == 1;
        if (!IsNoMouseGameplayEnabled)
        {
            IsShowMoleKeyCombosEnabled = false;
        }

        IsSpawnAudioCuesEnabled = PlayerPrefs.GetInt(SPAWN_AUDIO_CUES_KEY, defaultSpawnAudioCues ? 1 : 0) == 1;
        IsAimAssistEnabled = PlayerPrefs.GetInt(AIM_ASSIST_KEY, defaultAimAssist ? 1 : 0) == 1;
        IsEyeTrackingEnabled = PlayerPrefs.GetInt(EYE_TRACKING_KEY, defaultEyeTracking ? 1 : 0) == 1;

        string calibJson = PlayerPrefs.GetString(EYE_CALIBRATION_KEY, "");
        if (!string.IsNullOrEmpty(calibJson))
        {
            try
            {
                CurrentEyeCalibration = JsonUtility.FromJson<EyeCalibrationData>(calibJson);
            }
            catch
            {
                CurrentEyeCalibration = EyeCalibrationData.Default;
            }
        }
        else
        {
            CurrentEyeCalibration = EyeCalibrationData.Default;
        }
    }

    public void SaveEyeCalibration(EyeCalibrationData data)
    {
        CurrentEyeCalibration = data;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(EYE_CALIBRATION_KEY, json);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void ResetEyeCalibration()
    {
        CurrentEyeCalibration = EyeCalibrationData.Default;
        PlayerPrefs.DeleteKey(EYE_CALIBRATION_KEY);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public Vector2 ApplyEyeCalibration(Vector2 rawGaze)
    {
        // Default ergonomic active head zone: [0.30, 0.70] in X, [0.30, 0.70] in Y
        float minX = 0.30f;
        float maxX = 0.70f;
        float minY = 0.30f;
        float maxY = 0.70f;

        if (CurrentEyeCalibration.isCalibrated)
        {
            minX = CurrentEyeCalibration.minX;
            maxX = CurrentEyeCalibration.maxX;
            minY = CurrentEyeCalibration.minY;
            maxY = CurrentEyeCalibration.maxY;

            if (Mathf.Abs(maxX - minX) < 0.04f)
            {
                minX = 0.30f;
                maxX = 0.70f;
            }
            if (Mathf.Abs(maxY - minY) < 0.04f)
            {
                minY = 0.30f;
                maxY = 0.70f;
            }
        }

        float calibratedX = Mathf.Clamp01(Mathf.InverseLerp(minX, maxX, rawGaze.x));
        float calibratedY = Mathf.Clamp01(Mathf.InverseLerp(minY, maxY, rawGaze.y));

        return new Vector2(calibratedX, calibratedY);
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

    public void SetNoMouseGameplayEnabled(bool enabled)
    {
        IsNoMouseGameplayEnabled = enabled;
        if (enabled)
        {
            IsEyeTrackingEnabled = false;
            PlayerPrefs.SetInt(EYE_TRACKING_KEY, 0);
        }
        else
        {
            IsShowMoleKeyCombosEnabled = false;
            PlayerPrefs.SetInt(SHOW_MOLE_KEY_COMBOS_KEY, 0);
        }
        PlayerPrefs.SetInt(NO_MOUSE_GAMEPLAY_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetKeyboardControlMode(KeyboardControlMode mode)
    {
        CurrentKeyboardControlMode = mode;
        PlayerPrefs.SetInt(KEYBOARD_CONTROL_MODE_KEY, (int)mode);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void CycleKeyboardControlMode()
    {
        int nextMode = ((int)CurrentKeyboardControlMode + 1) % Enum.GetValues(typeof(KeyboardControlMode)).Length;
        SetKeyboardControlMode((KeyboardControlMode)nextMode);
    }

    public void SetShowMoleKeyCombosEnabled(bool enabled)
    {
        if (!IsNoMouseGameplayEnabled)
        {
            enabled = false;
        }
        IsShowMoleKeyCombosEnabled = enabled;
        PlayerPrefs.SetInt(SHOW_MOLE_KEY_COMBOS_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetSpawnAudioCuesEnabled(bool enabled)
    {
        IsSpawnAudioCuesEnabled = enabled;
        PlayerPrefs.SetInt(SPAWN_AUDIO_CUES_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetAimAssistEnabled(bool enabled)
    {
        IsAimAssistEnabled = enabled;
        if (enabled)
        {
            IsEyeTrackingEnabled = false;
            PlayerPrefs.SetInt(EYE_TRACKING_KEY, 0);
            SentisEyeTracker.Shutdown();
        }
        PlayerPrefs.SetInt(AIM_ASSIST_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetEyeTrackingEnabled(bool enabled)
    {
        IsEyeTrackingEnabled = enabled;
        if (enabled)
        {
            IsAimAssistEnabled = false;
            PlayerPrefs.SetInt(AIM_ASSIST_KEY, 0);

            IsNoMouseGameplayEnabled = false;
            IsShowMoleKeyCombosEnabled = false;
            PlayerPrefs.SetInt(NO_MOUSE_GAMEPLAY_KEY, 0);
            PlayerPrefs.SetInt(SHOW_MOLE_KEY_COMBOS_KEY, 0);

            SentisEyeTracker.EnsureInstance();
        }
        else
        {
            SentisEyeTracker.Shutdown();
        }
        PlayerPrefs.SetInt(EYE_TRACKING_KEY, enabled ? 1 : 0);
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
        SetNoMouseGameplayEnabled(defaultNoMouseGameplay);
        SetKeyboardControlMode(defaultKeyboardControlMode);
        SetShowMoleKeyCombosEnabled(defaultShowMoleKeyCombos);
        SetSpawnAudioCuesEnabled(defaultSpawnAudioCues);
        SetAimAssistEnabled(defaultAimAssist);
        SetEyeTrackingEnabled(defaultEyeTracking);
    }
}
