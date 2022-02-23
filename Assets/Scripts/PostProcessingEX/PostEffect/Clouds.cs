using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CloudsRenderer), PostProcessEvent.AfterStack, "Custom/Clouds")]
public sealed class Clouds : PostProcessEffectSettings
{
    public FloatParameter rayStep = new FloatParameter() {value = 0.01f};

    public TextureParameter weatherMap = new TextureParameter();
    public TextureParameter shapeTex = new TextureParameter();
    public FloatParameter shapeTiling = new FloatParameter() {value = 0.1f};
    public TextureParameter shapeDetailTex = new TextureParameter();
    public FloatParameter shapeDetailTiling = new FloatParameter() {value = 0.1f};
    public Vector3Parameter shapeNoiseWeights = new Vector3Parameter { value = new Vector4(-0.17f, 27.17f, -3.65f) };
    public FloatParameter densityOffset = new FloatParameter() {value = 1};
    public FloatParameter densityMultiplier = new FloatParameter() {value = 1};
    

    public Vector3Parameter boundsMin = new Vector3Parameter(){value =  new Vector3(-10,-10,-10)};
    public Vector3Parameter boundsMax = new Vector3Parameter(){value =  new Vector3(10,10,10)};
    
    public FloatParameter lightAbsorptionTowardSun = new FloatParameter() {value = .1f};
    public FloatParameter lightAbsorptionThroughCloud = new FloatParameter() {value = 1};
    
    public ColorParameter colorA = new ColorParameter();
    public ColorParameter colorB = new ColorParameter();
    public FloatParameter colorOffsetA = new FloatParameter();
    public FloatParameter colorOffsetB = new FloatParameter();
    public FloatParameter darknessThreshold = new FloatParameter();

    public Vector4Parameter phaseParams = new Vector4Parameter(){value = new Vector4(0.72f, 1, 0.5f, 1.58f) };
    // float3 _boundsMin, _boundsMax;
    // float _lightAbsorptionTowardSun;
    // float3 _ColA, _ColB;
    // float _ColorOffsetA, _ColorOffsetB;
    // float3 _LightColor0;
    // float _DarknessThreshold;
}

public sealed class CloudsRenderer : PostProcessEffectRenderer<Clouds>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Clouds"));

        var projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseProjectionMatrix"),projectionMatrix.inverse);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseViewMatrix"),context.camera.cameraToWorldMatrix);
        
        sheet.properties.SetTexture(Shader.PropertyToID("_WeatherMap"),settings.weatherMap.value);
        sheet.properties.SetTexture(Shader.PropertyToID("_ShapeTex"),settings.shapeTex.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_ShapeTiling"),settings.shapeTiling.value);
        sheet.properties.SetTexture(Shader.PropertyToID("_ShapeDetailTex"),settings.shapeDetailTex.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_ShapeDetailTiling"),settings.shapeDetailTiling.value);
        sheet.properties.SetVector(Shader.PropertyToID("_ShapeNoiseWeights"),settings.shapeNoiseWeights.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_DensityOffset"),settings.densityOffset.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_DensityMultiplier"),settings.densityMultiplier.value);
        
            
        sheet.properties.SetVector(Shader.PropertyToID("_boundsMin"),settings.boundsMin.value);
        sheet.properties.SetVector(Shader.PropertyToID("_boundsMax"),settings.boundsMax.value);
        
        sheet.properties.SetFloat(Shader.PropertyToID("_lightAbsorptionTowardSun"),settings.lightAbsorptionTowardSun.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_lightAbsorptionThroughCloud"),settings.lightAbsorptionThroughCloud.value);
        sheet.properties.SetColor(Shader.PropertyToID("_ColA"),settings.colorA.value);
        sheet.properties.SetColor(Shader.PropertyToID("_ColB"),settings.colorB.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_ColorOffsetA"),settings.colorOffsetA.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_ColorOffsetB"),settings.colorOffsetB.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_DarknessThreshold"),settings.darknessThreshold.value);
        
        sheet.properties.SetVector(Shader.PropertyToID("_phaseParams"),settings.phaseParams.value);
        
        sheet.properties.SetFloat(Shader.PropertyToID("_rayStep"),settings.rayStep.value);

        
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}