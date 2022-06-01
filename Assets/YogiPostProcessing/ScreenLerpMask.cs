using UnityEngine;

[ExecuteInEditMode]
public class ScreenLerpMask : MonoBehaviour
{
    public Material mat;
    public float BlendScale = 0;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture src0 = RenderTexture.GetTemporary(source.width, source.height);
        mat.SetTexture("_MainTex", source);
        mat.SetFloat("_BlendScale", BlendScale);
        Graphics.Blit(source, src0, mat, 0);
        Graphics.Blit(src0, destination);

        RenderTexture.ReleaseTemporary(src0);
    }
}