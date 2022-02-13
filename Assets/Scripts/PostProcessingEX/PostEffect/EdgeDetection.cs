using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(EdgeDetectionRenderer), PostProcessEvent.BeforeStack, "Custom/EdgeDetection")]
public sealed class EdgeDetection : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("EdgeDetection effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
}

public sealed class EdgeDetectionRenderer : PostProcessEffectRenderer<EdgeDetection>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/EdgeDetection"));
        sheet.properties.SetFloat("_Blend", settings.blend.value);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}