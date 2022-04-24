/*
** Author      : Yogi
** CreateDate  : 2022-23-24 08:23:19
** Description : Grid配置文件
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MapConfig : ScriptableObject
{
    #region Config

    [Header("地图配置, 需要保证每个显示贴图至少有15张图哦 后面的图只是保证随机性的")]
    public List<GridConfig> Data;

    #endregion
}

[System.Serializable]
public class GridConfig
{
    public int SID;
    public GridLayer Layer;
    public GridMaterial Mat;
    public List<Sprite> DisplayTextures;

    public Sprite GetSpriteByOpenDoor(Open open)
    {
        var openID = (int)open;
        if (openID < 0)
        {
            return null;
        }
        
        if (openID == 0|| openID >= 15)
        {
            // 随机15~最大数量 + 当前0的贴图
            int maxPlusOne = DisplayTextures.Count + 1;
            var index = Random.Range(15, maxPlusOne);
            if (index == DisplayTextures.Count)
            {
                index = 0;
            }
            return DisplayTextures[index];
        }
        else
        {
            return DisplayTextures[openID];
        }
    }
}