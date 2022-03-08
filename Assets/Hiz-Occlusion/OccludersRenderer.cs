using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OccludersRenderer : MonoBehaviour
{
    private Camera cam;
    public LayerMask OccluderLayer;
    public RenderTexture DepthRT;

    private void OnEnable()
    {
        cam = this.GetComponent<Camera>();
        cam.enabled = false;
        cam.CopyFrom(Camera.main);
        cam.cullingMask = OccluderLayer;
        cam.clearFlags = CameraClearFlags.Depth;
        cam.depthTextureMode = DepthTextureMode.Depth;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        
    }

    private void Update()
    {
        cam.Render();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest);
        //TODO: Get Depth

        if (DepthRT == null)
        {
            DepthRT = new RenderTexture(src.width, src.height, 0,
                src.format)
            {
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Point
            };
            DepthRT.Create();
        }
        Graphics.CopyTexture(src, DepthRT);
    }

    private void OnPostRender()
    {
        
    }
}