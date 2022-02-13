using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(MatrixRenderer), PostProcessEvent.AfterStack, "Custom/Matrix")]
public sealed class Matrix : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Matrix effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };

    public TextureParameter _FontTex = new TextureParameter();
}

public sealed class MatrixRenderer : PostProcessEffectRenderer<Matrix>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Matrix"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        sheet.properties.SetMatrix("_ViewToWorld", Camera.main.cameraToWorldMatrix);
        sheet.properties.SetTexture("_FontTex", settings._FontTex);
        // Material.SetMatrix("_ViewToWorld", Camera.cameraToWorldMatrix);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}