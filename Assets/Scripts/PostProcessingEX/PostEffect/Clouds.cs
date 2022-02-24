using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(RayMarchingCloudRenderer), PostProcessEvent.BeforeStack, "Custom/Clouds")]
public sealed class RayMarchingCloud : PostProcessEffectSettings
{
    //Texture
    [Header("整体形状贴图")]
    public TextureParameter noise3D = new TextureParameter { value = null };
    [Header("细节形状贴图")]
    public TextureParameter noiseDetail3D = new TextureParameter { value = null };
    
    [Header("整体形状贴图缩放")]
    public FloatParameter shapeTiling = new FloatParameter { value = 0.01f };
    [Header("细节形状贴图缩放")]
    public FloatParameter detailTiling = new FloatParameter { value = 0.1f };

    [Header("天气贴图")]
    public TextureParameter weatherMap = new TextureParameter { value = null };
    [Header("遮罩图 用于偏移天气采样")]
    public TextureParameter maskNoise = new TextureParameter { value = null };
    [Header("Dither图 用于处理提高步长后的阶梯感")]
    public TextureParameter blueNoise = new TextureParameter { value = null };

    //light
    //public FloatParameter numStepsLight = new FloatParameter { value = 6 };
    [Header("一阶颜色")]
    public ColorParameter colA = new ColorParameter { value = Color.white };
    [Header("二阶颜色")]
    public ColorParameter colB = new ColorParameter { value = Color.white };
    [Header("一阶颜色阈值")]
    public FloatParameter colorOffset1 = new FloatParameter { value = 0.59f };
    [Header("二阶颜色阈值")]
    public FloatParameter colorOffset2 = new FloatParameter { value = 1.02f };
    [Header("太阳直射光吸收系数")]
    public FloatParameter lightAbsorptionTowardSun = new FloatParameter { value = 0.1f };
    [Header("云层光吸收系数")]
    public FloatParameter lightAbsorptionThroughCloud = new FloatParameter { value = 1 };
    [Header("太阳散射系数")]
    public Vector4Parameter phaseParams = new Vector4Parameter { value = new Vector4(0.72f, 1, 0.5f, 1.58f) };

    //density
    [Header("密度偏移")]
    public FloatParameter densityOffset = new FloatParameter { value = 4.02f };
    [Header("密度系数")]
    public FloatParameter densityMultiplier = new FloatParameter { value = 2.31f };
    [Header("步长")]
    public FloatParameter step = new FloatParameter { value = 1.2f };
    [Header("步长")]
    public FloatParameter rayStep = new FloatParameter { value = 1.2f };
    [Header("抖动偏移距离")]
    public FloatParameter rayOffsetStrength = new FloatParameter { value = 1.5f };
    [Header("降分辨率次数")]
    [Range(1, 16)]
    public IntParameter Downsample = new IntParameter { value = 4 };
    [Header("高度系数")]
    [Range(0, 1)]
    public FloatParameter heightWeights = new FloatParameter { value = 1 };
    [Header("形状控制权重")]
    public Vector4Parameter shapeNoiseWeights = new Vector4Parameter { value = new Vector4(-0.17f, 27.17f, -3.65f, -0.08f) };
    [Header("细节权重")]
    public FloatParameter detailWeights = new FloatParameter { value = -3.76f };
    [Header("细节形状控制权重")]
    public FloatParameter detailNoiseWeight = new FloatParameter { value = 0.12f };
    [Header("x形状移动速度 y细节移动速度 z形状重复 w细节重复")]
    public Vector4Parameter xy_Speed_zw_Warp = new Vector4Parameter { value = new Vector4(0.05f, 1, 1, 10) };
}



public sealed class RayMarchingCloudRenderer : PostProcessEffectRenderer<RayMarchingCloud>
{
    Transform cloudTransform;
    Vector3 boundsMin;
    Vector3 boundsMax;
    [HideInInspector]
    public Material DownscaleDepthMaterial;
    public override DepthTextureMode GetCameraFlags()
    {
        return DepthTextureMode.Depth; 
    }
    public override void Init()
    {
        var cloudBox = GameObject.FindObjectOfType<CloudBox>();

        if (cloudBox != null)
        {
            cloudTransform = cloudBox.GetComponent<Transform>();
        }
        else
        {
            Debug.LogError("Without objects in the scene: CloudBox");
        }
    }
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/RayMarchingCloud"));
        //sheet.properties.SetColor(Shader.PropertyToID("_color"), settings.color);
         
        Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseProjectionMatrix"), projectionMatrix.inverse);
        sheet.properties.SetMatrix(Shader.PropertyToID("_InverseViewMatrix"), context.camera.cameraToWorldMatrix);
        sheet.properties.SetVector(Shader.PropertyToID("_CameraDir"), context.camera.transform.forward);
        //sheet.properties.SetVector(Shader.PropertyToID("_WorldSpaceLightPos0"), LightDir.transform.forward);

        if (cloudTransform != null){
            boundsMin = cloudTransform.position - cloudTransform.localScale / 2;
            boundsMax = cloudTransform.position + cloudTransform.localScale / 2;

            sheet.properties.SetVector(Shader.PropertyToID("_boundsMin"), boundsMin);
            sheet.properties.SetVector(Shader.PropertyToID("_boundsMax"), boundsMax);
        }

