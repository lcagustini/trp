using UnityEngine;

[CreateAssetMenu(fileName = "PostProcessSettings", menuName = "TRP/Post Process Settings")]
public class PostProcessSettings : ScriptableObject
{
    [System.Serializable]
    public struct BloomSettings
    {
        public enum Mode
        {
            Additive,
            Scattering
        }

        public Mode mode;

        [Range(0f, 16f)] public int maxIterations;
        [Min(1f)] public int downscaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)] public float threshold;
        [Range(0f, 1f)] public float thresholdKnee;
        [Min(0f)] public float intensity;

        [Range(0.05f, 0.95f)] public float scatter;
    }

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode
        {
            None,
            ACES
        }

        public Mode mode;
    }


    [System.Serializable]
    public struct ColorAdjustmentsSettings
    {
        public float postExposure;
        [Range(-100f, 100f)] public float contrast;
        [ColorUsage(false, true)] public Color colorFilter;
        [Range(-180f, 180f)] public float hueShift;
        [Range(-100f, 100f)] public float saturation;
    }

    [System.Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)] public float temperature;
        [Range(-100f, 100f)] public float tint;
    }

    [System.Serializable]
    public struct SplitToningSettings
    {
        [ColorUsage(false)] public Color shadows;
        [ColorUsage(false)] public Color highlights;
        [Range(-100f, 100f)] public float balance;
    }

    [System.Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red;
        public Vector3 green;
        public Vector3 blue;
    }

    [System.Serializable]
    public struct ShadowsMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)] public Color shadows;
        [ColorUsage(false, true)] public Color midtones;
        [ColorUsage(false, true)] public Color highlights;

        [Range(0f, 2f)] public float shadowsStart;
        [Range(0f, 2f)] public float shadowsEnd;
        [Range(0f, 2f)] public float highlightsStart;
        [Range(0f, 2f)] public float highLightsEnd;
    }

    [field: SerializeField]
    public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights { get; private set; } = new()
    {
        shadows = Color.white,
        midtones = Color.white,
        highlights = Color.white,
        shadowsEnd = 0.3f,
        highlightsStart = 0.55f,
        highLightsEnd = 1f
    };

    [field: SerializeField]
    public ChannelMixerSettings ChannelMixer { get; private set; } = new()
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward
    };

    [field: SerializeField]
    public SplitToningSettings SplitToning { get; private set; } = new()
    {
        shadows = Color.gray,
        highlights = Color.gray
    };

    [field: SerializeField] public WhiteBalanceSettings WhiteBalance { get; private set; }

    [field: SerializeField]
    public ColorAdjustmentsSettings ColorAdjustments { get; private set; } = new()
    {
        colorFilter = Color.white
    };

    [field: SerializeField] public ToneMappingSettings ToneMapping { get; private set; }

    [field: SerializeField]
    public BloomSettings Bloom { get; private set; } = new()
    {
        scatter = 0.7f
    };

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