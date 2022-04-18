using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;

[RequireComponent(typeof(HZB))]
public class FullCameraCulling : MonoBehaviour
{
    public static FullCameraCulling Instance;
    private HZB m_Hzb;
    private new Camera camera;

    private List<CellData> DrawData;

    public ComputeShader cs;

    public ComputeBuffer localToWorldMatrixBufferCulled, DrawDataBuffer;

    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    public MaterialPropertyBlock materialBlock;


    public Mesh MeshTest;
    public Material Mat;
    Bounds drawBounds = new Bounds(Vector3.zero, Vector3.one * 10000);

    private Dictionary<Tuple<Mesh, Material>, List<CullingRenderer>> CullingRenders =
        new Dictionary<Tuple<Mesh, Material>, List<CullingRenderer>>();

    public struct CellData
    {
        public Vector3 center;
        public Vector3 extent;
        public Matrix4x4 localToWorld;

        public CellData(Vector3 center, Vector3 extent, Matrix4x4 localToWorld)
        {
            this.center = center;
            this.extent = extent;
            this.localToWorld = localToWorld;
        }
    }

    public void Add(CullingRenderer renderer)
    {
        Profiler.BeginSample("Total");

        
        Profiler.BeginSample("AddData");

        if (this.CullingRenders.ContainsKey(renderer.MyKey))
        {
            this.CullingRenders[renderer.MyKey].Add(renderer);
        }
        else
        {
            this.CullingRenders.Add(renderer.MyKey, new List<CullingRenderer>() { renderer });
        }

        Bounds bounds = renderer.GetBounds;
        CellData cellData = new CellData(bounds.center, bounds.extents, renderer.GetLocalToWorld);
        DrawData.Add(cellData);

        renderer.Index = DrawData.IndexOf(cellData);
        Profiler.BeginSample("UpdateCell");
        Profiler.EndSample();
        Profiler.EndSample();
    }

    public void Modify(CullingRenderer renderer)
    {
        Bounds bounds = renderer.GetBounds;
        DrawData[renderer.Index] = new CellData(bounds.center, bounds.extents, renderer.GetLocalToWorld);
        UpdateDrawData();
    }

    public void Remove(CullingRenderer renderer)
    {
        if (this.CullingRenders.ContainsKey(renderer.MyKey))
        {
            var array = this.CullingRenders[renderer.MyKey];
            var myIndex = array.IndexOf(renderer);
            DrawData.RemoveAt(renderer.Index);
            array.Remove(renderer);
            for (var i = myIndex; i < array.Count; i++)
            {
                array[i].Index--;
            }
        }
        UpdateDrawData();
    }

    private void UpdateDrawData()
    {
        if (DrawDataBuffer != null)
        {
            DrawDataBuffer.Release();
        }
        DrawDataBuffer = new ComputeBuffer(DrawData.Count, Marshal.SizeOf(typeof(CellData)));
        DrawDataBuffer.SetData(DrawData.ToArray());
    }

    private void Awake()
    {
        Instance = this;
        camera = this.GetComponent<Camera>();
        m_Hzb = this.GetComponent<HZB>();


        DrawData = new List<CellData>();
        var renderTargets = FindObjectsOfType<Renderer>().ToList();
        for (var i = 0; i < renderTargets.Count; i++)
        {
            var renderTarget = renderTargets[i];
            // renderTarget.enabled = false;
            //
            // var bounds = renderTarget.bounds;
            // DrawData.Add(new CellData(bounds.center, bounds.extents,
            // renderTarget.localToWorldMatrix));
            renderTarget.gameObject.AddComponent<CullingRenderer>();
        }

        // InitSceneData();

        UpdateDrawData();

        InitArgsBuffer();
        UpdateArgsBuffer();
    }

    private void InitSceneData()
    {
        var renderTargets = FindObjectsOfType<Renderer>().ToList();

        Profiler.BeginSample("Test");
        for (var i = 0; i < renderTargets.Count; i++)
        {
            var renderTarget = renderTargets[i];
            if (renderTarget.GetComponent<CullingRenderer>() == null)
            {
                renderTarget.gameObject.AddComponent<CullingRenderer>();
            }
        }
        Profiler.EndSample();

    }

    void OnDestroy()
    {
        localToWorldMatrixBufferCulled.Dispose();
        DrawDataBuffer.Dispose();
    }


    private void InitArgsBuffer()
    {
        this.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void UpdateArgsBuffer()
    {
        materialBlock = new MaterialPropertyBlock();
        int subMeshIndex = 0;
        // Indirect args
        if (MeshTest != null)
        {
            args[0] = (uint)MeshTest.GetIndexCount(subMeshIndex);
            args[1] = (uint)DrawData.Count;
            args[2] = (uint)MeshTest.GetIndexStart(subMeshIndex);
            args[3] = (uint)MeshTest.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        argsBuffer.SetData(args);
    }

    private void Cull()
    {
        if (localToWorldMatrixBufferCulled != null)
        {
            localToWorldMatrixBufferCulled.SetCounterValue(0);
        }
        else
        {
            localToWorldMatrixBufferCulled =
                new ComputeBuffer(this.DrawData.Count, Marshal.SizeOf<Matrix4x4>(), ComputeBufferType.Append);
        }

        var k = cs.FindKernel("CSMain");
        cs.SetBuffer(k, "LocalToWorldCulled", localToWorldMatrixBufferCulled);

        cs.SetTexture(k, "_HiZMap", m_Hzb.Texture);
        cs.SetVector("_HiZTextureSize", m_Hzb.TextureSize);

        Matrix4x4 v = camera.worldToCameraMatrix;
        Matrix4x4 p = camera.projectionMatrix;
        Matrix4x4 m_MVP = p * v;
        cs.SetMatrix("_UNITY_MATRIX_MVP", m_MVP);
        Vector3 m_camPosition = camera.transform.position;
        cs.SetVector("_CamPosition", m_camPosition);

        cs.SetBuffer(k, "bounds", DrawDataBuffer);

        int length = DrawData.Count;
        cs.SetInt("_Count",length);
        int threadSizeX = 64;
        int dispatchXLength = length / threadSizeX + (length % (int)threadSizeX > 0 ? 1 : 0);
        
        cs.Dispatch(k, (int)dispatchXLength, 1, 1);

        ComputeBuffer.CopyCount(localToWorldMatrixBufferCulled, argsBuffer, sizeof(uint) * 1);
        this.materialBlock.SetBuffer("localToWorldBuffer", localToWorldMatrixBufferCulled);
    }

    private void Update()
    {
        Cull();
        Draw();
        DrawShadow();
    }

    private void Draw()
    {
        Graphics.DrawMeshInstancedIndirect(MeshTest, 0, Mat, drawBounds, argsBuffer, 0, materialBlock,
            ShadowCastingMode.Off, true);
    }

    private void DrawShadow()
    {
        Graphics.DrawMeshInstancedIndirect(MeshTest, 0, Mat, drawBounds, argsBuffer, 0, materialBlock,
            ShadowCastingMode.On, false);
    }
}