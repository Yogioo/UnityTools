using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(MaskRenderer), PostProcessEvent.AfterStack, "Custom/Mask")]
public sealed class Mask : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Mask effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
    public TextureParameter maskTex = new TextureParameter();
}

public sealed class MaskRenderer : PostProcessEffectRenderer<Mask>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Mask"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        if(settings.maskTex.overrideState){
            sheet.properties.SetTexture("_MaskTex", settings.maskTex);
        }

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}