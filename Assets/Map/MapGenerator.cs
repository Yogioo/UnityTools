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

    public static MapData Generate(string MapSeed, Vector2Int mapSize, out Open[,] DebugGrid)
    {
        MapData result = new MapData();
        result.Data = new Dictionary<GridLayer, GridData[,]>();

        Random.InitState(MapSeed.GetHashCode());
        //TODO: 地图生成算法

        //1. 地面生成

        GridLayer layer = GridLayer.Floor;
        GridMaterial material = GridMaterial.Mat0;

        // grid图集 0为有实体 -1为无实体
        Open[,] gridArray = new Open[mapSize.x, mapSize.y];
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                // 1/2概率生成Grid概率
                if (Random.Range(0, 1.0f) < 0.6f)
                {
                    gridArray[x, y] = Open.All;
                }
                else
                {
                    gridArray[x, y] = Open.Empty;
                }
            }
        }

        // 如果当前格子不满足要求就删除 (要求至少与两面接壤 只有上下接壤or左右接壤无效)
        MinusErrorGrid(ref gridArray);

        DebugGrid = gridArray;

        var layerData = ParseToGridData(layer, material, gridArray);
        result.Data.Add(layer, layerData);

        return result;
    }

    #endregion

    #region Private

    private static void MinusErrorGrid(ref Open[,] gridArray)
    {
        // 递归标识符, 如果当前循环中 有东西是错误的, 那么将会进行下一次递归 直到地图不会有错误为止
        bool isNeedContinue = false;

        int maxX = gridArray.GetLength(0);
        int maxY = gridArray.GetLength(1);

        for (var i = 0; i < maxX; i++)
        {
            for (var j = 0; j < maxY; j++)
            {
                var current = gridArray[i, j];
                if (current == Open.All)
                {
                    bool right, left, top, down;
                    right = left = top = down = false;

                    if (i == 0)
                    {
                        left = true;
                    }
                    else if (i == maxX - 1)
                    {
                        right = true;
                    }
                    else
                    {
                        left = gridArray[i - 1, j] != Open.Empty;
                        right = gridArray[i + 1, j] != Open.Empty;
                    }


                    if (j == 0)
                    {
                        down = true;
                    }
                    else if (j == maxY - 1)
                    {
                        top = true;
                    }
                    else
                    {
                        top = gridArray[i, j + 1] != Open.Empty;
                        down = gridArray[i, j - 1] != Open.Empty;
                    }

                    //当前节点必须至少满足右上 右下 左上 左下之中的一个 否则是无效节点需要移除

                    bool isCorrect = false;
                    if (top & right)
                    {
                        gridArray[i, j] += (int) Open.TopRight;
                        isCorrect = true;
                    }

                    if (down & right)
                    {
                        gridArray[i, j] += (int) Open.DownRight;
                        isCorrect = true;
                    }

                    if (top & left)
                    {
                        gridArray[i, j] += (int) Open.TopLeft;
                        isCorrect = true;
                    }

                    if (down & left)
                    {
                        gridArray[i, j] += (int) Open.DownLeft;
                        isCorrect = true;
                    }

                    if (!isCorrect)
                    {
                        gridArray[i, j] = Open.Empty;
                        isNeedContinue = true;
                    }
                }
            }
        }

        if (isNeedContinue)
        {
            MinusErrorGrid(ref gridArray);
        }
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