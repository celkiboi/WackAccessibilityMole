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
    GameObject playAgainButton;

    void Start()
    {
        Instance = this;
        isGameFinished = false;
        Time.timeScale = 1f;

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

        currentHealth = maxHealth;
        score = 0;
        comboMultiplier = 1;
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

            CheckForMisclick();
        }
    }

    private void CheckForMisclick()
    {
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

    private void SetHudActive(bool active)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(active);
        if (livesText != null) livesText.gameObject.SetActive(active);
        if (comboText != null) comboText.gameObject.SetActive(active);
    }

    IEnumerator StartCountdown()
    {
        SetHudActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        if (countdownText != null) countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.text = "1";
        yield return new WaitForSeconds(1f);

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
        comboMultiplier++;
        UpdateUI();
        TriggerScoreGainFlash();
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
    }

    private Coroutine cameraShakeCoroutine;

    public void TriggerCameraShake(float duration = 0.2f, float magnitude = 0.15f)
    {
        if (Camera.main == null) return;
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

            scoreText.color = Color.Lerp(neonGreen, Color.white, t);

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

            float flashT = Mathf.PingPong(elapsed * 14f, 1f);
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

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    public void OnEnemyEscaped(IEnemyBehaviour enemy)
    {
        if (isGameFinished) return;

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

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = $"GAME OVER\nFinal Score: {score:N0}";
        }

        if (playAgainButton != null)
        {
            playAgainButton.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameFinished = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
