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

    public Tilemap Tilemap;

    #endregion

    #region Tmp

    private MapData MapData;

    #endregion

    #region UnityFunc

    void OnEnable()
    {
        MapTemplateCache.Init(mapConfig);

        this.MapData = MapGenerator.Generate(GameSeed, GameMapSize, out debugGrid);

        InitTileMap();
    }

    void InitTileMap()
    {
        var floor = this.MapData.Data[GridLayer.Floor];
        var maxX = floor.GetLength(0);
        var maxY = floor.GetLength(0);
        Tilemap.size = new Vector3Int(maxX, maxY, 1);
        Tilemap.
        foreach (var gridData in this.MapData.Data.Values)
        {
            for (int i = 0; i < gridData.GetLength(0); i++)
            {
                for (int j = 0; j < gridData.GetLength(1); j++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = gridData[i, j].DisplaySprite;
                    Tilemap.SetTile(new Vector3Int(i, j, 0), tile);
                    gridData[i, j].OnDisplayChange += (x) => tile.sprite = x;
                }
            }
        }
    }

    private Open[,] debugGrid;

    private void OnDrawGizmos()
    {
        if (debugGrid == null)
        {
            return;
        }

        Vector3 size = Vector3.one * .5f;
        for (var x = 0; x < debugGrid.GetLength(0); x++)
        {
            for (var y = 0; y < debugGrid.GetLength(1); y++)
            {
                if (debugGrid[x, y] != Open.Empty)
                {
                    Gizmos.DrawCube(new Vector3(x, 0, y), size);
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