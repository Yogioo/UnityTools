using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ScanningWaveRenderer), PostProcessEvent.AfterStack, "Custom/ScanningWave")]
public sealed class ScanningWave : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("ScanningWave effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };

    public TextureParameter _FontTex = new TextureParameter();
    public TextureParameter _ScanTex = new TextureParameter();

    public Vector3Parameter scanningCenter = new Vector3Parameter();
    [ColorUsage(true,true)]
    public ColorParameter waveColor = new ColorParameter();

    
}

public sealed class ScanningWaveRenderer : PostProcessEffectRenderer<ScanningWave>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/ScanningWave"));
        sheet.properties.SetFloat("_Blend", (1-settings.blend)* 10);
        sheet.properties.SetMatrix("_ViewToWorld", Camera.main.cameraToWorldMatrix);
        sheet.properties.SetTexture("_FontTex", settings._FontTex);
        sheet.properties.SetVector("_ScanningCenter", settings.scanningCenter);
        sheet.properties.SetColor("_WaveColor", settings.waveColor);
        sheet.properties.SetTexture("_ScanTex", settings._ScanTex);
        
        // Material.SetScanningWave("_ViewToWorld", Camera.cameraToWorldScanningWave);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}