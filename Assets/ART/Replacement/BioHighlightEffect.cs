using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BioHighlightEffect : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material Material;
        public Material SelectionMaterial;
        public LayerMask layerMask = 0;
    }

    public Settings settings = new Settings();

    BioShaderPass renderPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.IsEnabled)
        {
            return;
        }

        var cameraColorTargetIdent = renderer.cameraColorTarget;
        //renderPass.Setup(ref cameraColorTargetIdent);
        renderer.EnqueuePass(renderPass);
    }

    public override void Create()
    {
        renderPass = new BioShaderPass(settings);
        renderPass.renderPassEvent = settings.WhenToInsert;
    }
}

class BioShaderPass : ScriptableRenderPass
{
    private BioHighlightEffect.Settings m_Cfg;

    public static bool Active { get; set; }

    public static Color Color { get; set; }

    public static Renderer Selection { get; set; }

    public static Color SelectionColor { get; set; }
    public static Color SelectionBlendColor { get; set; }


    private readonly ProfilingSampler m_ProfilingSampler;
    private RenderStateBlock m_RenderStateBlock;
    private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
    private FilteringSettings m_FilteringSettings;

    public BioShaderPass(BioHighlightEffect.Settings settings)
    {
        m_Cfg = settings;
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_Cfg.layerMask);
        m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
        m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!Active)
            return;
        
        SortingCriteria sortingCriteria = SortingCriteria.BackToFront;

        DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        drawingSettings.overrideMaterial = m_Cfg.Material;
        drawingSettings.enableDynamicBatching = true;
        drawingSettings.enableInstancing = true;

        // Stencil State
        StencilState stencilState = StencilState.defaultValue;
        stencilState.enabled = true;
        stencilState.SetCompareFunction(CompareFunction.Disabled);
        stencilState.SetPassOperation(StencilOp.Zero);
        stencilState.SetFailOperation(StencilOp.Zero);
        stencilState.SetZFailOperation(StencilOp.Zero);

        m_RenderStateBlock.stencilState = stencilState;
        // Material Parameters
        m_Cfg.Material.SetColor("_Color", Color);

        CommandBuffer cmd = CommandBufferPool.Get("OutlineEffect");
        cmd.Clear();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);

            if (Selection != null)
            {
                m_Cfg.SelectionMaterial.SetColor("_Color", SelectionColor);
                m_Cfg.SelectionMaterial.SetColor("_BlendColor", SelectionBlendColor);

                cmd.DrawRenderer(Selection, m_Cfg.SelectionMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
        }
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

}