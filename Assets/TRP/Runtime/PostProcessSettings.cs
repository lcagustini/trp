using UnityEngine;

[CreateAssetMenu(fileName = "PostProcessSettings", menuName = "TRP/Post Process Settings")]
public class PostProcessSettings : ScriptableObject
{
    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)] public int maxIterations;
        [Min(1f)] public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)] public float threshold;
        [Range(0f, 1f)] public float thresholdKnee;
        [Min(0f)] public float intensity;
    }

    [field: SerializeField] public BloomSettings Bloom { get; private set; }
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