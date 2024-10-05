using UnityEngine;

[CreateAssetMenu(fileName = "PostProcessSettings", menuName = "TRP/Post Process Settings")]
public class PostProcessSettings : ScriptableObject
{
    [SerializeField] private Shader shader;

    [System.NonSerialized] private Material material;
    public Material Material
    {
        get
        {
            if (material == null && shader != null) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return material;
        }
    }
}