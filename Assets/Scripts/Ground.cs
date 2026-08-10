using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    GameObject[] groundTiles;
    float[] lastSpawnTimes;

    [SerializeField]
    GameObject groundPrefab;

    [SerializeField]
    int totalRows = 4;
    [SerializeField]
    int totalCols = 4;

    [SerializeField]
    float startingWidth = -5;
    [SerializeField]
    float startingHeight = 5;
    [SerializeField]
    float endingWidth = 5;
    [SerializeField]
    float endingHeight = -5;

    public static int NumberOfRows { get; private set; }
    public static int NumberOfCols { get; private set; }

    void Start()
    {
        float totalWidth = Mathf.Abs(endingWidth - startingWidth);
        float totalHeight = Mathf.Abs(endingHeight - startingHeight);

        float tileHeight = totalHeight / totalRows;
        float tileWidth = totalWidth / totalCols;

        groundTiles = new GameObject[totalRows * totalCols];
        lastSpawnTimes = new float[totalRows * totalCols];

        for (int i = 0; i < groundTiles.Length; i++)
        {
            groundTiles[i] = Instantiate(groundPrefab, this.transform);
            
            int row = i / totalCols;
            int col = i % totalCols;

            groundTiles[i].name = $"GroundTile{row}{col}";

            float width = startingWidth + col * tileWidth;
            float height = startingHeight - row * tileHeight;
            groundTiles[i].transform.position = new Vector3(width, height, 0);
            groundTiles[i].transform.localScale = new Vector3(tileWidth, tileHeight, 1);
            
            int rowSortingOrder = row * 10;

            SpriteRenderer tileSr = groundTiles[i].GetComponent<SpriteRenderer>();
            if (tileSr != null)
            {
                tileSr.sortingOrder = rowSortingOrder;

                SpriteMask mask = groundTiles[i].GetComponent<SpriteMask>();
                if (mask == null)
                {
                    mask = groundTiles[i].AddComponent<SpriteMask>();
                }
                mask.sprite = tileSr.sprite;
                mask.isCustomRangeActive = true;
                mask.backSortingOrder = rowSortingOrder;
                mask.frontSortingOrder = rowSortingOrder + 5;
            }

            lastSpawnTimes[i] = -100f;
        }

        NumberOfCols = totalCols;
        NumberOfRows = totalRows;
    }

    public bool SpawnEnemy(GameObject enemy, float lifetime)
    {
        List<int> availableIndices = new List<int>();

        for (int i = 0; i < groundTiles.Length; i++)
        {
            if (groundTiles[i].transform.childCount == 0)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return false;
        }

        float totalWeight = 0f;
        float[] weights = new float[availableIndices.Count];

        for (int i = 0; i < availableIndices.Count; i++)
        {
            int tileIdx = availableIndices[i];
            float timeElapsed = Time.time - lastSpawnTimes[tileIdx];
            weights[i] = Mathf.Max(0.1f, timeElapsed * timeElapsed + 1.0f);
            totalWeight += weights[i];
        }

        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        int chosenTileIndex = availableIndices[0];

        for (int i = 0; i < availableIndices.Count; i++)
        {
            currentSum += weights[i];
            if (randomVal <= currentSum)
            {
                chosenTileIndex = availableIndices[i];
                break;
            }
        }

        lastSpawnTimes[chosenTileIndex] = Time.time;
        GameObject enemyObject = Instantiate(enemy, groundTiles[chosenTileIndex].transform);
        enemyObject.transform.Translate(new Vector3(0, 0, -0.001f));

        int tileRow = chosenTileIndex / totalCols;
        int enemySortingOrder = tileRow * 10 + 1;

        MoleRiseAnimation riseAnim = enemyObject.GetComponent<MoleRiseAnimation>();
        if (riseAnim == null)
        {
            riseAnim = enemyObject.AddComponent<MoleRiseAnimation>();
        }
        riseAnim.SetupMasking(enemySortingOrder);

        IEnemyBehaviour enemyScript = enemyObject.GetComponent<IEnemyBehaviour>();
        if (enemyScript != null)
        {
            enemyScript.OnEnemyEscaped += GameManager.Instance.OnEnemyEscaped;
            enemyScript.Initialize(lifetime);
        }

        return true;
    }

    public bool SpawnEnemy(GameObject enemy)
    {
        return SpawnEnemy(enemy, 2.0f);
    }

    public void ClearAllEnemies()
    {
        for (int i = 0; i < groundTiles.Length; i++)
        {
            if (groundTiles[i].transform.childCount > 0)
            {
                foreach (Transform child in groundTiles[i].transform)
                {
                    IEnemyBehaviour enemy = child.GetComponent<IEnemyBehaviour>();
                    if (enemy != null)
                    {
                        GameManager.Instance.AddScore(enemy.ScoreValue);
                    }
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
