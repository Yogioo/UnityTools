// 给予阴影剔除视椎, 方便阴影剔除 (阴影视椎剔除 + 阴影遮挡剔除)

using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light))]
public class FullCullingMainLight : MonoBehaviour
{
    public static FullCullingMainLight Instance;
    private Light m_Light;
    private Transform m_LightTrans;

    private Transform m_ShadowCamTrans;
    public Camera m_ShadowCam;
    private Camera m_MainCam;


    public float ShadowDistance = 10;

    private void OnValidate()
    {
    }

    private void Awake()
    {
        Instance = this;
        m_Light = this.GetComponent<Light>();
        m_LightTrans = m_Light.transform;
        if (!InvalidCheck())
        {
            return;
        }

        m_ShadowCamTrans = m_ShadowCam.transform;

        m_MainCam = Camera.main;
    }

    private bool InvalidCheck()
    {
        bool isInvalid = false;
        if (m_Light.type != LightType.Directional)
        {
            isInvalid = true;
        }
        else if (m_Light.shadows == LightShadows.None)
        {
            isInvalid = true;
        }
        else
        {
            isInvalid = false;
        }

        if (isInvalid)
        {
            this.enabled = false;
            Debug.Log($"{nameof(FullCullingMainLight)} 组件配置无效 灯光无阴影");
            return false;
        }

        return true;
    }

    void extract_planes_from_projmat(in Matrix4x4 mat,
        out Vector4 left, out Vector4 right,
        out Vector4 bottom, out Vector4 top,
        out Vector4 near, out Vector4 far)
    {
        left = right = bottom = top = near = far = Vector4.zero;

        for (int i = 4; i >= 0; i--)
        {
            left[i] = mat[i-1, 3] + mat[i-1, 0];
        }
        for (int i = 4; i >= 0; i--) right[i] = mat[i-1, 3] - mat[i-1, 0];
        for (int i = 4; i >= 0; i--) bottom[i] = mat[i-1, 3] + mat[i-1, 1];
        for (int i = 4; i >= 0; i--) top[i] = mat[i-1, 3] - mat[i-1, 1];
        for (int i = 4; i >= 0; i--) near[i] = mat[i-1, 3] + mat[i-1, 2];
        for (int i = 4; i >= 0; i--) far[i] = mat[i-1, 3] - mat[i-1, 2];
    }

    private void Update()
    {
        // var shadow = m_Light.shadowMatrixOverride;
        var shadowView = Shader.GetGlobalMatrix("unity_WorldToLight");
        var shadowProjection = Shader.GetGlobalMatrix("unity_WorldToShadow");
        Debug.Log(shadowView);
        extract_planes_from_projmat(shadowProjection, out var l, out var r, out var b, out var t, out var n, out var f);
        var mainCamTrans = m_MainCam.transform;
        var mainCamPos = mainCamTrans.position;
        // var mainCamDir = mainCamTrans.forward;
        // var midDis = (m_MainCam.farClipPlane - m_MainCam.nearClipPlane) / 2.0f;
        // this.m_ShadowCamTrans.position = mainCamPos + midDis * mainCamDir;
        var newPos = mainCamPos - m_LightTrans.forward * m_ShadowCam.farClipPlane / 2.0f;
        var mainForward = mainCamTrans.forward;
        mainForward.y = 0;
        newPos += mainForward * ShadowDistance / 2.0f;
        this.m_ShadowCamTrans.SetPositionAndRotation(newPos, m_LightTrans.rotation);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.matrix = m_MainCam.transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, m_MainCam.fieldOfView, m_MainCam.farClipPlane,
                m_MainCam.nearClipPlane, m_MainCam.aspect);

            Gizmos.matrix = m_ShadowCam.transform.localToWorldMatrix;

            var y = m_ShadowCam.orthographicSize * 2;
            var x = m_ShadowCam.aspect * y;
            var z = m_ShadowCam.farClipPlane;
            Gizmos.DrawWireCube(Vector3.forward * z / 2.0f, new Vector3(x, y, z));
            // Gizmos.DrawCube(Vector3.zero,);
        }
    }
}