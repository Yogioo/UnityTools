using System;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Renderer))]
public class CullingRenderer : MonoBehaviour
{
    private Renderer m_Renderer;
    public Tuple<Mesh, Material> MyKey { get; private set; }
    public Bounds GetBounds => this.m_Renderer.bounds;
    public Matrix4x4 GetLocalToWorld => this.m_Renderer.localToWorldMatrix;

    public bool Test;
    public int Index { get; set; }

    private bool IsInit = false;
    private Vector3 lastPos;
    private Vector3 lastRot;
    private Vector3 lastScale;


    private void OnValidate()
    {
        if (Application.isPlaying && IsInit)
        {
            FullCameraCulling.Instance.Modify(this);
        }
    }

    private void Awake()
    {
        m_Renderer = this.GetComponent<MeshRenderer>();
        var mesh = this.GetComponent<MeshFilter>().sharedMesh;
        var mat = this.m_Renderer.sharedMaterial;
        this.m_Renderer.sharedMaterial.enableInstancing = true;
        MyKey = new Tuple<Mesh, Material>(mesh, mat);
        m_Renderer = this.GetComponent<Renderer>();
        
        IsInit = true;
    }

    private void FixedUpdate()
    {
        Profiler.BeginSample("CullingFixed");
        if (lastPos != this.transform.position ||
            lastRot != this.transform.eulerAngles ||
            lastScale != this.transform.lossyScale)
        {
            lastPos = this.transform.position;
            lastRot = this.transform.eulerAngles;
            lastScale = this.transform.lossyScale;
            
            Profiler.BeginSample("Culling Modify");
            FullCameraCulling.Instance.Modify(this);
            Profiler.EndSample();
        }
        Profiler.EndSample();
    }

    void OnEnable()
    {

        FullCameraCulling.Instance.Add(this);
        // if (this.GetBounds.extents.magnitude > 20)
        // {
        //     this.enabled = false;
        //     return;
        // }
        m_Renderer.enabled = false;

    }

    private void OnDisable()
    {
        FullCameraCulling.Instance.Remove(this);
        IsInit = false;
        m_Renderer.enabled = true;

    }

    private void OnDestroy()
    {
    }
}