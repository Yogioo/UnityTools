using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(RaymarchRenderer), PostProcessEvent.AfterStack, "Custom/Raymarch")]
public sealed class Raymarch : PostProcessEffectSettings
{
    public TextureParameter maskTex = new TextureParameter();
}

public sealed class RaymarchRenderer : PostProcessEffectRenderer<Raymarch>
{

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Raymarch"));
        sheet.properties.SetTexture("_MaskTex", settings.maskTex);
        sheet.properties.SetMatrix("_ViewToWorld", Camera.main.cameraToWorldMatrix);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}