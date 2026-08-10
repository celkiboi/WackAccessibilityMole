using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [SerializeField]
    GameObject standardEnemy;

    [SerializeField]
    GameObject ground;

    [SerializeField]
    TextMeshProUGUI countdownText;
    [SerializeField]
    TextMeshProUGUI timeRemainingText;

    void Start()
    {
        Instance = this;
        isGameFinished = false;
        Time.timeScale = 1f;

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

    bool TrySpawnEnemy()
    {
        if (ground == null) return false;

        Ground groundScript = ground.GetComponent<Ground>();
        if (groundScript != null && groundScript.SpawnEnemy(standardEnemy, currentEnemyLifetime))
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

    IEnumerator StartCountdown()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);
        if (timeRemainingText != null)
            timeRemainingText.gameObject.SetActive(false);

        if (countdownText != null) countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (timeRemainingText != null)
            timeRemainingText.gameObject.SetActive(true);

        hasCountdownFinished = true;
        UpdateUI();
    }

    public void KillEnemy(IEnemyBehaviour enemy)
    {
        if (isGameFinished) return;

        score += enemy.ScoreValue * comboMultiplier;
        comboMultiplier++;
        UpdateUI();
    }

    public void OnEnemyEscaped(IEnemyBehaviour enemy)
    {
        if (isGameFinished) return;

        currentHealth--;
        comboMultiplier = 1;
        UpdateUI();

        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        isGameFinished = true;
        Time.timeScale = 0f;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = $"GAME OVER\nFinal Score: {score}";
        }
    }

    private void UpdateUI()
    {
        if (timeRemainingText != null)
        {
            timeRemainingText.text = $"Score: {score}  |  Lives: {Mathf.Max(0, currentHealth)}/{maxHealth}  |  Combo: x{comboMultiplier}";
        }
    }
}
