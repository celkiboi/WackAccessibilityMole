using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Health & Lives Settings")]
    [SerializeField]
    int maxHealth = 3;
    int currentHealth;

    [Header("Score & Combo Settings")]
    int score = 0;
    int comboMultiplier = 1;
    int maxComboAchieved = 1;
    int totalEnemiesHit = 0;

    [Header("Spawn Interval Pacing")]
    [SerializeField]
    float initialTimeBetweenEnemies = 2.5f;
    [SerializeField]
    float minTimeBetweenEnemies = 0.5f;
    [SerializeField]
    float spawnDecayFactor = 0.95f;

    [Header("Enemy Lifetime Pacing")]
    [SerializeField]
    float initialEnemyLifetime = 2.2f;
    [SerializeField]
    float minEnemyLifetime = 1.4f;
    [SerializeField]
    float lifetimeDecayFactor = 0.98f;

    bool hasCountdownFinished = false;
    float spawnTimer = 0f;
    float currentSpawnInterval;
    float currentEnemyLifetime;
    int totalEnemiesSpawned = 0;

    public static bool isGameFinished { get; private set; } = false;

    [System.Serializable]
    public struct EnemySpawnConfig
    {
        public GameObject enemyPrefab;
        public float weight;
    }

    [Header("Enemy Types")]
    [SerializeField]
    GameObject standardEnemy;
    [SerializeField]
    List<EnemySpawnConfig> enemySpawnPool;

    [SerializeField]
    GameObject ground;

    [Header("UI Elements")]
    [SerializeField]
    TextMeshProUGUI countdownText;
    [SerializeField]
    TextMeshProUGUI scoreText;
    [SerializeField]
    TextMeshProUGUI livesText;
    [SerializeField]
    TextMeshProUGUI comboText;
    [SerializeField]
    TextMeshProUGUI actionMessageText;
    [SerializeField]
    GameObject playAgainButton;
    [SerializeField]
    GameObject mainMenuButton;
    [SerializeField]
    string mainMenuSceneName = "MainMenu";

    void Start()
    {
        Instance = this;
        isGameFinished = false;
        hasCountdownFinished = false;

        float speed = SettingsManager.Instance != null ? SettingsManager.Instance.GameSpeedMultiplier : 1f;
        Time.timeScale = speed > 0.05f ? speed : 1f;

        if (ground == null)
        {
            Ground g = FindFirstObjectByType<Ground>();
            if (g != null) ground = g.gameObject;
        }

        if (playAgainButton != null)
        {
            playAgainButton.SetActive(false);
            Button btn = playAgainButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(RestartGame);
            }
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.SetActive(false);
            Button btn = mainMenuButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToMainMenu);
            }
        }

        currentHealth = maxHealth;
        score = 0;
        comboMultiplier = 1;
        maxComboAchieved = 1;
        totalEnemiesHit = 0;
        totalEnemiesSpawned = 0;
        currentSpawnInterval = initialTimeBetweenEnemies;
        currentEnemyLifetime = initialEnemyLifetime;

        StartCoroutine(StartCountdown());
    }

    void Update()
    {
        if (hasCountdownFinished && !isGameFinished)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentSpawnInterval)
            {
                if (TrySpawnEnemy())
                {
                    spawnTimer = 0f;
                }
            }

            if (SettingsManager.Instance != null && SettingsManager.Instance.IsNoMouseGameplayEnabled && !SettingsManager.Instance.IsEyeTrackingEnabled)
            {
                HandleKeyboardInput();
            }

            CheckForMisclick();
        }
    }

    public int KeyboardCursorRow { get; private set; } = 0;
    public int KeyboardCursorCol { get; private set; } = 0;

    private void HandleKeyboardInput()
    {
        if (SettingsManager.Instance == null) return;

        if (SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.GridCursor)
        {
            int maxRows = Ground.NumberOfRows > 0 ? Ground.NumberOfRows : 4;
            int maxCols = Ground.NumberOfCols > 0 ? Ground.NumberOfCols : 4;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                KeyboardCursorRow = Mathf.Max(0, KeyboardCursorRow - 1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                KeyboardCursorRow = Mathf.Min(maxRows - 1, KeyboardCursorRow + 1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                KeyboardCursorCol = Mathf.Max(0, KeyboardCursorCol - 1);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                KeyboardCursorCol = Mathf.Min(maxCols - 1, KeyboardCursorCol + 1);
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (ground != null)
                {
                    AimAssistController aimController = ground.GetComponent<AimAssistController>();
                    if (aimController != null)
                    {
                        aimController.TriggerClickPunch();
                    }
                }
                SmashTile(KeyboardCursorRow, KeyboardCursorCol);
            }
        }
        else
        {
            bool wDown = Input.GetKey(KeyCode.W);
            bool aDown = Input.GetKey(KeyCode.A);
            bool sDown = Input.GetKey(KeyCode.S);
            bool dDown = Input.GetKey(KeyCode.D);
            bool eDown = Input.GetKey(KeyCode.E);

            bool upDown = Input.GetKey(KeyCode.UpArrow);
            bool leftDown = Input.GetKey(KeyCode.LeftArrow);
            bool downDown = Input.GetKey(KeyCode.DownArrow);
            bool rightDown = Input.GetKey(KeyCode.RightArrow);

            bool wPressed = Input.GetKeyDown(KeyCode.W);
            bool aPressed = Input.GetKeyDown(KeyCode.A);
            bool sPressed = Input.GetKeyDown(KeyCode.S);
            bool dPressed = Input.GetKeyDown(KeyCode.D);
            bool ePressed = Input.GetKeyDown(KeyCode.E);

            bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
            bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
            bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);
            bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);

            int col = -1;
            if (wDown) col = 0;
            else if (aDown) col = 1;
            else if (sDown) col = 2;
            else if (dDown) col = 3;
            else if (eDown) col = 4;

            int row = -1;
            if (upDown) row = 0;
            else if (leftDown) row = 1;
            else if (downDown) row = 2;
            else if (rightDown) row = 3;

            bool colJustPressed = wPressed || aPressed || sPressed || dPressed || ePressed;
            bool rowJustPressed = upPressed || leftPressed || downPressed || rightPressed;

            if (row >= 0 && col >= 0 && (rowJustPressed || colJustPressed))
            {
                SmashTile(row, col);
            }
        }
    }

    public void SmashTile(int row, int col)
    {
        if (ground == null) return;
        Ground groundScript = ground.GetComponent<Ground>();
        if (groundScript != null)
        {
            groundScript.SmashTileAt(row, col);
        }
    }

    private void CheckForMisclick()
    {
        if (SettingsManager.Instance != null &&
            (SettingsManager.Instance.IsAimAssistEnabled ||
             SettingsManager.Instance.IsEyeTrackingEnabled ||
             (SettingsManager.Instance.IsNoMouseGameplayEnabled && SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.GridCursor)))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && Camera.main != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.GetComponentInParent<IEnemyBehaviour>() == null &&
                    hit.collider.GetComponent<IEnemyBehaviour>() == null)
                {
                    OnMisclick();
                }
            }
        }
    }

    public void OnMisclick()
    {
        if (comboMultiplier > 1)
        {
            comboMultiplier = 1;
            UpdateUI();
        }
    }

    GameObject GetRandomEnemyPrefab()
    {
        if (enemySpawnPool == null || enemySpawnPool.Count == 0)
        {
            return standardEnemy;
        }

        float totalWeight = 0;
        foreach (var config in enemySpawnPool)
        {
            totalWeight += config.weight;
        }

        float randomVal = Random.Range(0, totalWeight);
        float currentSum = 0;
        foreach (var config in enemySpawnPool)
        {
            currentSum += config.weight;
            if (randomVal <= currentSum)
            {
                return config.enemyPrefab;
            }
        }
        
        return standardEnemy;
    }

    bool TrySpawnEnemy()
    {
        if (ground == null) return false;

        Ground groundScript = ground.GetComponent<Ground>();
        GameObject prefabToSpawn = GetRandomEnemyPrefab();

        if (groundScript != null && groundScript.SpawnEnemy(prefabToSpawn, currentEnemyLifetime))
        {
            totalEnemiesSpawned++;
            currentSpawnInterval = Mathf.Max(
                minTimeBetweenEnemies,
                initialTimeBetweenEnemies * Mathf.Pow(spawnDecayFactor, totalEnemiesSpawned)
            );
            currentEnemyLifetime = Mathf.Max(
                minEnemyLifetime,
                initialEnemyLifetime * Mathf.Pow(lifetimeDecayFactor, totalEnemiesSpawned)
            );
            return true;
        }

        return false;
    }

    // Kill Message Pools
    private readonly string[] standardMessages = new string[] { "SMASH!", "KILL!", "SLAY!", "CRUSH!", "WHACK!" };
    private readonly string[] goldenMessages = new string[] { "THE KING IS DEAD!", "ET TU, BRUTE?", "GOLDEN DOWN!", "ROYALTY SLAIN!" };
    private readonly string[] bombMessages = new string[] { "OH NOOO!", "BOOOM!", "BOMBA!", "KABOOM!" };
    private readonly string[] toughHitMessages = new string[] { "AGAIN!", "AGAIN!", "KEEP HITTING!" };
    private readonly string[] toughSlainMessages = new string[] { "FINALLY!?", "FALLEN AT LAST!", "DEFEATED!" };

    // Escape / Despawn Message Pools (When NOT clicked)
    private readonly string[] bombDefusedMessages = new string[] { "BOMB DEFUSED", "SAFE FOR NOW!", "DISARMED!", "WHEW, CLOSE ONE!" };
    private readonly string[] nukeEscapedMessages = new string[] { "WE LIVE TO SEE ANOTHER DAY", "EVACUATED IN TIME!", "MISSILE DISARMED!", "CRISIS AVERTED!" };
    private readonly string[] regularEscapedMessages = new string[] { "GOT AWAY!", "MISSED IT!", "TOO SLOW!", "ESCAPED!", "SNEAKY MOLE!" };
    private readonly string[] toughEscapedMessages = new string[] { "UNDEFEATED", "UNSTOPPABLE!", "STILL STANDING!", "TOO TOUGH!" };
    private readonly string[] goldenEscapedMessages = new string[] { "THE KING RISES", "ROYAL ESCAPE!", "SLIPPED AWAY!", "TOO FAST!" };

    // Kill Colors
    private readonly Color standardColor = new Color(0.2f, 0.85f, 1.0f);
    private readonly Color goldenColor = new Color(1.0f, 0.85f, 0.0f);
    private readonly Color bombColor = new Color(1.0f, 0.25f, 0.2f);
    private readonly Color toughHitColor = new Color(1.0f, 0.55f, 0.0f);
    private readonly Color toughSlainColor = new Color(0.9f, 0.25f, 1.0f);
    private readonly Color nukeColor = new Color(0.2f, 1.0f, 0.35f);

    // Escape Colors (Distinct warning & relief tones)
    private readonly Color bombDefusedColor = new Color(0.2f, 0.9f, 0.6f);   // Mint / Relief Green
    private readonly Color nukeEscapedColor = new Color(0.1f, 0.8f, 0.9f);   // Teal / Cyan Relief
    private readonly Color regularEscapedColor = new Color(1.0f, 0.4f, 0.4f); // Coral Warning
    private readonly Color toughEscapedColor = new Color(0.85f, 0.15f, 0.25f); // Crimson Warning
    private readonly Color goldenEscapedColor = new Color(0.85f, 0.65f, 0.15f); // Bronze / Amber Warning

    private Coroutine actionMessageCoroutine;

    private void SetHudActive(bool active)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(active);
        if (livesText != null) livesText.gameObject.SetActive(active);
        if (comboText != null) comboText.gameObject.SetActive(active);
        if (actionMessageText != null && !active) actionMessageText.gameObject.SetActive(false);
    }

    public void ShowActionMessage(string text, Color color, float duration = 1.5f)
    {
        if (actionMessageText == null) return;
        if (actionMessageCoroutine != null)
        {
            StopCoroutine(actionMessageCoroutine);
        }
        actionMessageCoroutine = StartCoroutine(ActionMessageRoutine(text, color, duration));
    }

    private IEnumerator ActionMessageRoutine(string text, Color color, float duration)
    {
        actionMessageText.gameObject.SetActive(true);
        actionMessageText.text = text;
        actionMessageText.color = color;

        Transform textTransform = actionMessageText.transform;
        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * 1.3f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (elapsed < 0.25f)
            {
                float scaleT = Mathf.Sin((elapsed / 0.25f) * Mathf.PI);
                textTransform.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);
            }
            else
            {
                textTransform.localScale = originalScale;
            }

            if (t > 0.6f)
            {
                float alphaT = (t - 0.6f) / 0.4f;
                Color c = color;
                c.a = Mathf.Lerp(1f, 0f, alphaT);
                actionMessageText.color = c;
            }

            yield return null;
        }

        actionMessageText.gameObject.SetActive(false);
        textTransform.localScale = originalScale;
    }

    public void ShowStandardSlainMessage()
    {
        string msg = standardMessages[Random.Range(0, standardMessages.Length)];
        ShowActionMessage(msg, standardColor, 1.2f);
    }

    public void ShowGoldenSlainMessage()
    {
        string msg = goldenMessages[Random.Range(0, goldenMessages.Length)];
        ShowActionMessage(msg, goldenColor, 1.8f);
    }

    public void ShowBombExplodedMessage()
    {
        string msg = bombMessages[Random.Range(0, bombMessages.Length)];
        ShowActionMessage(msg, bombColor, 1.5f);
    }

    public void ShowToughHitMessage()
    {
        string msg = toughHitMessages[Random.Range(0, toughHitMessages.Length)];
        ShowActionMessage(msg, toughHitColor, 1.0f);
    }

    public void ShowToughSlainMessage()
    {
        string msg = toughSlainMessages[Random.Range(0, toughSlainMessages.Length)];
        ShowActionMessage(msg, toughSlainColor, 1.6f);
    }

    public void ShowBombDefusedMessage()
    {
        string msg = bombDefusedMessages[Random.Range(0, bombDefusedMessages.Length)];
        ShowActionMessage(msg, bombDefusedColor, 1.5f);
    }

    public void ShowNukeEscapedMessage()
    {
        string msg = nukeEscapedMessages[Random.Range(0, nukeEscapedMessages.Length)];
        ShowActionMessage(msg, nukeEscapedColor, 1.8f);
    }

    public void ShowRegularEscapedMessage()
    {
        string msg = regularEscapedMessages[Random.Range(0, regularEscapedMessages.Length)];
        ShowActionMessage(msg, regularEscapedColor, 1.2f);
    }

    public void ShowToughEscapedMessage()
    {
        string msg = toughEscapedMessages[Random.Range(0, toughEscapedMessages.Length)];
        ShowActionMessage(msg, toughEscapedColor, 1.5f);
    }

    public void ShowGoldenEscapedMessage()
    {
        string msg = goldenEscapedMessages[Random.Range(0, goldenEscapedMessages.Length)];
        ShowActionMessage(msg, goldenEscapedColor, 1.6f);
    }

    public void ShowNukeFalloutMessage(float duration = 3.0f)
    {
        if (actionMessageCoroutine != null)
        {
            StopCoroutine(actionMessageCoroutine);
        }
        actionMessageCoroutine = StartCoroutine(FalloutCountdownRoutine(duration));
    }

    private IEnumerator FalloutCountdownRoutine(float duration)
    {
        if (actionMessageText == null) yield break;

        actionMessageText.gameObject.SetActive(true);
        actionMessageText.color = nukeColor;

        Transform textTransform = actionMessageText.transform;
        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * 1.35f;

        float remaining = duration;
        float elapsed = 0f;

        while (remaining > 0f)
        {
            actionMessageText.text = $"<b>FALLOUT! ({remaining:F1}s)</b>";

            if (elapsed < 0.25f)
            {
                float scaleT = Mathf.Sin((elapsed / 0.25f) * Mathf.PI);
                textTransform.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);
            }
            else
            {
                textTransform.localScale = originalScale;
            }

            yield return null;
            remaining -= Time.deltaTime;
            elapsed += Time.deltaTime;
        }

        actionMessageText.gameObject.SetActive(false);
        textTransform.localScale = originalScale;
    }

    IEnumerator StartCountdown()
    {
        SetHudActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        if (countdownText != null) countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);
        if (countdownText != null) countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        if (countdownText != null) countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        SetHudActive(true);
        hasCountdownFinished = true;
        UpdateUI();
    }

    public void KillEnemy(IEnemyBehaviour enemy)
    {
        if (isGameFinished) return;

        score += enemy.ScoreValue * comboMultiplier;
        totalEnemiesHit++;
        comboMultiplier++;
        maxComboAchieved = Mathf.Max(maxComboAchieved, comboMultiplier);
        UpdateUI();
        TriggerScoreGainFlash();

        if (enemy is GoldenEnemyBehaviour)
        {
            ShowGoldenSlainMessage();
        }
        else if (enemy is ToughEnemyBehaviour)
        {
            ShowToughSlainMessage();
        }
        else if (enemy is StandardEnemyBehaviour)
        {
            ShowStandardSlainMessage();
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
        TriggerScoreGainFlash();
    }

    public void TriggerNuke()
    {
        if (ground != null)
        {
            Ground groundScript = ground.GetComponent<Ground>();
            if (groundScript != null)
            {
                groundScript.ClearAllEnemies();
            }
        }
        
        spawnTimer = -3f;
        ShowNukeFalloutMessage(3.0f);
    }

    private Coroutine cameraShakeCoroutine;

    public void TriggerCameraShake(float duration = 0.2f, float magnitude = 0.15f)
    {
        if (Camera.main == null) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.IsScreenShakeEnabled) return;
        if (cameraShakeCoroutine != null)
        {
            StopCoroutine(cameraShakeCoroutine);
        }
        cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPos;
    }

    private Coroutine scoreFlashCoroutine;

    private void TriggerScoreGainFlash()
    {
        if (scoreFlashCoroutine != null)
        {
            StopCoroutine(scoreFlashCoroutine);
        }
        scoreFlashCoroutine = StartCoroutine(ScoreGainFlashRoutine());
    }

    private IEnumerator ScoreGainFlashRoutine()
    {
        if (scoreText == null) yield break;

        Transform textTransform = scoreText.transform;
        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * 1.2f;
        Color neonGreen = new Color(0.2f, 1.0f, 0.4f);

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scaleT = Mathf.Sin(t * Mathf.PI);
            textTransform.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);

            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            scoreText.color = allowFlashes ? Color.Lerp(neonGreen, Color.white, t) : Color.white;

            yield return null;
        }

        textTransform.localScale = originalScale;
        scoreText.color = Color.white;
    }

    private Coroutine healthFlashCoroutine;

    private void TriggerHealthLostFlash()
    {
        if (healthFlashCoroutine != null)
        {
            StopCoroutine(healthFlashCoroutine);
        }
        healthFlashCoroutine = StartCoroutine(HealthLostFlashRoutine());
    }

    private IEnumerator HealthLostFlashRoutine()
    {
        TextMeshProUGUI targetText = livesText;
        if (targetText == null) yield break;

        Transform textTransform = targetText.transform;
        Vector3 originalScale = textTransform.localScale;
        Vector3 punchScale = originalScale * 1.25f;

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scaleT = Mathf.Sin(t * Mathf.PI);
            textTransform.localScale = Vector3.Lerp(originalScale, punchScale, scaleT);

            bool allowFlashes = SettingsManager.Instance == null || SettingsManager.Instance.IsScreenFlashesEnabled;
            float flashT = allowFlashes ? Mathf.PingPong(elapsed * 14f, 1f) : 0f;
            targetText.color = Color.Lerp(Color.white, Color.red, flashT);

            yield return null;
        }

        textTransform.localScale = originalScale;
        targetText.color = Color.white;
    }

    public void HitBomb()
    {
        if (isGameFinished) return;

        currentHealth--;
        comboMultiplier = 1;
        UpdateUI();
        TriggerHealthLostFlash();
        ShowBombExplodedMessage();

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    public void OnEnemyEscaped(IEnemyBehaviour enemy)
    {
        if (isGameFinished) return;

        if (enemy is BombEnemyBehaviour)
        {
            ShowBombDefusedMessage();
        }
        else if (enemy is NukeEnemyBehaviour)
        {
            ShowNukeEscapedMessage();
        }
        else if (enemy is GoldenEnemyBehaviour)
        {
            ShowGoldenEscapedMessage();
        }
        else if (enemy is ToughEnemyBehaviour)
        {
            ShowToughEscapedMessage();
        }
        else if (enemy is StandardEnemyBehaviour)
        {
            ShowRegularEscapedMessage();
        }

        if (enemy.PenalizeOnEscape)
        {
            currentHealth--;
            comboMultiplier = 1;
            UpdateUI();
            TriggerHealthLostFlash();

            if (currentHealth <= 0)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        isGameFinished = true;
        Time.timeScale = 0f;

        ScoreRepository.SaveScoreLog(score, maxComboAchieved, totalEnemiesHit, totalEnemiesSpawned);
        int highScore = ScoreRepository.GetHighScore();

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = $"GAME OVER\nFinal Score: {score:N0}\nHigh Score: {highScore:N0}";
        }

        if (playAgainButton != null)
        {
            playAgainButton.SetActive(true);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.SetActive(true);
        }
    }

    public void RestartGame()
    {
        SentisEyeTracker.ShutdownCamera();
        Time.timeScale = SettingsManager.Instance != null ? SettingsManager.Instance.GameSpeedMultiplier : 1f;
        isGameFinished = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SentisEyeTracker.ShutdownCamera();
        Time.timeScale = 1f;
        isGameFinished = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void UpdateUI()
    {
        int safeHealth = Mathf.Max(0, currentHealth);
        
        string heartsStr = "";
        for (int i = 1; i <= maxHealth; i++)
        {
            if (i <= safeHealth)
            {
                heartsStr += "<color=#FF3344>♥</color> ";
            }
            else
            {
                heartsStr += "<color=#444444>♡</color> ";
            }
        }
        heartsStr = heartsStr.Trim();

        string formattedScore = $"{score:N0}";
        string comboStr = comboMultiplier > 1 
            ? $"<color=#FFD700><b>x{comboMultiplier}</b></color>" 
            : $"x{comboMultiplier}";

        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {formattedScore}";
        }

        if (livesText != null)
        {
            livesText.text = $"LIVES: {heartsStr}";
        }

        if (comboText != null)
        {
            comboText.text = $"COMBO: {comboStr}";
        }
    }
}
