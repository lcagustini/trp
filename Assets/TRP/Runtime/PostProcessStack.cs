using System;
using UnityEngine;
using UnityEngine.Rendering;
using static PostProcessSettings;

public partial class PostProcessStack
{
    private static readonly Rect fullViewRect = new(0f, 0f, 1f, 1f);

    private enum Pass
    {
        BloomPrefilter,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomScatterFinal,

        Copy,
        Final,

        ToneMappingNone,
        ToneMappingACES,
    }

    private const string BUFFER_NAME = "Post Process";

    private const int MAX_BLOOM_PYRAMID_LEVELS = 16;

    private readonly int postProcessSourceId = Shader.PropertyToID("_PostProcessSource");
    private readonly int postProcessSourceId2 = Shader.PropertyToID("_PostProcessSource2");

    private readonly int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private readonly int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    private readonly int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private readonly int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    private readonly int bloomResultId = Shader.PropertyToID("_BloomResult");

    private readonly int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    private readonly int colorFilterId = Shader.PropertyToID("_ColorFilter");

    private readonly int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");

    private readonly int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    private readonly int splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");

    private readonly int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    private readonly int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    private readonly int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");

    private readonly int smhShadowsId = Shader.PropertyToID("_SMHShadows");
    private readonly int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    private readonly int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    private readonly int smhRangeId = Shader.PropertyToID("_SMHRange");

    private readonly int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    private readonly int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    private readonly int colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC");

    private readonly int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    private readonly int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    private readonly CommandBuffer buffer = new()
    {
        name = BUFFER_NAME
    };

    private bool useHDR;

    private ScriptableRenderContext context;
    private Camera camera;
    private PostProcessSettings settings;

    private int colorLUTResolution;
    private CameraSettings.FinalBlendMode finalBlendMode;

    private readonly int bloomPyramidId;

    public bool IsActive => settings != null;

    public PostProcessStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < 2 * MAX_BLOOM_PYRAMID_LEVELS; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostProcessSettings settings, bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.finalBlendMode = finalBlendMode;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        if (DoBloom(sourceId))
        {
            DoColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMapping(sourceId);
        }

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(postProcessSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    private void DrawFinal(RenderTargetIdentifier from)
    {
        buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.destination);
        buffer.SetGlobalTexture(postProcessSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)Pass.Final, MeshTopology.Triangles, 3);
    }

    partial void ApplySceneViewState();

    private bool DoBloom(int sourceId)
    {
        BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || height < 2 * bloom.downscaleLimit || width < 2 * bloom.downscaleLimit || bloom.intensity <= 0f)
        {
            return false;
        }

        buffer.BeginSample("Bloom");

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);

        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, Pass.BloomPrefilter);
        width /= 2;
        height /= 2;

        int fromId = bloomPrefilterId;
        int toId = bloomPyramidId + 1;
        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit) break;
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        buffer.SetGlobalFloat(bloomBicubicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);

        Pass combinePass;
        Pass finalPass;
        float finalIntensity;
        if (bloom.mode == BloomSettings.Mode.Additive)
        {
            combinePass = Pass.BloomAdd;
            finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }

        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(postProcessSourceId2, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(postProcessSourceId2, sourceId);
        buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");
        return true;
    }

    private void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(Mathf.Pow(2f, colorAdjustments.postExposure), colorAdjustments.contrast * 0.01f + 1f, colorAdjustments.hueShift * (1f / 360f), colorAdjustments.saturation * 0.01f + 1f));
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    private void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
    }

    private void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    private void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }

    private void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd));
    }

    private void DoColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));

        Pass pass = settings.ToneMapping.mode switch
        {
            ToneMappingSettings.Mode.None => Pass.ToneMappingNone,
            ToneMappingSettings.Mode.ACES => Pass.ToneMappingACES,
            _ => Pass.Copy
        };
        buffer.SetGlobalFloat(colorGradingLUTInLogId, useHDR && pass != Pass.ToneMappingNone ? 1f : 0f);
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
        DrawFinal(sourceId);
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
}