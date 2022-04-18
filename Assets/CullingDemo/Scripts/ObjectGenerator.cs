using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : MonoBehaviour
{
    public GameObject source;
    public Vector3Int genCount;
    public Vector3 gap;

    [ContextMenu("Generate objects")]
    public void Generate()
    {
        Vector3 start = new Vector3(genCount.x * gap.x, genCount.y * gap.y, genCount.z * gap.z) / 2;

        for (int x = 0; x < genCount.x; x++)
        {
            for (int y = 0; y < genCount.y; y++)
            {
                for (int z = 0; z < genCount.z; z++)
                {
                    GameObject copied = Instantiate(source);

                    copied.name = string.Format("{0},{1},{2}", x, y, z);
                    copied.transform.position = new Vector3(gap.x * x, gap.y * y, gap.z * z) - start;
                    copied.isStatic = true;
                    copied.transform.parent = transform;
                }
            }
        }
    }

    [ContextMenu("Clear objects")]
    public void Clear()
    {
        while(transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }
}
