// Get Depth Texture & Generate Mipmap

// Reference By https://zhuanlan.zhihu.com/p/396979267
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DepthTextureGenerator : MonoBehaviour
{
    public Shader depthTextureShader;

    RenderTexture m_depthTexture;

    public RenderTexture depthTexture
    {
        get => m_depthTexture;
    }

    int m_depthTextureSize = 0;

    public int depthTextureSize
    {
        get
        {
            if (m_depthTextureSize == 0)
                m_depthTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
            return m_depthTextureSize;
        }
    }

    Material m_depthTextureMaterial;
    const RenderTextureFormat m_depthTextureFormat = RenderTextureFormat.RHalf; 

    int m_depthTextureShaderID;

    void OnEnable()
    {
        m_depthTextureMaterial = new Material(depthTextureShader);
        this.GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

        m_depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");
        
        InitDepthTexture();
    }

    void InitDepthTexture()
    {
        if (m_depthTexture != null) return;
        m_depthTexture = new RenderTexture(depthTextureSize, depthTextureSize, 0, m_depthTextureFormat)
            {
                autoGenerateMips = false,
                useMipMap = true,
                filterMode = FilterMode.Point
            };
        m_depthTexture.Create();
    }

    void OnPostRender()
    {
        int w = m_depthTexture.width;
        int mipmapLevel = 0;

        RenderTexture currentRenderTexture = null; //当前mipmapLevel对应的mipmap
        RenderTexture preRenderTexture = null; //上一层的mipmap，即mipmapLevel-1对应的mipmap

        //如果当前的mipmap的宽高大于1，则计算下一层的mipmap
        while (w > 1)
        {
            currentRenderTexture = RenderTexture.GetTemporary(w, w, 0, m_depthTextureFormat);
            currentRenderTexture.filterMode = FilterMode.Point;
            if (preRenderTexture == null)
            {
                //Mipmap[0]即copy原始的深度图
                Graphics.Blit(Shader.GetGlobalTexture(m_depthTextureShaderID), currentRenderTexture);
            }
            else
            {
                //将Mipmap[i] Blit到Mipmap[i+1]上
                Graphics.Blit(preRenderTexture, currentRenderTexture, m_depthTextureMaterial);
                RenderTexture.ReleaseTemporary(preRenderTexture);
            }

            Graphics.CopyTexture(currentRenderTexture, 0, 0, m_depthTexture, 0, mipmapLevel);
            preRenderTexture = currentRenderTexture;

            w /= 2;
            mipmapLevel++;
        }
        
        RenderTexture.ReleaseTemporary(preRenderTexture);
    }

    void OnDestroy()
    {
        m_depthTexture?.Release();
        m_depthTexture = null;
    }
}