#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class TRPShaderGUI : ShaderGUI 
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        
        EditorGUI.BeginChangeCheck();
        materialEditor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck()) {
            foreach (Material m in materialEditor.targets) {
                m.globalIlluminationFlags &=
                    ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
}
#endif