        if (settings.noise3D.value != null)
        {
            sheet.properties.SetTexture(Shader.PropertyToID("_noiseTex"), settings.noise3D.value);
        }
        
        if (settings.noiseDetail3D.value != null)
        {
            sheet.properties.SetTexture(Shader.PropertyToID("_noiseDetail3D"), settings.noiseDetail3D.value);
        }
        if (settings.weatherMap.value != null)
        {
            sheet.properties.SetTexture(Shader.PropertyToID("_weatherMap"), settings.weatherMap.value);
        }
        if (settings.maskNoise.value != null)
        {
            sheet.properties.SetTexture(Shader.PropertyToID("_maskNoise"), settings.maskNoise.value);
        }


        if (settings.blueNoise.value != null)
        {
            Vector4 screenUv = new Vector4(
            (float)context.screenWidth / (float)settings.blueNoise.value.width,
            (float)context.screenHeight / (float)settings.blueNoise.value.height,0,0);
            sheet.properties.SetVector(Shader.PropertyToID("_BlueNoiseCoords"), screenUv);
            sheet.properties.SetTexture(Shader.PropertyToID("_BlueNoise"), settings.blueNoise.value);
        }
        
        sheet.properties.SetFloat(Shader.PropertyToID("_shapeTiling"), settings.shapeTiling.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_detailTiling"), settings.detailTiling.value);

        sheet.properties.SetFloat(Shader.PropertyToID("_step"), settings.step.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_rayStep"), settings.rayStep.value);

        //sheet.properties.SetFloat(Shader.PropertyToID("_dstTravelled"),settings.dstTravelled.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_densityOffset"), settings.densityOffset.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_densityMultiplier"), settings.densityMultiplier.value);

        
        //sheet.properties.SetInt(Shader.PropertyToID("_numStepsLight"), (int)settings.numStepsLight.value);

        sheet.properties.SetColor(Shader.PropertyToID("_colA"), settings.colA.value);
        sheet.properties.SetColor(Shader.PropertyToID("_colB"), settings.colB.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_colorOffset1"), settings.colorOffset1.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_colorOffset2"), settings.colorOffset2.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_lightAbsorptionTowardSun"), settings.lightAbsorptionTowardSun.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_lightAbsorptionThroughCloud"), settings.lightAbsorptionThroughCloud.value);

        
        sheet.properties.SetFloat(Shader.PropertyToID("_rayOffsetStrength"), settings.rayOffsetStrength.value);
        sheet.properties.SetVector(Shader.PropertyToID("_phaseParams"), settings.phaseParams.value);
        sheet.properties.SetVector(Shader.PropertyToID("_xy_Speed_zw_Warp"), settings.xy_Speed_zw_Warp.value);
        
        sheet.properties.SetVector(Shader.PropertyToID("_shapeNoiseWeights"), settings.shapeNoiseWeights.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_heightWeights"), settings.heightWeights.value);

        
        sheet.properties.SetFloat(Shader.PropertyToID("_detailWeights"), settings.detailWeights.value);
        sheet.properties.SetFloat(Shader.PropertyToID("_detailNoiseWeight"), settings.detailNoiseWeight.value);


        if (cloudTransform == null)
        {
            Debug.LogError("Without objects in the scene: CloudBox");
            return;
        }
        Quaternion rotation = Quaternion.Euler(cloudTransform.eulerAngles);
        Vector3 scaleMatrix = cloudTransform.localScale * 0.1f;
        scaleMatrix = new Vector3(1 / scaleMatrix.x, 1 / scaleMatrix.y, 1 / scaleMatrix.z);
        Matrix4x4 TRSMatrix = Matrix4x4.TRS(cloudTransform.position, rotation, scaleMatrix);
        sheet.properties.SetMatrix(Shader.PropertyToID("_TRSMatrix"), TRSMatrix);

        var cmd = context.command;

        //降深度采样
        var DownsampleDepthID = Shader.PropertyToID("_DownsampleTemp");
        context.GetScreenSpaceTemporaryRT(cmd, DownsampleDepthID, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Point, context.screenWidth / settings.Downsample.value, context.screenHeight / settings.Downsample.value);
        cmd.BlitFullscreenTriangle(context.source, DownsampleDepthID, sheet, 1);
        cmd.SetGlobalTexture(Shader.PropertyToID("_LowDepthTexture"), DownsampleDepthID);

        //降cloud分辨率 并使用第1个pass 渲染云
        var DownsampleColorID = Shader.PropertyToID("_DownsampleColor");
        context.GetScreenSpaceTemporaryRT(cmd, DownsampleColorID, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Trilinear, context.screenWidth / settings.Downsample.value, context.screenHeight / settings.Downsample.value);
        cmd.BlitFullscreenTriangle(context.source, DownsampleColorID, sheet,0);

        //降分辨率后的云设置回_DownsampleColor
        cmd.SetGlobalTexture(Shader.PropertyToID("_DownsampleColor"), DownsampleColorID);

        //使用第0个Pass 合成
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 2);

        cmd.ReleaseTemporaryRT(DownsampleColorID);
        cmd.ReleaseTemporaryRT(DownsampleDepthID);

    }
}