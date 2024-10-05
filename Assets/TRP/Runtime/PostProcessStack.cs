using UnityEngine;
using UnityEngine.Rendering;

public partial class PostProcessStack
{
    private enum Pass
    {
        Copy
    }

    private const string BUFFER_NAME = "Post Process";

    private readonly int fxSourceId = Shader.PropertyToID("_PostFXSource");

    private readonly CommandBuffer buffer = new()
    {
        name = BUFFER_NAME
    };

    private ScriptableRenderContext context;
    private Camera camera;
    private PostProcessSettings settings;

    public bool IsActive => settings != null;

    public void Setup(ScriptableRenderContext context, Camera camera, PostProcessSettings settings)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    partial void ApplySceneViewState();
}