using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(RadialBlurRenderer), PostProcessEvent.AfterStack, "Custom/RadialBlur")]
public sealed class RadialBlur : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("RadialBlur effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
    public IntParameter stepCount = new IntParameter { value = 5 };
}

public sealed class RadialBlurRenderer : PostProcessEffectRenderer<RadialBlur>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/RadialBlur"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        sheet.properties.SetFloat("_StepCount", settings.stepCount);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}