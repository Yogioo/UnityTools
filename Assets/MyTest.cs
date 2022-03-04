using System.Collections;
using System.Collections.Generic;
using Sunset.SceneManagement;
using UnityEngine;

public class MyTest : MonoBehaviour
{
    public Renderer r;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var BoundsVerts = new Vector3[8];
        var min = BoundsVerts[0] = r.bounds.min;
        var max = BoundsVerts[1] = r.bounds.max;
        BoundsVerts[2] = new Vector3(max.x, max.y, min.z);
        BoundsVerts[3] = new Vector3(max.x, min.y, max.z);
        BoundsVerts[4] = new Vector3(max.x, min.y, min.z);
        BoundsVerts[5] = new Vector3(min.x, max.y, max.z);
        BoundsVerts[6] = new Vector3(min.x, max.y, min.z);
        BoundsVerts[7] = new Vector3(min.x, min.y, max.z);

        r.enabled = CheckExtent.CheckBoundIsInCamera(Camera.main, ref BoundsVerts, out _);
    }

    float BoxIntersect(Vector3 extent, Matrix4x4 boxLocalToWorld, Vector3 position, Vector4[] planes)
    {
        float result = 1;
        for (uint i = 0; i < 6; ++i)
        {
            Vector4 plane = planes[i];
            Vector3 absNormal = (boxLocalToWorld.inverse * plane);
            absNormal.x = Mathf.Abs(absNormal.x);
            absNormal.y = Mathf.Abs(absNormal.y);
            absNormal.z = Mathf.Abs(absNormal.z);
            
            var r = ((Vector3.Dot(position, plane) - Vector3.Dot(absNormal, extent)) < -plane.w);
            result *= r ? 1 : 0;
        }

        return result;
    }
}