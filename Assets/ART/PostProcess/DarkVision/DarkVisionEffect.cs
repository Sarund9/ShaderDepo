using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DarkVisionEffect : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material Material;
    }

    public Settings settings = new Settings();

    DarkVisionPass renderPass;

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
        renderPass = new DarkVisionPass(settings);
        renderPass.renderPassEvent = settings.WhenToInsert;
    }

}

class DarkVisionPass : ScriptableRenderPass
{
    private DarkVisionEffect.Settings m_Cfg;
    private RenderTargetIdentifier cameraColorTargetIdent;
    private RenderTargetHandle tempTexture;

    static float s_Intensity;

    public static float Intensity
    {
        get => s_Intensity;
        set => s_Intensity = Mathf.Clamp01(value);
    }
    public static Color Color
    {
        get; set;
    } = new Color(0.2f, 0.1f, 0.3f, 1.0f);

    public DarkVisionPass(DarkVisionEffect.Settings settings)
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
        if (s_Intensity < .001f)
            return;

        // Set Material
        m_Cfg.Material.SetColor("_Color", Color);

        CommandBuffer cmd = CommandBufferPool.Get("DarkVisionEffect");
        cmd.Clear();

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
