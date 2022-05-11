/*
** Author      : Yogi
** CreateDate  : 2022-56-24 07:56:23
** Description : 地图生成器, 用于生成随机地图
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapGenerator
{
    #region Config

    #endregion

    #region Tmp

    #endregion

    #region Public

    public static MapData Generate(string MapSeed, Vector2Int mapSize, out bool[,] pointArray)
    {
        MapData result = new MapData
        {
            Data = new Dictionary<GridLayer, GridData[,]>()
        };

        Random.InitState(MapSeed.GetHashCode());
        // 地图生成算法

        //TODO: 基于需求生成不同的层
        result.Data.Add(GridLayer.Floor, GenerateLayer(GridLayer.Floor, GridMaterial.Mat0, mapSize, out pointArray));
        // result.Data.Add(GridLayer.Floor, GenerateLayer(GridLayer.Floor, GridMaterial.Mat1, mapSize, out pointArray));

        return result;
    }

    #endregion

    #region Private

    private static GridData[,] GenerateLayer(GridLayer layer, GridMaterial material, Vector2Int mapSize,
        out bool[,] pointArray)
    {
        // Point点集合 数量为边长-1 true为有实体 false为无实体
        pointArray = new bool[mapSize.x - 1, mapSize.y - 1];
        for (int y = 0; y < mapSize.y - 1; y++)
        {
            for (int x = 0; x < mapSize.x - 1; x++)
            {
                // 1/2概率生成Grid概率
                if (Random.Range(0, 1.0f) < 0.6f)
                {
                    pointArray[x, y] = true;
                }
                else
                {
                    pointArray[x, y] = false;
                }
            }
        }

        Open[,] openArray = ParseToOpenArray(pointArray);

        return ParseToGridData(layer, material, openArray);
    }

    private static Open[,] ParseToOpenArray(bool[,] points)
    {
        var xMax = points.GetLength(0) + 1;
        var yMax = points.GetLength(1) + 1;
        Open[,] result = new Open[xMax, yMax];

        // 遍历点
        for (int x = 0; x < xMax - 1; x++)
        {
            for (int y = 0; y < yMax - 1; y++)
            {
                if (points[x, y])
                {
                    result[x, y] += (int) Open.TopRight;
                    result[x + 1, y] += (int) Open.TopLeft;
                    result[x, y + 1] += (int) Open.DownRight;
                    result[x + 1, y + 1] += (int) Open.DownLeft;
                }
            }
        }

        return result;
    }

    private static GridData[,] ParseToGridData(GridLayer layer, GridMaterial gridMaterial, Open[,] gridArray)
    {
        var maxX = gridArray.GetLength(0);
        var maxY = gridArray.GetLength(1);
        GridData[,] result = new GridData[maxX, maxY];
        for (int i = 0; i < maxY; i++)
        {
            for (int j = 0; j < maxX; j++)
            {
                var newData = new GridData(layer, gridMaterial, gridArray[i, j]);
                newData.UpdateDisplay();
                result[i, j] = newData;
            }
        }

        return result;
    }

    #endregion
}