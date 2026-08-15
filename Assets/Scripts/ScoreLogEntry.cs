using System;
using UnityEngine;

[System.Serializable]
public struct AccessibilitySettingsSnapshot
{
    public bool isScreenShakeEnabled;
    public bool isScreenFlashesEnabled;
    public float gameSpeedMultiplier;
    public string colorblindMode;
    public float colorblindIntensity;
    public bool isNoMouseGameplayEnabled;
    public string keyboardControlMode;
    public bool isShowMoleKeyCombosEnabled;
    public bool isSpawnAudioCuesEnabled;
    public bool isAimAssistEnabled;

    public static AccessibilitySettingsSnapshot CaptureCurrent()
    {
        AccessibilitySettingsSnapshot snapshot = new AccessibilitySettingsSnapshot();
        if (SettingsManager.Instance != null)
        {
            snapshot.isScreenShakeEnabled = SettingsManager.Instance.IsScreenShakeEnabled;
            snapshot.isScreenFlashesEnabled = SettingsManager.Instance.IsScreenFlashesEnabled;
            snapshot.gameSpeedMultiplier = SettingsManager.Instance.GameSpeedMultiplier;
            snapshot.colorblindMode = SettingsManager.Instance.CurrentColorblindMode.ToString();
            snapshot.colorblindIntensity = SettingsManager.Instance.ColorblindIntensity;
            snapshot.isNoMouseGameplayEnabled = SettingsManager.Instance.IsNoMouseGameplayEnabled;
            snapshot.keyboardControlMode = SettingsManager.Instance.CurrentKeyboardControlMode.ToString();
            snapshot.isShowMoleKeyCombosEnabled = SettingsManager.Instance.IsShowMoleKeyCombosEnabled;
            snapshot.isSpawnAudioCuesEnabled = SettingsManager.Instance.IsSpawnAudioCuesEnabled;
            snapshot.isAimAssistEnabled = SettingsManager.Instance.IsAimAssistEnabled;
        }
        return snapshot;
    }
}

[System.Serializable]
public class ScoreLogEntry
{
    public string id;
    public int score;
    public int maxCombo;
    public int enemiesHit;
    public int totalEnemiesSpawned;
    public string timestamp;
    public AccessibilitySettingsSnapshot accessibilitySettings;

    public ScoreLogEntry()
    {
        id = Guid.NewGuid().ToString();
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public ScoreLogEntry(int score, int maxCombo, int enemiesHit, int totalEnemiesSpawned) : this()
    {
        this.score = score;
        this.maxCombo = maxCombo;
        this.enemiesHit = enemiesHit;
        this.totalEnemiesSpawned = totalEnemiesSpawned;
        this.accessibilitySettings = AccessibilitySettingsSnapshot.CaptureCurrent();
    }
}

[System.Serializable]
public class ScoreLogWrapper
{
    public System.Collections.Generic.List<ScoreLogEntry> entries = new System.Collections.Generic.List<ScoreLogEntry>();
}
