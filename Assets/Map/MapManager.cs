/*
** Author      : Yogi
** CreateDate  : 2022-55-24 07:55:56
** Description : 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    #region Config

    public MapConfig mapConfig;

    public string GameSeed;
    public Vector2Int GameMapSize = new Vector2Int(100, 100);

    public SpriteRenderer GirdPrefab;

    #endregion

    #region Tmp

    private MapData MapData;

    #endregion

    #region UnityFunc

    void OnEnable()
    {
        MapTemplateCache.Init(mapConfig);

        this.MapData = MapGenerator.Generate(GameSeed, GameMapSize, out debugPointArray);

        StartCoroutine(InitTileMap());
    }

    private bool[,] debugPointArray;

    private void OnDrawGizmos()
    {
        if (debugPointArray == null) return;
        var maxX = debugPointArray.GetLength(0);
        var maxY = debugPointArray.GetLength(1);
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
            {
                if (debugPointArray[i, j])
                {
                    Gizmos.DrawWireSphere(new Vector3(i + 0.5f, j + .5f, 0), .1f);
                }
            }
        }
    }

    IEnumerator InitTileMap()
    {
        var floor = this.MapData.Data[GridLayer.Floor];
        var maxX = floor.GetLength(0);
        var maxY = floor.GetLength(0);
        Debug.Log(this.MapData.Data.Values.Count);
        foreach (var gridData in this.MapData.Data.Values)
        {
            for (int i = 0; i < gridData.GetLength(0); i++)
            {
                for (int j = 0; j < gridData.GetLength(1); j++)
                {
                    var renderer = GameObject.Instantiate(GirdPrefab, this.transform);
                    renderer.sprite = gridData[i, j].DisplaySprite;
                    renderer.transform.position = new Vector3(i, j, 0);
                    gridData[i, j].OnDisplayChange += (x) => renderer.sprite = x;

                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }

    void OnDisable()
    {
    }

    void Awake()
    {
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void OnDestroy()
    {
    }

    #endregion

    #region Public

    #endregion

    #region Private

    #endregion
}