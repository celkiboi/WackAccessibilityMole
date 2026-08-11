using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Ground : MonoBehaviour
{
    GameObject[] groundTiles;
    GameObject[] keyHintHeaderObjects;
    GameObject[] keyHintTileObjects;
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

    private readonly string[] rowKeys = new string[] { "↑", "←", "↓", "→" };
    private readonly string[] colKeys = new string[] { "W", "A", "S", "D", "E" };

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += UpdateKeyHintVisibility;
        }
        UpdateKeyHintVisibility();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= UpdateKeyHintVisibility;
        }
    }

    void Start()
    {
        float totalWidth = Mathf.Abs(endingWidth - startingWidth);
        float totalHeight = Mathf.Abs(endingHeight - startingHeight);

        float tileHeight = totalHeight / totalRows;
        float tileWidth = totalWidth / totalCols;

        groundTiles = new GameObject[totalRows * totalCols];
        keyHintTileObjects = new GameObject[totalRows * totalCols];
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

            // Create per-tile key combo badge (e.g., "D →")
            GameObject tileBadgeObj = new GameObject($"TileKeyCombo_{row}_{col}");
            tileBadgeObj.transform.SetParent(groundTiles[i].transform, false);
            tileBadgeObj.transform.localPosition = new Vector3(0, 0.35f, -0.01f);

            TextMeshPro textMesh = tileBadgeObj.AddComponent<TextMeshPro>();
            string rStr = row < rowKeys.Length ? rowKeys[row] : $"{row}";
            string cStr = col < colKeys.Length ? colKeys[col] : $"{col}";
            textMesh.text = $"{cStr} {rStr}";
            textMesh.fontSize = 3.5f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = new Color(1.0f, 0.95f, 0.3f, 0.95f);
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.sortingOrder = rowSortingOrder + 8;

            tileBadgeObj.AddComponent<TileKeyComboEffect>();
            keyHintTileObjects[i] = tileBadgeObj;
            lastSpawnTimes[i] = -100f;
        }

        keyHintHeaderObjects = new GameObject[totalCols + totalRows];
        int headerIdx = 0;

        for (int c = 0; c < totalCols; c++)
        {
            GameObject colHeaderObj = new GameObject($"ColHeader_{c}");
            colHeaderObj.transform.SetParent(this.transform, false);

            float posX = startingWidth + c * tileWidth;
            float posY = startingHeight + tileHeight * 0.55f;
            colHeaderObj.transform.position = new Vector3(posX, posY, -0.1f);

            TextMeshPro textMesh = colHeaderObj.AddComponent<TextMeshPro>();
            textMesh.text = c < colKeys.Length ? colKeys[c] : $"{c}";
            textMesh.fontSize = 4.5f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.sortingOrder = 100;

            keyHintHeaderObjects[headerIdx++] = colHeaderObj;
        }

        for (int r = 0; r < totalRows; r++)
        {
            GameObject rowHeaderObj = new GameObject($"RowHeader_{r}");
            rowHeaderObj.transform.SetParent(this.transform, false);

            float posX = startingWidth + (totalCols - 1) * tileWidth + tileWidth * 0.65f;
            float posY = startingHeight - r * tileHeight;
            rowHeaderObj.transform.position = new Vector3(posX, posY, -0.1f);

            TextMeshPro textMesh = rowHeaderObj.AddComponent<TextMeshPro>();
            textMesh.text = r < rowKeys.Length ? rowKeys[r] : $"{r}";
            textMesh.fontSize = 4.5f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.sortingOrder = 100;

            keyHintHeaderObjects[headerIdx++] = rowHeaderObj;
        }

        NumberOfCols = totalCols;
        NumberOfRows = totalRows;

        UpdateKeyHintVisibility();
    }

    private void Update()
    {
        UpdateKeyHintVisibility();
    }

    public void UpdateKeyHintVisibility()
    {
        bool showHeaders = SettingsManager.Instance != null && SettingsManager.Instance.IsNoMouseGameplayEnabled;
        if (keyHintHeaderObjects != null)
        {
            foreach (GameObject headerObj in keyHintHeaderObjects)
            {
                if (headerObj != null)
                {
                    headerObj.SetActive(showHeaders);
                }
            }
        }

        bool showTileCombosSetting = SettingsManager.Instance != null && 
                                     SettingsManager.Instance.IsNoMouseGameplayEnabled && 
                                     SettingsManager.Instance.IsShowMoleKeyCombosEnabled;

        if (keyHintTileObjects != null && groundTiles != null)
        {
            for (int i = 0; i < groundTiles.Length; i++)
            {
                if (keyHintTileObjects[i] != null)
                {
                    bool hasActiveEnemy = groundTiles[i] != null && 
                                          groundTiles[i].GetComponentInChildren<IEnemyBehaviour>() != null;

                    keyHintTileObjects[i].SetActive(showTileCombosSetting && hasActiveEnemy);
                }
            }
        }
    }

    public void SmashTileAt(int row, int col)
    {
        if (groundTiles == null) return;
        int tileIndex = row * totalCols + col;
        if (tileIndex < 0 || tileIndex >= groundTiles.Length) return;

        IEnemyBehaviour enemy = groundTiles[tileIndex].GetComponentInChildren<IEnemyBehaviour>();
        if (enemy != null)
        {
            enemy.Hit();
        }
        else
        {
            GameManager.Instance.OnMisclick();
        }
    }

    public bool SpawnEnemy(GameObject enemy, float lifetime)
    {
        List<int> availableIndices = new List<int>();

        for (int i = 0; i < groundTiles.Length; i++)
        {
            if (groundTiles[i].GetComponentInChildren<IEnemyBehaviour>() == null)
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
            IEnemyBehaviour enemy = groundTiles[i].GetComponentInChildren<IEnemyBehaviour>();
            if (enemy != null)
            {
                GameManager.Instance.AddScore(enemy.ScoreValue);
                Destroy(enemy.GameObject);
            }
        }
    }
}
