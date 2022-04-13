using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class MeshDecal2 : MonoBehaviour
{
    private MeshFilter meshFilter
    {
        get
        {
            if (_MeshFilter == null)
            {
                _MeshFilter = this.GetComponent<MeshFilter>();
                if (_MeshFilter == null)
                {
                    _MeshFilter = this.gameObject.AddComponent<MeshFilter>();
                }
            }

            return _MeshFilter;
        }
    }
    private MeshFilter _MeshFilter;

    private MeshRenderer meshRenderer
    {
        get
        {
            if (_MeshRenderer == null)
            {
                _MeshRenderer = this.GetComponent<MeshRenderer>();
                if (_MeshRenderer == null)
                {
                    _MeshRenderer = this.gameObject.AddComponent<MeshRenderer>();
                }
            }

            return _MeshRenderer;
        }
    }
    private MeshRenderer _MeshRenderer;
    private Mesh currMesh = null;

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();
    private List<Vector2> texcoords = new List<Vector2>();

    private bool IsDisplay = true;
    public LayerMask HitLayer;
    public Material Mateiral;

#if UNITY_EDITOR
    private Vector3 m_PrevLocalPos;
    Vector3 m_PrevLocalEuler;
    Vector3 m_PrevLocalScale;

    void EditorUpdate()
    {
        if (transform.localPosition != m_PrevLocalPos ||
            transform.localEulerAngles != m_PrevLocalEuler ||
            transform.localScale != m_PrevLocalScale)
        {
            GetTargetObjects();

            m_PrevLocalPos = transform.localPosition;
            m_PrevLocalEuler = transform.localEulerAngles;
            m_PrevLocalScale = transform.localScale;
        }
    }

    void OnValidate()
    {
        GetTargetObjects();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = this.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.DrawRay(-Vector3.forward/2.0f, Vector3.forward/2.0f);
    }

#endif

    private void OnEnable()
    {
        meshRenderer.sharedMaterial = Mateiral;
        Display();
        if (Application.isPlaying)
        {
            GetTargetObjects();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    // [Button]
    private void Display()
    {
        IsDisplay = !IsDisplay;
        meshRenderer.sharedMaterial = Mateiral;
        if (IsDisplay)
        {
            meshFilter.hideFlags &= ~HideFlags.HideInInspector;
            meshRenderer.hideFlags &= ~HideFlags.HideInInspector;
        }
        else
        {
            meshFilter.hideFlags |= HideFlags.HideInInspector;
            meshRenderer.hideFlags |= HideFlags.HideInInspector;
        }
    }

    // [Button("Gen Decal")]
    public void GetTargetObjects()
    {
        Debug.Log("Update");
        meshRenderer.sharedMaterial = Mateiral;
        // Physics.OverlapBox(this.transform.position, this.transform.localScale/2.0f, this.transform.rotation,HitLayer)

        MeshRenderer[] mrs = null;
#if UNITY_EDITOR
        mrs = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<MeshRenderer>();
#else
         mrs = FindObjectsOfType<MeshRenderer>();
#endif

        var id = this.meshRenderer.GetInstanceID();
        foreach (var r in mrs)
        {
            //剔除Decal自身
            if (r.GetInstanceID() == id)
                continue;

            if (IsInLayerMask(r.gameObject, HitLayer))
            {
                //遍历所有的MeshRenderer判断和自身立方体相交的Mesh进行Decal Mesh生成
                var bounds = new Bounds(this.transform.position, this.transform.localScale);
                if (bounds.Intersects(r.bounds))
                {
                    GenerateDecalMesh(r);
                }
            }
        }

        //将存储的数据生成Unity使用的Mesh
        GenerateUnityMesh();
    }

    public bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        // 根据Layer数值进行移位获得用于运算的Mask值
        int objLayerMask = 1 << obj.layer;
        return (layerMask.value & objLayerMask) > 0;
    }

    public void GenerateDecalMesh(MeshRenderer target)
    {
        var mesh = target.GetComponent<MeshFilter>().sharedMesh;
        //GC很高，可以优化
        var meshVertices = mesh.vertices;
        var meshTriangles = mesh.triangles;

        var targetToDecalMatrix = transform.worldToLocalMatrix * target.transform.localToWorldMatrix;
        for (int i = 0; i < meshTriangles.Length; i = i + 3)
        {
            var index1 = meshTriangles[i];
            var index2 = meshTriangles[i + 1];
            var index3 = meshTriangles[i + 2];

            var vertex1 = meshVertices[index1];
            var vertex2 = meshVertices[index2];
            var vertex3 = meshVertices[index3];
            //将网格的三角形转化到Decal自身立方体的坐标系中
            vertex1 = targetToDecalMatrix.MultiplyPoint(vertex1);
            vertex2 = targetToDecalMatrix.MultiplyPoint(vertex2);
            vertex3 = targetToDecalMatrix.MultiplyPoint(vertex3);

            var dir1 = vertex1 - vertex2;
            var dir2 = vertex1 - vertex3;
            var normalDir = Vector3.Cross(dir1, dir2).normalized;

            var vectorList = new List<Vector3>();
            vectorList.Add(vertex1);
            vectorList.Add(vertex2);
            vectorList.Add(vertex3);
            // if (Vector3.Angle(Vector3.forward, -normalDir) <= 90.0f)
            {
                CollisionChecker.CheckCollision(vectorList);
                if (vectorList.Count > 0)
                    AddPolygon(vectorList.ToArray(), normalDir);
            }
        }
    }

    public void AddPolygon(Vector3[] poly, Vector3 normal)
    {
        int ind1 = AddVertex(poly[0], normal);

        for (int i = 1; i < poly.Length - 1; i++)
        {
            int ind2 = AddVertex(poly[i], normal);
            int ind3 = AddVertex(poly[i + 1], normal);

            indices.Add(ind1);
            indices.Add(ind2);
            indices.Add(ind3);
        }
    }

    private int AddVertex(Vector3 vertex, Vector3 normal)
    {
        //优先寻找是否包含该顶点
        int index = FindVertex(vertex);
        if (index == -1)
        {
            vertices.Add(vertex);
            normals.Add(normal);
            //物体空间的坐标作为uv，需要从（-0.5，0.5）转化到（0，1）区间
            float u = Mathf.Lerp(0.0f, 1.0f, vertex.x + 0.5f);
            float v = Mathf.Lerp(0.0f, 1.0f, vertex.y + 0.5f);
            texcoords.Add(new Vector2(u, v));
            return vertices.Count - 1;
        }
        else
        {
            //已包含时，将该顶点的法线与新插入的顶点进行平均，共享的顶点，需要修改法线
            normals[index] = (normals[index] + normal).normalized;
            return index;
        }
    }

    private int FindVertex(Vector3 vertex)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if (Vector3.Distance(vertices[i], vertex) < 0.01f) return i;
        }

        return -1;
    }

    public void HandleZFighting(float distance)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] += normals[i] * distance;
        }
    }

    public void GenerateUnityMesh()
    {
        currMesh = new Mesh();
        HandleZFighting(0.001f);

        currMesh.Clear(true);

        currMesh.vertices = vertices.ToArray();
        currMesh.normals = normals.ToArray();
        currMesh.triangles = indices.ToArray();
        currMesh.uv = texcoords.ToArray();

        vertices.Clear();
        normals.Clear();
        indices.Clear();
        texcoords.Clear();

        meshFilter.sharedMesh = currMesh;
    }
}

