/*
** Author      : Yogi
** CreateDate  : 2022-18-24 08:18:15
** Description : Map数据配置缓存, 配置数据
*/

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class MapTemplateCache
{
    #region Tmp

    /// <summary>
    /// 地图配置数据
    /// </summary>
    public static Dictionary<GridLayer, Dictionary<GridMaterial, GridConfig>>
        GridConfig = new Dictionary<GridLayer, Dictionary<GridMaterial, GridConfig>>();

    #endregion

    #region Public

    public static void Clear()
    {
        GridConfig.Clear();
    }

    public static void Init(MapConfig mapConfig)
    {
        foreach (var gridConfig in mapConfig.Data)
        {
            if (!GridConfig.ContainsKey(gridConfig.Layer))
            {
                GridConfig.Add(gridConfig.Layer, new Dictionary<GridMaterial, GridConfig>());
            }

            var tmp1 = GridConfig[gridConfig.Layer];
            if (!tmp1.ContainsKey(gridConfig.Mat))
            {
                tmp1.Add(gridConfig.Mat, gridConfig);
            }
        }
    }

    public static Sprite GetTexture(GridLayer layer, GridMaterial material, Open open)
    {
        return GridConfig[layer][material].GetSpriteByOpenDoor(open);
    }
    

    #endregion

    #region Private

    #endregion
}