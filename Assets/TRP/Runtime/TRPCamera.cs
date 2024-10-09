using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode source;
        public BlendMode destination;
    }

    public FinalBlendMode finalBlendMode = new() { source = BlendMode.One, destination = BlendMode.Zero };

    public PostProcessSettings postProcessSettingsOverride;
    public bool OverridesPostProcess => postProcessSettingsOverride != null;
}

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class TRPCamera : MonoBehaviour
{
    [field: SerializeField] public CameraSettings Settings { get; private set; } = new();
}