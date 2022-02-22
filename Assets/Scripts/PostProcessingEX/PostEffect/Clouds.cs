using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CloudsRenderer), PostProcessEvent.AfterStack, "Custom/Clouds")]
public sealed class Clouds : PostProcessEffectSettings
{
}

public sealed class CloudsRenderer : PostProcessEffectRenderer<Clouds>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Clouds"));

        var projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseProjectionMatrix"),projectionMatrix.inverse);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseViewMatrix"),context.camera.cameraToWorldMatrix);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}