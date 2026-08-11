using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string SCREEN_SHAKE_KEY = "Accessibility_ScreenShake";
    private const string SCREEN_FLASHES_KEY = "Accessibility_ScreenFlashes";
    private const string GAME_SPEED_KEY = "Accessibility_GameSpeed";

    [Header("Default Settings")]
    [SerializeField]
    private bool defaultScreenShake = true;
    [SerializeField]
    private bool defaultScreenFlashes = true;
    [SerializeField]
    private float defaultGameSpeed = 1.0f;

    public bool IsScreenShakeEnabled { get; private set; }
    public bool IsScreenFlashesEnabled { get; private set; }
    public float GameSpeedMultiplier { get; private set; }

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

    public void ResetToDefaults()
    {
        SetScreenShakeEnabled(defaultScreenShake);
        SetScreenFlashesEnabled(defaultScreenFlashes);
        SetGameSpeedMultiplier(defaultGameSpeed);
    }
}
