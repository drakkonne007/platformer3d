using UnityEngine;

public class DashGhost : MonoBehaviour
{
    private float _duration;
    private float _timer;
    private MeshRenderer[] _renderers;
    private SkinnedMeshRenderer[] _skinnedRenderers;
    private Material _ghostMaterial;

    public void Init(float duration, Material ghostMaterial)
    {
        _duration = duration;
        _timer = duration;
        _ghostMaterial = ghostMaterial;

        _renderers = GetComponentsInChildren<MeshRenderer>();
        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var r in _renderers)
        {
            r.material = _ghostMaterial;
        }
        foreach (var r in _skinnedRenderers)
        {
            r.material = _ghostMaterial;
        }

        Destroy(gameObject, duration);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        float alpha = _timer / _duration;

        // Note: This requires the material to have an _Alpha or _Color property that can be adjusted.
        // If it's a standard Ghost material from KinematicCharacterController, it might just be semi-transparent already.
        // We'll just let it exist for the duration for now.
    }
}
