using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUIController : MonoBehaviour
{
    [Header("Header & Summary UI")]
    [SerializeField]
    private TextMeshProUGUI highScoreText;
    [SerializeField]
    private TextMeshProUGUI totalGamesText;
    [SerializeField]
    private TextMeshProUGUI totalEnemiesHitText;

    [Header("Container & Item Template")]
    [SerializeField]
    private Transform scoreListContainer;
    [SerializeField]
    private GameObject scoreEntryPrefab;
    [SerializeField]
    private TextMeshProUGUI noScoresText;

    [Header("Buttons")]
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Button clearHistoryButton;

    private void OnEnable()
    {
        InitializeButtons();
        EnsureContainerLayout();
        RefreshUI();
    }

    private void Start()
    {
        InitializeButtons();
        EnsureContainerLayout();
        RefreshUI();
    }

    private void InitializeButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.RemoveAllListeners();
            clearHistoryButton.onClick.AddListener(OnClearHistoryClicked);
        }
    }

    private Transform TargetContentTransform
    {
        get
        {
            if (scoreListContainer == null) return null;
            ScrollRect sr = scoreListContainer.GetComponent<ScrollRect>();
            if (sr != null && sr.content != null)
            {
                return sr.content;
            }
            return scoreListContainer;
        }
    }

    private void EnsureContainerLayout()
    {
        Transform contentTransform = TargetContentTransform;
        if (contentTransform == null) return;

        VerticalLayoutGroup vgroup = contentTransform.GetComponent<VerticalLayoutGroup>();
        if (vgroup == null)
        {
            vgroup = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        vgroup.padding = new RectOffset(8, 8, 8, 8);
        vgroup.spacing = 10f;
        vgroup.childControlWidth = true;
        vgroup.childControlHeight = false;
        vgroup.childForceExpandWidth = true;
        vgroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentTransform.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void RefreshUI()
    {
        EnsureContainerLayout();

        List<ScoreLogEntry> logs = ScoreRepository.GetScoreLogs();
        Transform contentTransform = TargetContentTransform;

        if (contentTransform != null)
        {
            foreach (Transform child in contentTransform)
            {
                if (noScoresText != null && child == noScoresText.transform)
                {
                    continue;
                }
                Destroy(child.gameObject);
            }
        }

        if (logs == null || logs.Count == 0)
        {
            if (highScoreText != null)
                highScoreText.text = "HIGH SCORE: 0";
            if (totalGamesText != null)
                totalGamesText.text = "GAMES PLAYED: 0";
            if (totalEnemiesHitText != null)
                totalEnemiesHitText.text = "TOTAL HIT: 0";

            if (scoreListContainer != null)
            {
                scoreListContainer.gameObject.SetActive(false);
            }

            if (noScoresText != null)
            {
                noScoresText.gameObject.SetActive(true);
                noScoresText.text = "No score history recorded yet.\nPlay a game to see your scores!";
            }

            return;
        }

        if (scoreListContainer != null)
        {
            scoreListContainer.gameObject.SetActive(true);
        }

        if (noScoresText != null)
        {
            noScoresText.gameObject.SetActive(false);
        }

        int highestScore = 0;
        int sumEnemiesHit = 0;

        foreach (ScoreLogEntry entry in logs)
        {
            if (entry.score > highestScore)
            {
                highestScore = entry.score;
            }
            sumEnemiesHit += entry.enemiesHit;
        }

        if (highScoreText != null)
            highScoreText.text = $"HIGH SCORE: {highestScore:N0}";
        if (totalGamesText != null)
            totalGamesText.text = $"GAMES PLAYED: {logs.Count}";
        if (totalEnemiesHitText != null)
            totalEnemiesHitText.text = $"TOTAL HIT: {sumEnemiesHit:N0}";

        List<ScoreLogEntry> sortedLogs = new List<ScoreLogEntry>(logs);
        sortedLogs.Sort((a, b) => b.score.CompareTo(a.score));

        int rank = 1;
        foreach (ScoreLogEntry entry in sortedLogs)
        {
            CreateScoreEntryCard(entry, rank++);
        }

        Canvas.ForceUpdateCanvases();
        if (TargetContentTransform is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void CreateScoreEntryCard(ScoreLogEntry entry, int rank)
    {
        Transform contentTransform = TargetContentTransform;
        if (contentTransform == null)
            return;

        if (scoreEntryPrefab != null)
        {
            GameObject itemObj = Instantiate(scoreEntryPrefab, contentTransform);

            // Ensure instantiated prefab has LayoutElement minHeight so VerticalLayoutGroup positions it properly
            LayoutElement itemLayout = itemObj.GetComponent<LayoutElement>();
            if (itemLayout == null)
            {
                itemLayout = itemObj.AddComponent<LayoutElement>();
                itemLayout.minHeight = 85f;
                itemLayout.preferredHeight = 85f;
                itemLayout.flexibleWidth = 1f;
            }

            TextMeshProUGUI txt = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = FormatEntryText(entry, rank);
            }
        }
        else
        {
            GameObject cardObj = new GameObject($"ScoreEntry_{rank}");
            cardObj.transform.SetParent(contentTransform, false);

            Image bg = cardObj.AddComponent<Image>();
            bg.color = rank == 1
                ? new Color(0.25f, 0.22f, 0.05f, 0.95f)
                : new Color(0.12f, 0.14f, 0.18f, 0.95f);

            LayoutElement layout = cardObj.AddComponent<LayoutElement>();
            layout.minHeight = 85f;
            layout.preferredHeight = 85f;
            layout.flexibleWidth = 1f;

            VerticalLayoutGroup cardInnerGroup = cardObj.AddComponent<VerticalLayoutGroup>();
            cardInnerGroup.padding = new RectOffset(12, 12, 8, 8);
            cardInnerGroup.childControlWidth = true;
            cardInnerGroup.childControlHeight = true;
            cardInnerGroup.childForceExpandWidth = true;
            cardInnerGroup.childForceExpandHeight = true;

            GameObject textObj = new GameObject("ScoreCardText");
            textObj.transform.SetParent(cardObj.transform, false);

            TextMeshProUGUI tmpro = textObj.AddComponent<TextMeshProUGUI>();
            tmpro.text = FormatEntryText(entry, rank);
            tmpro.fontSize = 14f;
            tmpro.color = Color.white;
            tmpro.richText = true;
            tmpro.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private string FormatEntryText(ScoreLogEntry entry, int rank)
    {
        string rankTag = rank == 1 ? "<color=#FFD700><b>#1 HIGH SCORE</b></color>" : $"<b>#{rank}</b>";
        float accuracy = entry.totalEnemiesSpawned > 0
            ? ((float)entry.enemiesHit / entry.totalEnemiesSpawned) * 100f
            : 0f;

        string headerLine = $"{rankTag}  |  <color=#00E5FF><b>{entry.score:N0} PTS</b></color>  |  Max Combo: <color=#FFD700>x{entry.maxCombo}</color>";
        string detailsLine = $"Hit: {entry.enemiesHit}/{entry.totalEnemiesSpawned} ({accuracy:F1}%)  |  Date: {entry.timestamp}";

        List<string> badges = new List<string>();
        var snap = entry.accessibilitySettings;

        if (snap.gameSpeedMultiplier != 1.0f)
            badges.Add($"Speed {snap.gameSpeedMultiplier:F1}x");
        if (snap.isAimAssistEnabled)
            badges.Add("Aim Assist");
        if (snap.isEyeTrackingEnabled)
            badges.Add("Eye Gaze & Blink");
        if (snap.isNoMouseGameplayEnabled)
            badges.Add($"No-Mouse ({snap.keyboardControlMode})");
        if (snap.colorblindMode != "Off" && !string.IsNullOrEmpty(snap.colorblindMode))
            badges.Add($"Colorblind: {snap.colorblindMode}");
        if (snap.isSpawnAudioCuesEnabled)
            badges.Add("Audio Cues");
        if (!snap.isScreenShakeEnabled)
            badges.Add("No Shake");
        if (!snap.isScreenFlashesEnabled)
            badges.Add("No Flashes");

        string badgesStr = badges.Count > 0
            ? $"<color=#88FF88>[{string.Join("] [", badges)}]</color>"
            : "<color=#AAAAAA>[Standard Controls]</color>";

        return $"{headerLine}\n<size=85%>{detailsLine}\nAccessibility: {badgesStr}</size>";
    }

    public void OnClearHistoryClicked()
    {
        ScoreRepository.ClearAllScores();
        RefreshUI();
    }

    public void OnBackClicked()
    {
        MainMenuManager menuManager = FindAnyObjectByType<MainMenuManager>();
        if (menuManager != null)
        {
            menuManager.CloseScores();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
