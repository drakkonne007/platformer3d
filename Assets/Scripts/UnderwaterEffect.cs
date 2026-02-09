using UnityEngine;
using System.Collections.Generic;

public class UnderwaterEffect : MonoBehaviour
{
    [Header("Settings")]
    public Color UnderwaterColor = new Color(0, 0.4f, 0.7f, 1);
    public float UnderwaterFogDensity = 0.15f;
    public float TransitionSpeed = 5f;

    [Header("Post-Processing Shader")]
    public Material DistortionMaterial;
    public float DistortionStrength = 0.02f;
    public float DistortionSpeed = 2f;
    public bool ShowDebugMask = false;

    [Header("References")]
    public Camera TargetCamera;

    private bool _isUnderwater;
    private Color _originalBackgroundColor;
    private Color _originalFogColor;
    private float _originalFogDensity;
    private bool _originalFogEnabled;
    private FogMode _originalFogMode;

    private float _lerpFactor;

    private static readonly int _propIntensity = Shader.PropertyToID("_Intensity");
    private static readonly int _propDistortionStrength = Shader.PropertyToID("_DistortionStrength");
    private static readonly int _propDistortionSpeed = Shader.PropertyToID("_DistortionSpeed");
    private static readonly int _propTintColor = Shader.PropertyToID("_TintColor");
    private static readonly int _propWaterVolumeCount = Shader.PropertyToID("_WaterVolumeCount");
    private static readonly int _propWaterMatrices = Shader.PropertyToID("_WaterMatrices");
    private static readonly int _propInvVP = Shader.PropertyToID("_InvVP");
    private static readonly int _propShowDebugMask = Shader.PropertyToID("_ShowDebugMask");

    private Matrix4x4[] _matrices = new Matrix4x4[8];

    void Start()
    {
        if (TargetCamera == null) TargetCamera = GetComponent<Camera>();

        Debug.Log($"UnderwaterEffect started. PC_RPAsset settings MUST have Depth Texture enabled!");

        // Store original settings
        _originalBackgroundColor = TargetCamera.backgroundColor;
        _originalFogColor = RenderSettings.fogColor;
        _originalFogDensity = RenderSettings.fogDensity;
        _originalFogEnabled = RenderSettings.fog;
        _originalFogMode = RenderSettings.fogMode; 

        if (DistortionMaterial)
        {
            DistortionMaterial.SetFloat(_propIntensity, 0f);
        }
    }

    void Update()
    {
        if (TargetCamera == null) return;

        // Check if camera is inside ANY volume
        bool currentlyUnderwater = false;
        Vector3 camPos = TargetCamera.transform.position;
        
        foreach (var volume in WaterBox.AllVolumes)
        {
            if (volume == null) continue;
            Vector3 localPos = volume.transform.worldToLocalMatrix.MultiplyPoint(camPos);
            // Inside unit box check
            if (Mathf.Abs(localPos.x) <= 0.5f && Mathf.Abs(localPos.y) <= 0.5f && Mathf.Abs(localPos.z) <= 0.5f)
            {
                currentlyUnderwater = true;
                break;
            }
        }

        if (currentlyUnderwater != _isUnderwater)
        {
            _isUnderwater = currentlyUnderwater;
            // Debug.Log($"Camera submerged state changed: {currentlyUnderwater}");
        }

        // Smoothly transition between states
        _lerpFactor = Mathf.MoveTowards(_lerpFactor, _isUnderwater ? 1f : 0f, Time.deltaTime * TransitionSpeed);

        ApplyEffect(_lerpFactor);
    }

    private void ApplyEffect(float factor)
    {
        if (factor > 0.01f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(_originalFogColor, UnderwaterColor, factor);
            RenderSettings.fogDensity = Mathf.Lerp(_originalFogDensity, UnderwaterFogDensity, factor);
            TargetCamera.backgroundColor = Color.Lerp(_originalBackgroundColor, UnderwaterColor, factor);
        }
        else
        {
            RenderSettings.fog = _originalFogEnabled;
            RenderSettings.fogMode = _originalFogMode;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;
            TargetCamera.backgroundColor = _originalBackgroundColor;
        }

        if (DistortionMaterial)
        {
            DistortionMaterial.SetFloat(_propIntensity, 1f); 
            DistortionMaterial.SetFloat(_propDistortionStrength, DistortionStrength);
            DistortionMaterial.SetFloat(_propDistortionSpeed, DistortionSpeed);
            DistortionMaterial.SetColor(_propTintColor, UnderwaterColor);
            DistortionMaterial.SetInt(_propShowDebugMask, ShowDebugMask ? 1 : 0);

            // Manual reconstruction matrix (VP inverse)
            // Sometimes true is needed for blit, sometimes false depends on URP's current state.
            // Let's try false again but with corrected NDC logic in shader.
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(TargetCamera.projectionMatrix, false);
            Matrix4x4 view = TargetCamera.worldToCameraMatrix;
            Matrix4x4 invVP = (proj * view).inverse;
            DistortionMaterial.SetMatrix(_propInvVP, invVP);

            // Send all volume matrices to shader
            int count = Mathf.Min(WaterBox.AllVolumes.Count, 8);
            for (int i = 0; i < count; i++)
            {
                _matrices[i] = WaterBox.AllVolumes[i].transform.worldToLocalMatrix;
            }
            
            DistortionMaterial.SetInt(_propWaterVolumeCount, count);
            DistortionMaterial.SetMatrixArray(_propWaterMatrices, _matrices);
        }
    }

    private void OnDisable()
    {
        // Restore settings when script/object is disabled
        RenderSettings.fog = _originalFogEnabled;
        RenderSettings.fogColor = _originalFogColor;
        RenderSettings.fogDensity = _originalFogDensity;
        if (TargetCamera) TargetCamera.backgroundColor = _originalBackgroundColor;

        if (DistortionMaterial)
        {
            DistortionMaterial.SetFloat(_propIntensity, 0f);
        }
    }
}
