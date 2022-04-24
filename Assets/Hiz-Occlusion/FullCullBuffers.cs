using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 剔除需要的数据包
/// </summary>
public class FullCullBuffers
{
    /// <summary>
    /// 本地到世界的坐标矩阵集合
    /// </summary>
    private ComputeBuffer LocalToWorldMatrixBufferCulled;
    /// <summary>
    /// 需要剔除的数据集合
    /// </summary>
    private ComputeBuffer DrawDataBuffer;
    /// <summary>
    /// Draw数据
    /// </summary>
    private ComputeBuffer ArgsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private MaterialPropertyBlock materialBlock;
    private int DrawCount;

    private Mesh m_Mesh;
    private Material m_Material;
    private Bounds m_DrawBounds = new Bounds(Vector3.zero, Vector3.one * 100);

    public FullCullBuffers(Mesh p_Mesh, Material p_Mat)
    {
        Init(p_Mesh, p_Mat);
    }
    private void Init(Mesh p_Mesh, Material p_Mat)
    {
        InitDrawData(p_Mesh, p_Mat);
        InitBuffer();
        InitArgsBuffer();
    }

    private void InitBuffer()
    {
        if (this.ArgsBuffer != null || this.DrawDataBuffer != null)
        {
            Dispose();
        }

        this.ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void InitDrawData(Mesh p_Mesh, Material p_Mat)
    {
        m_Mesh = p_Mesh;
        m_Material = p_Mat;
    }
    /// <summary>
    /// 绘制总量发生改变时调用 更新总量
    /// </summary>
    private void InitArgsBuffer()
    {
        materialBlock = new MaterialPropertyBlock();
        int subMeshIndex = 0;
        // Indirect args
        if (m_Mesh != null)
        {
            args[0] = (uint)m_Mesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)DrawCount;
            args[2] = (uint)m_Mesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)m_Mesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        ArgsBuffer.SetData(args);
    }

    /// <summary>
    /// 刷新剔除数据 (每当数据总量发生修改时调用)
    /// </summary>
    /// <param name="p_DrawData"></param>
    public void UpdateBuffer(List<FullCameraCulling.CellData> p_DrawData)
    {
        DrawCount = p_DrawData.Count;
        if (DrawDataBuffer != null)
        {
            DrawDataBuffer.Release();
        }

        DrawDataBuffer = new ComputeBuffer(p_DrawData.Count, Marshal.SizeOf(typeof(FullCameraCulling.CellData)));
        DrawDataBuffer.SetData(p_DrawData.ToArray());
    }


    public void Cull(ComputeShader cs, bool p_ActiveFrustumCulling, bool p_ActiveOcclusionCulling,
        RenderTexture hzRT, Vector4 hzRTSize, Matrix4x4 p_VP, Vector3 p_CamPos,
        Vector4[] p_CameraPanels
    )
    {
        if (LocalToWorldMatrixBufferCulled != null)
        {
            LocalToWorldMatrixBufferCulled.SetCounterValue(0);
        }
        else
        {
            LocalToWorldMatrixBufferCulled =
                new ComputeBuffer(DrawCount, Marshal.SizeOf<Matrix4x4>(), ComputeBufferType.Append);
        }

        var k = cs.FindKernel("CSMain");
        cs.SetBuffer(k, "LocalToWorldCulled", LocalToWorldMatrixBufferCulled);

        cs.SetTexture(k, "_HiZMap", hzRT);
        cs.SetVector("_HiZTextureSize", hzRTSize);

        cs.SetMatrix("_UNITY_MATRIX_VP", p_VP);
        cs.SetVector("_CamPosition", p_CamPos);

        cs.SetBuffer(k, "bounds", DrawDataBuffer);

        cs.SetBool("_ActiveFrustumCulling", p_ActiveFrustumCulling);
        cs.SetBool("_ActiveOcclusionCulling", p_ActiveOcclusionCulling);

        cs.SetVectorArray("_CameraPanels", p_CameraPanels);

        cs.SetInt("_Count", DrawCount);
        int threadSizeX = 64;
        int dispatchXLength = DrawCount / threadSizeX + (DrawCount % (int)threadSizeX > 0 ? 1 : 0);

        cs.Dispatch(k, (int)dispatchXLength, 1, 1);

        ComputeBuffer.CopyCount(LocalToWorldMatrixBufferCulled, ArgsBuffer, sizeof(uint) * 1);
        this.materialBlock.SetBuffer("localToWorldBuffer", LocalToWorldMatrixBufferCulled);
    }

    public void Draw(Vector3 drawCenter, ShadowCastingMode p_ShadowCastingMode, bool isReceiveShadow)
    {
        m_DrawBounds.center = drawCenter;
        Graphics.DrawMeshInstancedIndirect(m_Mesh, 0, m_Material, m_DrawBounds, ArgsBuffer, 0,
            materialBlock, p_ShadowCastingMode, isReceiveShadow);
    }

    public void Dispose()
    {
        LocalToWorldMatrixBufferCulled.Dispose();
        DrawDataBuffer.Dispose();
        ArgsBuffer.Dispose();
    }
}