using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class DrawMeshTest : MonoBehaviour
{
    // public Mesh instanceMesh;
    // public Material instanceMaterial;
    // public int subMeshIndex = 0;

    // private int cachedInstanceCount = -1;
    // private int cachedSubMeshIndex = -1;
    // private ComputeBuffer localToWorldMatrixBuffer;
    // private ComputeBuffer argsBuffer;
    // private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public GameObject TestGO;

    private Dictionary<int, RendererCall> rendererData = new Dictionary<int, RendererCall>();

    class RendererCall
    {
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public int subMeshIndex = 0;
        public List<Renderer> renders;
        public ComputeBuffer localToWorldMatrixBuffer;
        public ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        public bool isNeedUpdateBuffer = false;

        private List<Matrix4x4> local2World = new List<Matrix4x4>();

        public RendererCall(Renderer render)
        {
            this.renders = new List<Renderer>() { render };
            this.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.instanceMaterial = render.material;
            this.instanceMesh = render.GetComponent<MeshFilter>().mesh;
            this.isNeedUpdateBuffer = true;
        }

        public void Add(Renderer render)
        {
            this.renders.Add(render);
            isNeedUpdateBuffer = true;
        }

        public void UpdateBuffers()
        {
            // if (!isNeedUpdateBuffer)
            //     return;
            // isNeedUpdateBuffer = false;

            // Ensure submesh index is in range
            if (instanceMesh != null)
                subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

            // Positions
            if (localToWorldMatrixBuffer != null)
                localToWorldMatrixBuffer.SetCounterValue(0);
                // localToWorldMatrixBuffer.Release();
            else
                localToWorldMatrixBuffer = new ComputeBuffer(renders.Count, sizeof(float) * 4 * 4);
            // localToWorldMatrixBuffer.SetCounterValue(0);
            for (int i = 0; i < renders.Count; i++)
            {
                if (local2World.Count <= i)
                {
                    local2World.Add(this.renders[i].transform.localToWorldMatrix);
                }
                else
                {
                    local2World[i] = this.renders[i].transform.localToWorldMatrix;
                }
            }

            localToWorldMatrixBuffer.SetData(local2World);
            instanceMaterial.SetBuffer("localToWorldBuffer", localToWorldMatrixBuffer);

            // Indirect args
            if (instanceMesh != null)
            {
                args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
                args[1] = (uint)renders.Count;
                args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
                args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }

            argsBuffer.SetData(args);
        }

        public void Release()
        {
            if (localToWorldMatrixBuffer != null)
            {
                localToWorldMatrixBuffer.Release();
            }

            localToWorldMatrixBuffer = null;
            if (argsBuffer != null)
            {
                argsBuffer.Release();
            }

            argsBuffer = null;
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < 1000; i++)
        {
            GameObject.Instantiate(TestGO, Random.insideUnitSphere * 100, Quaternion.identity);
        }


        rendererData.Clear();
        Renderer[] renderObj = GameObject.FindObjectsOfType<Renderer>();
        for (var i = 0; i < renderObj.Length; i++)
        {
            var targetObj = renderObj[i];
            var id = targetObj.GetComponent<MeshFilter>().sharedMesh.GetInstanceID();
            if (rendererData.TryGetValue(id, out var rendererCall))
            {
                rendererCall.Add(targetObj);
            }
            else
            {
                rendererData.Add(id, new RendererCall(targetObj));
            }
        }
    }

    void Start()
    {
        UpdateBuffers();
    }

    void Update()
    {
        // Update starting position buffer
        // if (cachedSubMeshIndex != subMeshIndex)
        // UpdateBuffers();

        // Render
        foreach (var call in this.rendererData)
        {
            var data = call.Value;
            Graphics.DrawMeshInstancedIndirect(data.instanceMesh, data.subMeshIndex, data.instanceMaterial,
                new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), data.argsBuffer);
        }
    }

    void UpdateBuffers()
    {
        foreach (var call in this.rendererData)
        {
            call.Value.UpdateBuffers();
        }
    }

    void OnDisable()
    {
        foreach (var call in this.rendererData)
        {
            call.Value.Release();
        }
    }
}