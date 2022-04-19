// 给予阴影剔除视椎, 方便阴影剔除 (阴影视椎剔除 + 阴影遮挡剔除)

using System;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class FullCullingMainLight : MonoBehaviour
{
    public static FullCullingMainLight Instance;
    private Light m_Light;

    private Transform m_ShadowCamTrans;
    public Camera m_ShadowCam;
    private void OnValidate()
    {
    }
    private void Awake()
    {
        Instance = this;
        m_Light = this.GetComponent<Light>();
        if (!InvalidCheck())
        {
            return;
        }

        m_ShadowCamTrans = m_ShadowCam.transform;
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

    private void Update()
    {
    }

    private void OnDrawGizmos()
    {
    }
}