using System;
using System.Runtime.InteropServices;
using UnityEngine;

public enum CullingMode
{
    None,
    FrustumCullingOnly,
    FrustumAndOcclusionCulling

}

[RequireComponent(typeof(Camera),typeof(HZB))]
public class CameraCulling : MonoBehaviour
{
    private Camera camera;
    public ComputeShader frustumCullingComputeShader;
    public ComputeShader occlusionCullingComputeShader;
    
    private int frustumKernel;
    private int occlusionKernel;
    private uint threadSizeX;
    [HideInInspector]
    public Renderer[] staticRenderers;
    [HideInInspector]
    public Bounds[] staticRendererBounds;

    public HZB hiZBuffer; 
    public ComputeBuffer boundBuffer;
    

    [Header("Culling Option")]
    public CullingMode cullingMode;

    // 为什么要保证resultBuffer不被序列化呢？

    [NonSerialized]
    public int[] cullingResults;
    public ComputeBuffer resultBuffer;

    [NonSerialized]
    public Plane[] frustumPlanes;
    public ComputeBuffer planeBuffer;


    void Awake()
    {
        camera = GetComponent<Camera>();
        staticRenderers = Array.FindAll(FindObjectsOfType<Renderer>(), (renderer) => renderer.gameObject.isStatic);
        // Array.ConvertAll(TInput, TOutput) 
        // 将一种类型的数组转换为另一种类型的数组
        // 获取目标渲染物体的轴对齐包围盒
        staticRendererBounds = Array.ConvertAll(staticRenderers, renderer => renderer.bounds);

        Init();

    }

    void Update()
    {
        
        if (cullingMode == CullingMode.None)
        {
            foreach (Renderer i in staticRenderers)
            {
                i.enabled = true;
            }
        }
            
        if (cullingMode == CullingMode.FrustumCullingOnly)
            FrustumCull();
        if (cullingMode == CullingMode.FrustumAndOcclusionCulling)
            FullCull();
    }

    void OnDestroy() 
    {
        resultBuffer.Dispose();
        planeBuffer.Dispose();
        boundBuffer.Dispose();
    }

    private void Init()
    {
        // 使用FindKernel函数，用名字找到ComputeShader中定义的一个运算unit
        frustumKernel = frustumCullingComputeShader.FindKernel("CSMain");

        uint threadSizeY;
        uint threadSizeZ;

        // 获取GPU线程的X/Y/Z
        frustumCullingComputeShader.GetKernelThreadGroupSizes(frustumKernel, out threadSizeX, out threadSizeY, out threadSizeZ);

        // 构造视锥平面
        frustumPlanes = new Plane[6];
        // 用Marshal.SizeOf()获取一个对象所占内存的大小
        planeBuffer = new ComputeBuffer(6, Marshal.SizeOf(typeof(Plane)));
        // 用数组内容初始化ComputeBuffer
        planeBuffer.SetData(frustumPlanes);

        boundBuffer = new ComputeBuffer(staticRenderers.Length, Marshal.SizeOf(typeof(Bounds)));
        boundBuffer.SetData(staticRendererBounds);

        if (cullingResults == null)
            cullingResults = new int[staticRenderers.Length];

        resultBuffer = new ComputeBuffer(staticRenderers.Length, Marshal.SizeOf(typeof(uint)));
        resultBuffer.SetData(cullingResults);

        // 把脚本的输入和ComputeShader的输入关联在一起
        // 输入的长度是所有渲染体的数量
        frustumCullingComputeShader.SetInt("resultLength", staticRenderers.Length);

        // 将ComputeBuffer关联到外部，给其它阶段的shader提供数据
        frustumCullingComputeShader.SetBuffer(frustumKernel, "bounds", boundBuffer);
        frustumCullingComputeShader.SetBuffer(frustumKernel, "planes", planeBuffer);
        frustumCullingComputeShader.SetBuffer(frustumKernel, "results", resultBuffer);
        
        // Occlusion Culling
        occlusionKernel = occlusionCullingComputeShader.FindKernel("CSMain");

        occlusionCullingComputeShader.SetBuffer(occlusionKernel, "bounds", boundBuffer);
        occlusionCullingComputeShader.SetBuffer(occlusionKernel, "results", resultBuffer);

        resultBuffer.SetData(cullingResults);
        

    }


    public void FrustumCull()
    {
        uint length = (uint)staticRenderers.Length;
        uint dispatchXLength = length / threadSizeX + (uint)((int)length % (int)threadSizeX > 0 ? 1 : 0);

        GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
        planeBuffer.SetData(frustumPlanes);

        // 启动ComputeShader的运算unit
        frustumCullingComputeShader.Dispatch(frustumKernel, (int)dispatchXLength, 1, 1);
        resultBuffer.GetData(cullingResults);

        // 根据resultBuffer的内容，确定是否渲染
        for (int i = 0; i < staticRenderers.Length; i++)
            staticRenderers[i].enabled = cullingResults[i] != 0;
    }


    public void FullCull()
    {
        uint length = (uint)staticRenderers.Length;
        uint dispatchXLength = length / threadSizeX + (uint)(((int)length % (int)threadSizeX > 0) ? 1 : 0);

        Matrix4x4 v = camera.worldToCameraMatrix;
        Matrix4x4 p = camera.projectionMatrix;
        Matrix4x4 m_MVP = p * v;
        Vector3 m_camPosition = camera.transform.position;

        occlusionCullingComputeShader.SetMatrix("_UNITY_MATRIX_MVP", m_MVP);
        occlusionCullingComputeShader.SetVector("_CamPosition", m_camPosition);
        occlusionCullingComputeShader.SetVector("_HiZTextureSize", hiZBuffer.TextureSize);
        occlusionCullingComputeShader.SetTexture(occlusionKernel, "_HiZMap", hiZBuffer.Texture);
        
        occlusionCullingComputeShader.Dispatch(occlusionKernel, (int)dispatchXLength, 1, 1);
        resultBuffer.GetData(cullingResults);

        // 根据resultBuffer的内容，确定是否渲染
        for (int i = 0; i < staticRenderers.Length; i++)
            staticRenderers[i].enabled = cullingResults[i] != 0;
    }



}
