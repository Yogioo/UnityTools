/*
** Author      : Yogi
** CreateDate  : 2022-56-24 07:56:36
** Description : 地图中的一个格子数据 
*/

using System;
using UnityEngine;

[System.Serializable]
public class GridData
{
    public GridData(GridLayer layer, GridMaterial material, Open open)
    {
        this.Layer = layer;
        this.Material = material;
        this.OpenDoor = open;
    }
    /// <summary>
    /// 当前层
    /// </summary>
    public GridLayer Layer;

    /// <summary>
    /// 当前材质
    /// </summary>
    public GridMaterial Material;

    /// <summary>
    /// 是否连续Flag
    /// </summary>
    public Open OpenDoor;

    /// <summary>
    /// 显示的信息
    /// </summary>
    public Sprite DisplaySprite
    {
        get { return _DisplaySprite; }
        private set
        {
            if (_DisplaySprite != value)
            {
                _DisplaySprite = value;
                OnDisplayChange?.Invoke(value);
            }
        }
    }

    private Sprite _DisplaySprite;

    public delegate void SpriteChangeDelegate(Sprite sprite);

    public SpriteChangeDelegate OnDisplayChange;


    /// <summary>
    /// 基于当前格子的属性 更新格子的显示信息
    /// </summary>
    public void UpdateDisplay()
    {
        DisplaySprite = MapTemplateCache.GetTexture(Layer, Material, OpenDoor);
    }
}

/// <summary>
/// 网格层级
/// </summary>
public enum GridLayer
{
    Floor = 0,
    Room = 1,
    Ceil = 2,
}

/// <summary>
/// 网格材质类型, 会基于Layer不同而变化
/// </summary>
public enum GridMaterial
{
    Mat0 = 1,
    Mat1 = 2,
}

/// <summary>
/// 是否开门的Flag
/// </summary>
[Flags]
public enum Open
{
    /// <summary>
    /// 空
    /// </summary>
    Empty = -1,
    /// <summary>
    ///  全开口
    /// </summary>
    All = 0,
    TopRight = 1,
    TopLeft = 2,
    DownRight = 4,
    DownLeft = 8,
}