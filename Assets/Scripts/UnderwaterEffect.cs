using UnityEngine;

[ExecuteAlways]
public class UnderwaterEffect : MonoBehaviour
{
    public Material EffectMaterial;
    public Color TintColor = new Color(0, 0.5f, 1f, 0.5f);
    public float Intensity = 1.0f;
    
    [Header("Debug")]
    public int DebugMode = 0; // 0=Off, 1=Depth, 2=Magenta(Alive)

    void Update()
    {
        if (EffectMaterial != null)
        {
            EffectMaterial.SetColor("_TintColor", TintColor);
            EffectMaterial.SetFloat("_Intensity", Intensity);
            EffectMaterial.SetInt("_DebugMode", DebugMode);
        }
    }
}
