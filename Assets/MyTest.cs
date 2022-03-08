using Sunset.SceneManagement;
using UnityEngine;

public class MyTest : MonoBehaviour
{
    /*
    public Renderer r;

    private ComputeBuffer argsBuffer;

    public DepthTextureGenerator depthGenerator;

    // Start is called before the first frame update
    void Start()
    {
        m = r.GetComponent<MeshFilter>().mesh;
        
        this.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        Shader.EnableKeyword("HiZCull");
    }

    float minX = 1, minY = 1, minZ = 1, maxX = -1, maxY = -1, maxZ = -1; //NDC下新的的AABB各个参数


    private Mesh m;
    // Update is called once per frame
    void Update()
    {
        List<Matrix4x4> matrix = new List<Matrix4x4>() { r.localToWorldMatrix };
        Graphics.DrawMeshInstancedIndirect(m,0,r.material,new Bounds(Vector3.zero,Vector3.one),argsBuffer);
        
        var cam = Camera.main;
        Vector3 center = r.bounds.center;
        Vector3 extens = r.bounds.extents;
        
        Vector3[] BoundsVerts = new Vector3[8];
        var minBounds = BoundsVerts[0] = center - extens;
        var maxBounds = BoundsVerts[1] = center + extens;
        BoundsVerts[2] = new Vector3(maxBounds.x, maxBounds.y, minBounds.z);
        BoundsVerts[3] = new Vector3(maxBounds.x, minBounds.y, maxBounds.z);
        BoundsVerts[4] = new Vector3(maxBounds.x, minBounds.y, minBounds.z);
        BoundsVerts[5] = new Vector3(minBounds.x, maxBounds.y, maxBounds.z);
        BoundsVerts[6] = new Vector3(minBounds.x, maxBounds.y, minBounds.z);
        BoundsVerts[7] = new Vector3(minBounds.x, minBounds.y, maxBounds.z);

        Matrix4x4 vpMatrix = (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix);

        minX = 1;
        minY = 1;
        minZ = 1;
        maxX = -1;
        maxY = -1;
        maxZ = -1;

        for (var i = 0; i < BoundsVerts.Length; i++)
        {
            // 计算八个顶点的ClipSpace坐标 用于深度剔除
            Vector4 posCenter = BoundsVerts[i];
            posCenter.w = 1;
            var clipSpace = vpMatrix * posCenter;
            //计算该ndc下的AABB
            Vector3 ndc = new Vector3(clipSpace.x, clipSpace.y, clipSpace.z) / clipSpace.w;
            minX = Mathf.Min(minX, ndc.x);
            minY = Mathf.Min(minY, ndc.y);
            minZ = Mathf.Min(minZ, ndc.z);
            maxX = Mathf.Max(maxX, ndc.x);
            maxY = Mathf.Max(maxY, ndc.y);
            maxZ = Mathf.Max(maxZ, ndc.z);
        }

        // Vector4 posCenter = center;
        // posCenter.w = 1;
        // var clipSpace = vpMatrix * posCenter;
        // Vector3 ndc = new Vector3(clipSpace.x, clipSpace.y, clipSpace.z) / clipSpace.w;
        // minX = Mathf.Min(minX, ndc.x);
        // minY = Mathf.Min(minY, ndc.y);
        // minZ = Mathf.Min(minZ, ndc.z);
        // maxX = Mathf.Max(maxX, ndc.x);
        // maxY = Mathf.Max(maxY, ndc.y);
        // maxZ = Mathf.Max(maxZ, ndc.z);

        Vector3[] bv = new Vector3[8];
        for (var i = 0; i < BoundsVerts.Length; i++)
        {
            bv[i] = BoundsVerts[i];
        }

        // r.enabled = CheckExtent.CheckBoundIsInCamera(Camera.main, ref bv, out _);
    }

    public bool isDisplayDepth = true;
    public Rect DepthTexRect = new Rect(0,0,1920/2,1080/2);
    private void OnGUI()
    {
        if (isDisplayDepth)
        {
            GUI.DrawTexture(DepthTexRect, depthGenerator.depthTexture);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 2);


        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(minX, minY, minZ), 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(maxX, maxY, maxZ), 0.1f);
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
    
    */

    
    /* ----------------------------------------------Shadow Cull-------------------------------------------------------
    public Renderer t;
    public Light l;
    private Camera cam;
    private float MaxShadowDistance = 30;

    private void OnEnable()
    {
        cam = this.GetComponent<Camera>();
    }

    Vector3[] BoundsVerts;
    private Vector4[] planes;

    private void Update()
    {
        t.enabled = false;

        planes = CheckExtent.GetFrustumPlane(cam);

        BoundsVerts = new Vector3[8];
        var min = BoundsVerts[0] = t.bounds.center - t.bounds.extents;
        var max = BoundsVerts[1] = t.bounds.center + t.bounds.extents;
        BoundsVerts[2] = new Vector3(max.x, max.y, min.z);
        BoundsVerts[3] = new Vector3(max.x, min.y, max.z);
        BoundsVerts[4] = new Vector3(max.x, min.y, min.z);
        BoundsVerts[5] = new Vector3(min.x, max.y, max.z);
        BoundsVerts[6] = new Vector3(min.x, max.y, min.z);
        BoundsVerts[7] = new Vector3(min.x, min.y, max.z);

        // 1.计算交点
        // 2.如果交点 不在任何平面的背面 那么需要投影
        points = new Vector3[8 * 6];
        for (int i = 0; i < 8; i++)
        {
            var p0 = BoundsVerts[i];
            Ray r = new Ray(p0, l.transform.forward);
            for (int j = 0; j < 6; j++)
            {
                var p1 = planes[j];
                // 射线与平面求交点 需要同时满足射线方程与平面方程
                if (RayCastToPlane(p1, r, out var t))
                {
                    if (t <= MaxShadowDistance)
                    {
                        var hitPoint = r.origin + t * r.direction;
                        points[i + j] = hitPoint;
                    }
                }
            }
        }

        bool visilbe = true;
        t.enabled = visilbe;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        foreach (var p in points)
        {
            if (p != Vector3.zero)
            {
                bool isOutSide = false;
                for (int k = 0; k < 6; k++)
                {
                    var p2 = planes[k];
                    if (CheckExtent.IsOutsideThePlane(p2, p))
                    {
                        isOutSide = true;
                        break;
                    }
                }

                if (isOutSide)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawWireSphere(p, 0.1f);

                foreach (var boundsVert in BoundsVerts)
                {
                    Gizmos.DrawWireCube(boundsVert, Vector3.one * .1f);
                    Gizmos.DrawLine(boundsVert, p);
                }
            }
        }

        Gizmos.color = Color.white;
        foreach (var boundsVert in BoundsVerts)
        {
            Gizmos.DrawRay(boundsVert, l.transform.forward * 1000);
        }

        Gizmos.matrix = cam.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
    }

    private Vector3[] points = new Vector3[1];

    /// <summary>
    /// 求射线与平面的距离
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="ray"></param>
    /// <param name="enter"></param>
    /// <returns></returns>
    public bool RayCastToPlane(Vector4 plane, Ray ray, out float enter)
    {
        enter = 0;
        // 灯光方向 点乘 面的法线
        float vdot = Vector3.Dot(ray.direction, plane);
        // 顶点位置 点乘 面的法线
        float ndot = -Vector3.Dot(ray.origin, plane) - plane.w;
        if (Mathf.Approximately(vdot, 0))
        {
            return false;
        }

        enter = ndot / vdot;
        return enter > 0;
    }
    */
}