public class CollisionChecker
{
    private static List<Plane> planList = new List<Plane>();

    static CollisionChecker()
    {
        //front
        planList.Add(new Plane(Vector3.forward, 0.5f));
        //back
        planList.Add(new Plane(Vector3.back, 0.5f));
        //up
        planList.Add(new Plane(Vector3.up, 0.5f));
        //down
        planList.Add(new Plane(Vector3.down, 0.5f));
        //left
        planList.Add(new Plane(Vector3.left, 0.5f));
        //right
        planList.Add(new Plane(Vector3.right, 0.5f));
    }

    private static void CheckCollision(Plane plane, List<Vector3> vectorList)
    {
        var newList = new List<Vector3>();
        for (var current = 0; current < vectorList.Count; current++)
        {
            var next = (current + 1) % vectorList.Count;
            var v1 = vectorList[current];
            var v2 = vectorList[next];
            var currentPointIn = plane.GetSide(v1);
            if (currentPointIn == true)
                newList.Add(v1);

            if (plane.GetSide(v2) != currentPointIn)
            {
                float distance;
                var ray = new Ray(v1, v2 - v1);
                plane.Raycast(ray, out distance);
                var newPoint = ray.GetPoint(distance);
                newList.Add(newPoint);
            }
        }

        vectorList.Clear();
        vectorList.AddRange(newList);
    }

    public static void CheckCollision(List<Vector3> vectorList)
    {
        foreach (var curPlane in planList)
        {
            CheckCollision(curPlane, vectorList);
        }
    }
}
