using System;
using UnityEngine;
using System.Collections.Generic;
using Sunset.SceneManagement;
using Random = UnityEngine.Random;

[RequireComponent(typeof(DepthTextureGenerator))]
public class DrawMeshTest : MonoBehaviour
{
    public GameObject TestGO;
    public int DrawCount = 100;
    public int Size = 100;
    public ComputeShader CullCompute;

    private Dictionary<int, RendererCall> rendererData = new Dictionary<int, RendererCall>();
    private static DepthTextureGenerator depthGenerator;

    private static bool isOpenGL;
    private static Matrix4x4 vpMatrix;
    private static Camera cam;
    private static Vector4[] cameraPanels;

    struct CullData
    {
        public Vector3 center;
        public Vector3 extents;
        public Matrix4x4 local2World;
    }

    class RendererCall
    {
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public int subMeshIndex = 0;
        public List<Renderer> renders;
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

            UpdateArgsBuffer();
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
                cullDataBuffer = new ComputeBuffer(this.renders.Count, sizeof(float) * 3 * 2 + sizeof(float) * 4 * 4);
            }

            CullData[] data = new CullData[this.renders.Count];
            for (var i = 0; i < this.renders.Count; i++)
            {
                data[i] = new CullData()
                {
                    center = this.renders[i].bounds.center,
                    extents = this.renders[i].bounds.extents,
                    local2World = this.renders[i].localToWorldMatrix,
                };
            }

            cullDataBuffer.SetData(data);
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
            cs.SetInt("Count", this.renders.Count);

            cs.SetBool("isOpenGL", isOpenGL);
            cs.SetMatrix("vpMatrix", vpMatrix);
            cs.SetVectorArray("cameraPanels", cameraPanels);
            cs.SetInt("depthTextureSize", depthGenerator.depthTextureSize);
            
            cs.SetTexture(k,"hizTexture", depthGenerator.depthTexture);
            cs.SetBuffer(k, "LocalToWorldCulled", localToWorldMatrixBufferCulled);
            cs.SetBuffer(k, "CullDataBuffer", cullDataBuffer);

            int count = this.renders.Count / 8;
            if (count < 1)
            {
                count = 1;
            }

            cs.Dispatch(k, count, 1, 1);
            ComputeBuffer.CopyCount(localToWorldMatrixBufferCulled, argsBuffer, sizeof(uint) * 1);
            instanceMaterial.SetBuffer("localToWorldBuffer", localToWorldMatrixBufferCulled);

            // Matrix4x4[] data = new Matrix4x4[this.renders.Count];
            // localToWorldMatrixBufferCulled.GetData(data);
        }

        public void Release()
        {
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
        cam = this.GetComponent<Camera>();
        isOpenGL = cam.projectionMatrix.Equals(GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false));

        depthGenerator = GetComponent<DepthTextureGenerator>();

        for (int i = 0; i < DrawCount; i++)
        {
            GameObject.Instantiate(TestGO, Random.insideUnitSphere * Size, Quaternion.identity);
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

#if UNITY_EDITOR
    // Debug
    public Rect DepthTexRect = new Rect(0, 0, 1920 / 2.0f, 1080 / 2.0f);
    public bool isDisplayDepth = true;

    private void OnGUI()
    {
        if (isDisplayDepth)
        {
            GUI.DrawTexture(DepthTexRect, depthGenerator.depthTexture);
        }
    }

    public bool isDisplayGizmos = true;

    private void OnDrawGizmos()
    {
        if (isDisplayGizmos)
        {
            foreach (var kv in this.rendererData)
            {
                foreach (var r in kv.Value.renders)
                {
                    Gizmos.matrix = r.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                }
            }
        }
    }
#endif

    void Update()
    {
        vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix;
        cameraPanels = CheckExtent.GetFrustumPlane(cam);
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