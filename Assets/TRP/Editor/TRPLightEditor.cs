using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(ThorRenderPipelineAsset))]
public class TRPLightEditor : LightEditor 
{
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();
        }
        
        Light light = target as Light;
        if (light.cullingMask != -1) {
            EditorGUILayout.HelpBox("Culling Mask only affects shadows.", MessageType.Warning);
        }
    }
}