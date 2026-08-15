using System.Collections.Generic;
using UnityEngine;

public static class ScoreRepository
{
    private const string SCORE_LOG_HISTORY_KEY = "Accessibility_ScoreLogHistory";

    public static List<ScoreLogEntry> GetScoreLogs()
    {
        string json = PlayerPrefs.GetString(SCORE_LOG_HISTORY_KEY, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return new List<ScoreLogEntry>();
        }

        try
        {
            ScoreLogWrapper wrapper = JsonUtility.FromJson<ScoreLogWrapper>(json);
            return wrapper != null && wrapper.entries != null ? wrapper.entries : new List<ScoreLogEntry>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ScoreRepository] Failed to parse score log JSON: {ex.Message}");
            return new List<ScoreLogEntry>();
        }
    }

    public static void SaveScoreLog(int score, int maxCombo, int enemiesHit, int totalEnemiesSpawned)
    {
        List<ScoreLogEntry> logs = GetScoreLogs();
        ScoreLogEntry newEntry = new ScoreLogEntry(score, maxCombo, enemiesHit, totalEnemiesSpawned);
        logs.Add(newEntry);

        ScoreLogWrapper wrapper = new ScoreLogWrapper { entries = logs };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SCORE_LOG_HISTORY_KEY, json);
        PlayerPrefs.Save();

        Debug.Log($"[ScoreRepository] Logged score: {score} (Enemies Hit: {enemiesHit}, Max Combo: x{maxCombo})");
    }

    public static int GetHighScore()
    {
        List<ScoreLogEntry> logs = GetScoreLogs();
        int maxScore = 0;
        foreach (ScoreLogEntry entry in logs)
        {
            if (entry.score > maxScore)
            {
                maxScore = entry.score;
            }
        }
        return maxScore;
    }

    /// <summary>
    /// Resets/empties the score log repository completely.
    /// </summary>
    public static void ClearAllScores()
    {
        PlayerPrefs.DeleteKey(SCORE_LOG_HISTORY_KEY);
        PlayerPrefs.Save();
        Debug.Log("[ScoreRepository] All score logs have been reset and repository emptied.");
    }
}
