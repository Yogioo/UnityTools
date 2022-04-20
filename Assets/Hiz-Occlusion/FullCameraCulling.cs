using System;
using System.Collections.Generic;
using System.Linq;
using Sunset.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[RequireComponent(typeof(HZB))]
public class FullCameraCulling : MonoBehaviour
{
    public bool ActiveOcclusionCulling= true;
    public bool ActiveFrustumCulling = true;
    
    public Mesh Mesh;
    public Material Material;
    public Material ShadowOnlyMaterial;
    
    public static FullCameraCulling Instance;
    private HZB m_Hzb;
    private new Camera camera;
    private List<CellData> DrawData;
    public ComputeShader cs;
    public LayerMask CullingMask = new LayerMask() { value = int.MaxValue };

    private Dictionary<Tuple<Mesh, Material>, List<CullingRenderer>> CullingRenders =
        new Dictionary<Tuple<Mesh, Material>, List<CullingRenderer>>();

    private FullCullBuffers m_FullCullBuffers;
    private FullCullBuffers m_FullCullShadowBuffers;

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
        m_FullCullBuffers.UpdateBuffer(DrawData);
        m_FullCullShadowBuffers.UpdateBuffer(DrawData);
    }

    private void Awake()
    {
        Instance = this;
        camera = this.GetComponent<Camera>();
        m_Hzb = this.GetComponent<HZB>();

        DrawData = new List<CellData>();
        m_FullCullBuffers = new FullCullBuffers(Mesh,Material);
        m_FullCullShadowBuffers = new FullCullBuffers(Mesh,ShadowOnlyMaterial);

        InitSceneData();
        UpdateDrawData();
    }

    private void InitSceneData()
    {
        var renderTargets = FindObjectsOfType<Renderer>().ToList();
        for (var i = 0; i < renderTargets.Count; i++)
        {
            var renderTarget = renderTargets[i];
            var result = CullingMask.value & (1 << renderTarget.gameObject.layer);
            if (result != 0)
            {
                renderTarget.gameObject.AddComponent<CullingRenderer>();
            }
        }
    }

    void OnDestroy()
    {
        m_FullCullBuffers.Dispose();
        m_FullCullShadowBuffers.Dispose();
    }

    private void Cull()
    {
        
        
        
        var v = camera.worldToCameraMatrix;
        var p = camera.projectionMatrix;
        Matrix4x4 m_VP = p * v;
        Vector4[] cameraPanels = CheckExtent.GetFrustumPlane(camera);
        // var panels = GeometryUtility.CalculateFrustumPlanes(camera);
        
        m_FullCullBuffers.Cull(cs,ActiveFrustumCulling,ActiveOcclusionCulling,
            m_Hzb.Texture,m_Hzb.TextureSize,m_VP, camera.transform.position,cameraPanels);

        
        // var shadowView = Shader.GetGlobalMatrix("unity_WorldToLight");
        var shadowProjection = Shader.GetGlobalMatrix("unity_WorldToShadow");
        var shadowVP = shadowProjection;// * shadowView;
        var panels2 = GeometryUtility.CalculateFrustumPlanes(shadowVP);
        // var shadowCam = FullCullingMainLight.Instance.m_ShadowCam;
        // var shadowV = shadowCam.worldToCameraMatrix;
        // var shadowP = shadowCam.projectionMatrix;
        // var shadowVP = shadowP * shadowV;
        var shadowCameraPanels = new Vector4[6];// CheckExtent.GetFrustumPlane(shadowCam);
        // var panels2 = GeometryUtility.CalculateFrustumPlanes(shadowCam);
        
        for (var i = 0; i < shadowCameraPanels.Length; i++)
        {
            Plane plane = panels2[i];
            var normal = plane.normal;
            shadowCameraPanels[i] = new Vector4(-normal.x,-normal.y,-normal.z, -plane.distance);
        }
        m_FullCullShadowBuffers.Cull(cs,ActiveFrustumCulling,false,
            m_Hzb.Texture,m_Hzb.TextureSize,shadowVP, camera.transform.position,shadowCameraPanels);
    }

    private void Update()
    {
        Cull();
        Draw();
    }

    private void Draw()
    {
        m_FullCullBuffers.Draw(this.transform.position, ShadowCastingMode.Off, true);
        
        m_FullCullShadowBuffers.Draw(this.transform.position, ShadowCastingMode.On, false);
    }
}