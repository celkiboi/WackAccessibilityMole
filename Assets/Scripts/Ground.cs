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
    public static float TileWidth { get; private set; }
    public static float TileHeight { get; private set; }

    public GameObject[] GroundTiles => groundTiles;
    private TileHoleEffect[] tileHoleEffects;

    private readonly string[] rowKeys = new string[] { "↑", "←", "↓", "→" };
    private readonly string[] colKeys = new string[] { "W", "A", "S", "D", "E" };

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
        HandleSettingsChanged();
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
    }

    private void HandleSettingsChanged()
    {
        EnsureControllers();
        UpdateKeyHintVisibility();
    }

    private static Sprite holeVisualSprite;
    private static Sprite holeMaskSprite;

    private static Sprite GetOrCreateHoleVisualSprite()
    {
        if (holeVisualSprite != null) return holeVisualSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radiusX = size * 0.45f;
        float radiusY = size * 0.32f;

        Color holeInnerColor = new Color(0.08f, 0.06f, 0.05f, 1f);
        Color holeRimColor = new Color(0.18f, 0.14f, 0.10f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                float distSq = dx * dx + dy * dy;

                if (distSq <= 1.0f)
                {
                    float t = Mathf.Sqrt(distSq);
                    pixels[y * size + x] = Color.Lerp(holeInnerColor, holeRimColor, t * t);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        holeVisualSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return holeVisualSprite;
    }

    private static Sprite GetOrCreateHoleMaskSprite()
    {
        if (holeMaskSprite != null) return holeMaskSprite;

        int width = 128;
        int height = 160;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, 64f);
        float radiusX = width * 0.45f;
        float radiusY = width * 0.32f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;

                bool isInside = false;
                if (dy >= 0f)
                {
                    isInside = Mathf.Abs(dx) <= 1.0f;
                }
                else
                {
                    isInside = (dx * dx + dy * dy) <= 1.0f;
                }

                pixels[y * width + x] = isInside ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        holeMaskSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 64f / height), 128f);
        return holeMaskSprite;
    }

    void Start()
    {
        float totalWidth = Mathf.Abs(endingWidth - startingWidth);
        float totalHeight = Mathf.Abs(endingHeight - startingHeight);

        float tileHeight = totalHeight / totalRows;
        float tileWidth = totalWidth / totalCols;

        NumberOfRows = totalRows;
        NumberOfCols = totalCols;

        TileWidth = tileWidth;
        TileHeight = tileHeight;

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
            }

            // Hole Visual
            GameObject holeObj = new GameObject("Hole_Visual");
            holeObj.transform.SetParent(groundTiles[i].transform, false);
            holeObj.transform.localPosition = new Vector3(0, -0.05f, -0.001f);
            holeObj.transform.localScale = new Vector3(0.85f, 0.55f, 1f);

            SpriteRenderer holeSr = holeObj.AddComponent<SpriteRenderer>();
            holeSr.sprite = GetOrCreateHoleVisualSprite();
            holeSr.sortingOrder = rowSortingOrder + 1;

            GameObject maskObj = new GameObject("Hole_Mask");
            maskObj.transform.SetParent(groundTiles[i].transform, false);
            maskObj.transform.localPosition = new Vector3(0, -0.05f, -0.002f);
            maskObj.transform.localScale = new Vector3(0.85f, 0.55f, 1f);

            SpriteMask mask = maskObj.AddComponent<SpriteMask>();
            mask.sprite = GetOrCreateHoleMaskSprite();
            mask.isCustomRangeActive = true;
            mask.backSortingOrder = rowSortingOrder + 1;
            mask.frontSortingOrder = rowSortingOrder + 9;

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

        EnsureControllers();
        UpdateKeyHintVisibility();
    }

    private void EnsureControllers()
    {
        bool isAimAssist = SettingsManager.Instance != null && SettingsManager.Instance.IsAimAssistEnabled;
        if (isAimAssist)
        {
            if (GetComponent<AimAssistController>() == null)
            {
                gameObject.AddComponent<AimAssistController>();
            }
        }
        else
        {
            AimAssistController assist = GetComponent<AimAssistController>();
            if (assist != null) Destroy(assist);
        }

        bool isEyeTracking = SettingsManager.Instance != null && SettingsManager.Instance.IsEyeTrackingEnabled;
        if (isEyeTracking)
        {
            if (GetComponent<SentisEyeTracker>() == null)
            {
                gameObject.AddComponent<SentisEyeTracker>();
            }
        }
        else
        {
            SentisEyeTracker tracker = GetComponent<SentisEyeTracker>();
            if (tracker != null) Destroy(tracker);
        }
    }

    public bool GetTileAtWorldPosition(Vector2 worldPos, out int row, out int col)
    {
        row = -1;
        col = -1;
        if (groundTiles == null || groundTiles.Length == 0) return false;

        int bestIndex = -1;
        float minSqDistance = float.MaxValue;

        for (int i = 0; i < groundTiles.Length; i++)
        {
            if (groundTiles[i] == null) continue;

            Vector3 tilePos = groundTiles[i].transform.position;
            float dx = worldPos.x - tilePos.x;
            float dy = worldPos.y - tilePos.y;
            float sqDist = dx * dx + dy * dy;

            if (sqDist < minSqDistance)
            {
                minSqDistance = sqDist;
                bestIndex = i;
            }
        }

        float maxAllowedDist = Mathf.Max(TileWidth, TileHeight) * 1.5f;
        if (bestIndex >= 0 && Mathf.Sqrt(minSqDistance) <= maxAllowedDist)
        {
            row = bestIndex / totalCols;
            col = bestIndex % totalCols;
            return true;
        }

        return false;
    }

    private void Update()
    {
        UpdateKeyHintVisibility();
    }

    public void UpdateKeyHintVisibility()
    {
        bool isMatrixMode = SettingsManager.Instance == null || 
                            SettingsManager.Instance.CurrentKeyboardControlMode == KeyboardControlMode.MatrixCombo;

        bool showHeaders = SettingsManager.Instance != null && 
                           SettingsManager.Instance.IsNoMouseGameplayEnabled && 
                           isMatrixMode;

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
                                     isMatrixMode && 
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
        enemyObject.transform.localPosition = new Vector3(0, -1.0f, -0.003f);

        int tileRow = chosenTileIndex / totalCols;
        int enemySortingOrder = tileRow * 10 + 2;

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

        if (SettingsManager.Instance != null && SettingsManager.Instance.IsSpawnAudioCuesEnabled)
        {
            int tileCol = chosenTileIndex % totalCols;
            if (SpawnAudioCueManager.Instance != null)
            {
                SpawnAudioCueManager.Instance.PlaySpawnCue(tileRow, tileCol, enemyScript);
            }
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
