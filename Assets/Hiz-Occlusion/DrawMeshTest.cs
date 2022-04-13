using UnityEngine;
using System.Collections.Generic;
using Sunset.SceneManagement;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

[RequireComponent(typeof(DepthTextureGenerator))]
public class DrawMeshTest : MonoBehaviour
{
    public bool IsSpawnTestGO = true;
    public bool IsDraw = false;
    public GameObject TestGO;
    public int DrawCount = 100;
    public int Size = 100;
    public ComputeShader CullCompute;

    public Dictionary<int, RendererCall> rendererData = new Dictionary<int, RendererCall>();
    private static DepthTextureGenerator depthGenerator;

    private static bool isOpenGL;
    private static Matrix4x4 vpMatrix;
    private static Camera cam;
    private static Vector4[] cameraPanels;

    public Camera DisplayCam;

    struct CullData
    {
        public Vector3 center;
        public Vector3 extents;
        public Matrix4x4 local2World;
    }

    [System.Serializable]
    public class RendererCall
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
        public MaterialPropertyBlock materialBlock;

        private List<CullData> cullData;

        public RendererCall(Renderer render)
        {
            this.renders = new List<Renderer>() { render };
            this.argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            this.instanceMaterial = render.sharedMaterial;
            render.sharedMaterial.enableInstancing = true;
            this.instanceMesh = render.GetComponent<MeshFilter>().sharedMesh;
            this.isNeedUpdateBuffer = true;
            render.enabled = false;
            materialBlock = new MaterialPropertyBlock();
            
            // Ensure submesh index is in range
            if (instanceMesh != null)
                subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

            cullData = new List<CullData>() { new CullData() };
        }

        public void AddStatic(Renderer render)
        {
            render.enabled = false;
            this.renders.Add(render);
            isNeedUpdateBuffer = true;

            cullData.Add(new CullData());
        }
        public void UpdateStaticCullData()
        {
            UpdateArgsBuffer();
            UpdateCullDataBuffer();
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

            for (var i = 0; i < this.renders.Count; i++)
            {
                var render = this.renders[i];
                var bounds = this.renders[i].bounds;
                var data = cullData[i];
                data.center = bounds.center;
                data.extents = bounds.extents;
                data.local2World = render.localToWorldMatrix;
                cullData[i] = data;
            }

            cullDataBuffer.SetData(cullData);
        }

        public void Cull(ComputeShader cs)
        {
            Profiler.BeginSample("Setup localToWorldMatrixBufferCulled");

            if (localToWorldMatrixBufferCulled != null)
            {
                localToWorldMatrixBufferCulled.SetCounterValue(0);
            }
            else
            {
                localToWorldMatrixBufferCulled =
                    new ComputeBuffer(this.renders.Count, sizeof(float) * 4 * 4, ComputeBufferType.Append);
            }
            Profiler.EndSample();

            var k = cs.FindKernel("CSMain");
            cs.SetInt("Count", this.renders.Count);

            cs.SetBool("isOpenGL", isOpenGL);
            cs.SetMatrix("vpMatrix", vpMatrix);
            cs.SetVectorArray("cameraPanels", cameraPanels);
            cs.SetInt("depthTextureSize", depthGenerator.depthTextureSize);

            cs.SetTexture(k, "hizTexture", depthGenerator.depthTexture);
            cs.SetBuffer(k, "LocalToWorldCulled", localToWorldMatrixBufferCulled);
            cs.SetBuffer(k, "CullDataBuffer", cullDataBuffer);

            cs.SetInt("SpawnCount", this.renders.Count - 1);

            int count = this.renders.Count / 8 + 1;
            cs.Dispatch(k, count, 1, 1);
            ComputeBuffer.CopyCount(localToWorldMatrixBufferCulled, argsBuffer, sizeof(uint) * 1);
            this.materialBlock.SetBuffer("localToWorldBuffer", localToWorldMatrixBufferCulled);

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

    public Shader TargetShader;

    private void SetAllRendererToTargetMaterial()
    {
        var allRenderer = GameObject.FindObjectsOfType<Renderer>();
        foreach (var r in allRenderer)
        {
            Material[] mats = r.materials;
            for (var i = 0; i < mats.Length; i++)
            {
                mats[i].shader = TargetShader;
            }

            r.materials = mats;
            r.receiveShadows = false;
        }
    }

    private void OnEnable()
    {
        Random.InitState(0);

        if (IsSpawnTestGO)
        {
            for (int i = 0; i < DrawCount; i++)
            {
                GameObject.Instantiate(TestGO, Random.insideUnitSphere * Size, Quaternion.identity);
            }
        }


        if (!IsDraw)
        {
            return;
        }
        // SetAllRendererToTargetMaterial();

        EnableKey();
        cam = this.GetComponent<Camera>();
        isOpenGL = cam.projectionMatrix.Equals(GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false));
        depthGenerator = GetComponent<DepthTextureGenerator>();


        rendererData.Clear();
        Renderer[] renderObj = GameObject.FindObjectsOfType<Renderer>();
        for (var i = 0; i < renderObj.Length; i++)
        {
            var targetObj = renderObj[i];
            var id = targetObj.GetComponent<MeshFilter>().sharedMesh.GetInstanceID() +
                     targetObj.sharedMaterial.GetInstanceID();
            if (rendererData.TryGetValue(id, out var rendererCall))
            {
                rendererCall.AddStatic(targetObj);
            }
            else
            {
                rendererData.Add(id, new RendererCall(targetObj));
            }
        }
        
        foreach (var cell in rendererData)
        {
            cell.Value.UpdateStaticCullData();
        }
    }

    private void EnableKey()
    {
        Shader.EnableKeyword("HiZCull");
    }

    private void DisableKey()
    {
        Shader.DisableKeyword("HiZCull");
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

    Bounds drawBounds = new Bounds(Vector3.zero, Vector3.one * 10000);

    void Update()
    {
        if (!IsDraw)
        {
            return;
        }
        
        Profiler.BeginSample("Hiz Draw Mesh Update");

        Profiler.BeginSample("Get Camera VP Matrix");
        vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix;
        Profiler.EndSample();
        Profiler.BeginSample("Get Camera FrustumPlanes");
        cameraPanels = CheckExtent.GetFrustumPlane(cam);
        Profiler.EndSample();
        Profiler.BeginSample("Update All Buffers");
        UpdateBuffers();
        Profiler.EndSample();

        Profiler.BeginSample("Draw All");
        // Render
        foreach (var call in this.rendererData)
        {
            Profiler.BeginSample("DrawSingle");
            var renderer = call.Value.renders[0];
            Graphics.DrawMeshInstancedIndirect(call.Value.instanceMesh, call.Value.subMeshIndex, call.Value.instanceMaterial, drawBounds
                , call.Value.argsBuffer,
                0, call.Value.materialBlock, renderer.shadowCastingMode, renderer.receiveShadows,
                renderer.gameObject.layer, DisplayCam);
            Profiler.EndSample();
        }

        Profiler.EndSample();

        Profiler.EndSample();
    }

    void UpdateBuffers()
    {
        foreach (var call in this.rendererData)
        {
            Profiler.BeginSample("Cull Single");
            call.Value.Cull(CullCompute);
            Profiler.EndSample();
        }        
    }

    void OnDisable()
    {
        DisableKey();
        foreach (var call in this.rendererData)
        {
            call.Value.Release();
        }
    }
}