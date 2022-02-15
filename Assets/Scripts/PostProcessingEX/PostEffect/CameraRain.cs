using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CameraRainRenderer), PostProcessEvent.AfterStack, "Custom/CameraRain", false)]
public sealed class CameraRain : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("CameraRain effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
    [Range(0f, 2f)]
    public FloatParameter gaussStrength = new FloatParameter { value = 1f };

    [Range(0f, 2f)]
    public FloatParameter dropSpeed = new FloatParameter { value = 1f };
    [Range(0f, 1f)]
    public FloatParameter rainAmount = new FloatParameter { value = 1f };
    [Range(-5f, 5f)]
    public FloatParameter rainScale = new FloatParameter { value = 0 };



}

public sealed class CameraRainRenderer : PostProcessEffectRenderer<CameraRain>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/CameraRain"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        sheet.properties.SetFloat("_GaussStrength", settings.gaussStrength);
        sheet.properties.SetFloat("_DropSpeed", settings.dropSpeed);
        sheet.properties.SetFloat("_RainAmount", settings.rainAmount);
        sheet.properties.SetFloat("_RainScale", settings.rainScale);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}