/*
** Author      : Yogi
** CreateDate  : 2022-55-24 07:55:56
** Description : 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    #region Config

    public MapConfig mapConfig;

    public string GameSeed;
    public Vector2Int GameMapSize = new Vector2Int(100, 100);

    public SpriteRenderer GirdPrefab;
    public SpriteRenderer PointPrefab;

    #endregion

    #region Tmp

    private MapData MapData;
    private Dictionary<GameObject, bool> PointDataDic ;

    
    #endregion

    #region UnityFunc

    void OnEnable()
    {
        MapTemplateCache.Init(mapConfig);

        this.MapData = MapGenerator.Generate(GameSeed, GameMapSize, out debugPointArray);

        var maxX = debugPointArray.GetLength(0);
        var maxY = debugPointArray.GetLength(1);
        PointDataDic = new Dictionary<GameObject, bool>();
        for (int i = 0; i < maxX; i++)
        {
            for (int j = 0; j < maxY; j++)
            {
                var pointGO = GameObject.Instantiate(PointPrefab, this.transform);
                pointGO.sortingOrder = 10;
                pointGO.transform.position = new Vector3(i + .5f, j + .5f, -1);
                if (debugPointArray[i, j])
                {
                    pointGO.color = Color.white;
                    PointDataDic.Add(pointGO.gameObject,true);
                }
                else
                {
                    pointGO.color = Color.red;
                    PointDataDic.Add(pointGO.gameObject,false);
                }
            }
        }

        StartCoroutine(InitMap());
    }

    private bool[,] debugPointArray;

    private void OnDrawGizmos()
    {
        return;
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

    IEnumerator InitMap()
    {
        var floor = this.MapData.Data[GridLayer.Floor];
        var maxX = floor.GetLength(0);
        var maxY = floor.GetLength(0);
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

                    yield return null;
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
        if (Input.GetMouseButton(0))
        {
            //TODO: 优化 这里不能这么写
            Vector3 origin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hitData = Physics2D.Raycast(origin, Vector2.zero, 100, LayerMask.GetMask("Point"));
            if (hitData.collider != null)
            {
                Vector2 changeIndexFloat = (hitData.transform.position - Vector3.one * .5f);
                Vector2Int index = new Vector2Int((int) changeIndexFloat.x, (int) changeIndexFloat.y);
                var go = hitData.collider.gameObject;
                PointDataDic[go] = !PointDataDic[go];
                go.GetComponent<SpriteRenderer>().color = PointDataDic[go] ? Color.white : Color.red;
                ChangePoint(GridLayer.Floor, index, PointDataDic[go]);
            }
        }
    }

    void OnDestroy()
    {
    }

    #endregion

    #region Public

    public void ChangePoint(GridLayer gridLayer, Vector2Int changIndex, bool isActive)
    {
        var tmp = this.MapData.Data[gridLayer];
        int x = changIndex.x;
        int y = changIndex.y;
        if (isActive)
        {
            tmp[x, y].OpenDoor += (int) Open.TopRight;
            tmp[x + 1, y].OpenDoor += (int) Open.TopLeft;
            tmp[x, y + 1].OpenDoor += (int) Open.DownRight;
            tmp[x + 1, y + 1].OpenDoor += (int) Open.DownLeft;
        }
        else
        {
            tmp[x, y].OpenDoor -= (int) Open.TopRight;
            if (tmp[x, y].OpenDoor <= 0)
            {
                tmp[x, y].OpenDoor = Open.Empty;
            }
            tmp[x + 1, y].OpenDoor -= (int) Open.TopLeft;
            if (tmp[x+ 1, y].OpenDoor <= 0)
            {
                tmp[x+ 1, y].OpenDoor = Open.Empty;
            }
            tmp[x, y + 1].OpenDoor -= (int) Open.DownRight;
            if (tmp[x, y+ 1].OpenDoor <= 0)
            {
                tmp[x, y+ 1].OpenDoor = Open.Empty;
            }
            tmp[x + 1, y + 1].OpenDoor -= (int) Open.DownLeft;
            if (tmp[x+ 1, y+ 1].OpenDoor <= 0)
            {
                tmp[x+ 1, y+ 1].OpenDoor = Open.Empty;
            }
        }

        tmp[x, y].UpdateDisplay();
        tmp[x + 1, y].UpdateDisplay();
        tmp[x, y + 1].UpdateDisplay();
        tmp[x + 1, y + 1].UpdateDisplay();
    }

    #endregion

    #region Private

    #endregion
}