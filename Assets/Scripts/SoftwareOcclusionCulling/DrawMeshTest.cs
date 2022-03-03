using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class DrawMeshTest : MonoBehaviour
{
    public GameObject TestGO;
    public ComputeShader CullCompute;
    private Dictionary<int, RendererCall> rendererData = new Dictionary<int, RendererCall>();

    struct CullData
    {
        public Vector3 center;
        public Vector3 extent;
    }

    class RendererCall
    {
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public int subMeshIndex = 0;
        public List<Renderer> renders;
        public ComputeBuffer localToWorldMatrixBuffer;
        public ComputeBuffer localToWorldMatrixBufferCulled;
        public ComputeBuffer cullDataBuffer;
        public ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        public bool isNeedUpdateBuffer = false;

        private List<Matrix4x4> local2World = new List<Matrix4x4>();
        private CullData[] cullData;
        
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
            if (!isNeedUpdateBuffer)
                return;
            isNeedUpdateBuffer = false;

            // Ensure submesh index is in range
            if (instanceMesh != null)
                subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

            UpdateMatrixBuffer();

            UpdateArgsBuffer();
        }

        private void UpdateMatrixBuffer()
        {
            // Positions
            if (localToWorldMatrixBuffer != null)
                localToWorldMatrixBuffer.SetCounterValue(0);
            // localToWorldMatrixBuffer.Release();
            else
                localToWorldMatrixBuffer = new ComputeBuffer(renders.Count, sizeof(float) * 4 * 4);


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
        }

        private void UpdateArgsBuffer()
        {
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

        private void UpdateCullDataBuffer()
        {
            if (cullDataBuffer != null)
            {
                cullDataBuffer.SetCounterValue(0);
            }
            else
            {
                cullDataBuffer = new ComputeBuffer(this.renders.Count, sizeof(float) * 3 * 2);
            }

            CullData[] data = new CullData[this.renders.Count];
            for (var i = 0; i < this.renders.Count; i++)
            {
                data[i] = new CullData()
                {
                    center = this.renders[i].bounds.center,
                    extent = this.renders[i].bounds.extents
                };
            }
        }

        public void Cull(ComputeShader cs)
        {
            if (localToWorldMatrixBufferCulled != null)
            {
                localToWorldMatrixBufferCulled.SetCounterValue(0);
            }
            else
            {
                localToWorldMatrixBufferCulled =
                    new ComputeBuffer(this.renders.Count, sizeof(float) * 4 * 4, ComputeBufferType.Append);
            }

            UpdateCullDataBuffer();
            // TODO: 视椎剔除
            // TODO: 遮挡剔除

            var k = cs.FindKernel("CSMain");
            cs.SetBuffer(k, "LocalToWorld", localToWorldMatrixBuffer);
            cs.SetBuffer(k, "args", argsBuffer);
            cs.SetInt("Count", this.renders.Count);

            cs.SetBuffer(k, "LocalToWorldCulled", localToWorldMatrixBufferCulled);
            cs.SetBuffer(k,"CullDataBuffer", cullDataBuffer);

            cs.Dispatch(k, this.renders.Count / 8, 1, 1);
            // Matrix4x4[] data = new Matrix4x4[this.renders.Count];
            // localToWorldMatrixBufferCulled.GetData(data);
        }

        public void Release()
        {
            localToWorldMatrixBuffer?.Release();
            localToWorldMatrixBuffer = null;
            argsBuffer?.Release();
            argsBuffer = null;
            localToWorldMatrixBufferCulled?.Release();
            localToWorldMatrixBufferCulled = null;
            cullDataBuffer?.Release();
            cullDataBuffer = null;
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
        UpdateBuffers();
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


            call.Value.Cull(CullCompute);
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