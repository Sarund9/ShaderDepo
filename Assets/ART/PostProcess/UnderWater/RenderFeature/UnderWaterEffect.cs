using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class UnderWaterEffect : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material Material;
        public Material fullClipMat;
        public bool enableInEditMode;
    }
    
    public Settings settings = new Settings();

    UnderWaterPass renderPass = null;
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.IsEnabled)
        {
            return;
        }

        var cameraColorTargetIdent = renderer.cameraColorTarget;
        renderPass.Setup(ref cameraColorTargetIdent);
        renderer.EnqueuePass(renderPass);
    }
    
    public override void Create()
    {
        renderPass = new UnderWaterPass(settings);
        renderPass.renderPassEvent = settings.WhenToInsert;
    }

}

struct WaterShaderParams
{
    public float waterLevel;
    public Vector3 UpDir;

    public Color baseColor;
    public Color blendColor;
    public float depth;
    public float strenght;
    public float blend;
    public float minOpacity;
}

class UnderWaterPass : ScriptableRenderPass
{

    RenderTargetIdentifier cameraColorTargetIdent;
    RenderTargetHandle tempTexture;

    UnderWaterEffect.Settings m_Cfg;

    public delegate float WaterVolDriver(Vector3 camPos, out WaterShaderParams waterParams);

    static List<WaterVolDriver> m_Drivers = new List<WaterVolDriver>();

    public static event WaterVolDriver Drivers
    {
        add => m_Drivers.Add(value);
        remove => m_Drivers.Remove(value);
    }

    static float GetIntensity(Vector3 camPos, out WaterShaderParams waterParams)
    {
        float i = 0;
        waterParams = new WaterShaderParams{ };
        foreach (var item in m_Drivers) {
            var newI = item(camPos, out var newWaterParams);
            if (newI > i)
            {
                i = newI;
                waterParams = newWaterParams;
            }
            //Debug.Log($"DRIVER RETURNS: {newI}");
            if (i == 1)
                break;
        }
        return i;
    }

    public UnderWaterPass(UnderWaterEffect.Settings settings)
    {
        m_Cfg = settings;
    }

    public void Setup(ref RenderTargetIdentifier cameraColorTargetIdent)
    {
        this.cameraColorTargetIdent = cameraColorTargetIdent;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!Application.isPlaying && !m_Cfg.enableInEditMode)
        {
            return;
        }
        
        var cam = renderingData.cameraData.camera;
        var pos = cam.transform.position;
        var camPos = renderingData.cameraData.camera.transform.position;
        //Debug.Log($"EXECUTE {camPos}");
        var intensity = GetIntensity(camPos, out var waterParams);

        if (intensity < 0.01f)
            return;

        // SET SHADER PARAMS
        {
            Shader.SetGlobalFloat("_UNDER_WATER_EFFECT_WATER_LEVEL", waterParams.waterLevel);
            // TODO: Up Dir
            m_Cfg.Material.SetColor("_BaseColor", waterParams.baseColor);
            m_Cfg.Material.SetColor("_BlendColor", waterParams.blendColor);
            m_Cfg.Material.SetFloat("_Depth", waterParams.depth);
            m_Cfg.Material.SetFloat("_Strenght", waterParams.strenght);
            m_Cfg.Material.SetFloat("_Blend", waterParams.blend);
            m_Cfg.Material.SetFloat("_MinOpacity", waterParams.minOpacity);

        }
        //if (pos.y > m_Cfg.WaterLevel)
        //    return;

        CommandBuffer cmd = CommandBufferPool.Get("UnderWaterEffect");
        cmd.Clear();

        // Set Camera Direction
        var ray1 = cam.ViewportPointToRay(Vector3.zero, Camera.MonoOrStereoscopicEye.Mono);
        m_Cfg.Material.SetVector("_CamDir1", ray1.direction);
        var ray2 = cam.ViewportPointToRay(Vector3.one, Camera.MonoOrStereoscopicEye.Mono);
        m_Cfg.Material.SetVector("_CamDir2", ray2.direction);

        // Post Process
        cmd.Blit(cameraColorTargetIdent, tempTexture.Identifier(), m_Cfg.Material, 0);
        cmd.Blit(tempTexture.Identifier(), cameraColorTargetIdent);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(tempTexture.id);
    }
}
