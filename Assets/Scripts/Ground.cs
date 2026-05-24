using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    GameObject[] groundTiles;
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
        }

        NumberOfCols = totalCols;
        NumberOfRows = totalRows;
    }
    

}
