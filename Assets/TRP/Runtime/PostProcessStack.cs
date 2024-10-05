using UnityEngine;
using UnityEngine.Rendering;

public partial class PostProcessStack
{
    private enum Pass
    {
        BloomPrefilter,
        BloomHorizontal,
        BloomVertical,
        BloomCombine,
        Copy
    }

    private const string BUFFER_NAME = "Post Process";

    private const int MAX_BLOOM_PYRAMID_LEVELS = 16;

    private readonly int postProcessSourceId = Shader.PropertyToID("_PostProcessSource");
    private readonly int postProcessSourceId2 = Shader.PropertyToID("_PostProcessSource2");
    private readonly int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private readonly int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    private readonly int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private readonly int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");

    private readonly CommandBuffer buffer = new()
    {
        name = BUFFER_NAME
    };

    private ScriptableRenderContext context;
    private Camera camera;
    private PostProcessSettings settings;

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

    public void Setup(ScriptableRenderContext context, Camera camera, PostProcessSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        DoBloom(sourceId);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(postProcessSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    partial void ApplySceneViewState();

    private void DoBloom(int sourceId)
    {
        buffer.BeginSample("Bloom");

        PostProcessSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2;
        int height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || height < 2 * bloom.downscaleLimit || width < 2 * bloom.downscaleLimit || bloom.intensity <= 0f)
        {
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.EndSample("Bloom");
            return;
        }
        
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);

        RenderTextureFormat format = RenderTextureFormat.Default;
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

        buffer.SetGlobalFloat(bloomIntensityId, 1f);
        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(postProcessSourceId2, toId + 1);
                Draw(fromId, toId, Pass.BloomCombine);
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

        buffer.SetGlobalFloat(bloomIntensityId, bloom.intensity);
        buffer.SetGlobalTexture(postProcessSourceId2, sourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        buffer.ReleaseTemporaryRT(fromId);

        buffer.EndSample("Bloom");
    }
}