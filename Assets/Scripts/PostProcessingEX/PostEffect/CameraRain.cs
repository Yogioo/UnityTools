using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CameraRainRenderer), PostProcessEvent.AfterStack, "Custom/CameraRain")]
public sealed class CameraRain : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("CameraRain effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
}

public sealed class CameraRainRenderer : PostProcessEffectRenderer<CameraRain>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/CameraRain"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}