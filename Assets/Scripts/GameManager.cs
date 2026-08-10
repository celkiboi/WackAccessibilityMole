using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField]
    float timeRemaining = 30;
    bool hasCountdownFinished = false;
    [SerializeField]
    float timeBetweenEnemies = 3;
    [SerializeField]
    float enemyPerKillSpawnTimeDecrease = 0.3f;
    float lastTimeEnemySpawned = 0;

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
        StartCoroutine(StartCountdown());
        lastTimeEnemySpawned = timeRemaining;
        Instance = this;
    }

    void Update()
    {
        if (hasCountdownFinished)
        {
            timeRemaining -= Time.deltaTime;
            timeRemainingText.text = $"{timeRemaining:F2}";
        }

        if (timeRemaining <= 0)
        {
            isGameFinished = true;
            Time.timeScale = 0;
        }

        if (timeRemaining <= lastTimeEnemySpawned - timeBetweenEnemies)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        lastTimeEnemySpawned = timeRemaining;
        timeBetweenEnemies -= enemyPerKillSpawnTimeDecrease;

        ground.GetComponent<Ground>().SpawnEnemy(standardEnemy);
    }

    IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);
        timeRemainingText.gameObject.SetActive(false);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);
        timeRemainingText.gameObject.SetActive(true);
        hasCountdownFinished = true;
    }

    public void KillEnemy(IEnemyBehaviour enemy)
    {
        timeRemaining += enemy.TimeExtension;
        lastTimeEnemySpawned += enemy.TimeExtension;
        Destroy(enemy.GameObject);
    }
}
