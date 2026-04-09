using UnityEngine;
[ExecuteInEditMode]
public class WindZoneShaderBridge : MonoBehaviour
{
    private WindZone windZone;
    [SerializeField] private float WavePulseFrequency = 0;
    void Update()
    {
        if (windZone == null)
            windZone = GetComponent<WindZone>();
        if (windZone == null) return;
        // 1. Направление ветра (forward-вектор объекта с WindZone)
        Vector4 windDir = transform.forward;
        windDir.w = windZone.windMain; // В W-компоненту запишем основную силу
        Shader.SetGlobalVector("_GlobalWindDir", windDir);
        // 2. Параметры турбулентности и пульсации
        // Рассчитываем динамический пульс (синусоида на основе параметров WindZone)
        float pulse = 0;        
        if(windZone.windMain != 0)
        {
            pulse = windZone.windMain * Mathf.Sin(Time.time * WavePulseFrequency);
            pulse /= 3;
        }
        Shader.SetGlobalFloat("_GlobalWindStrength", windZone.windMain + pulse);
        Shader.SetGlobalFloat("_GlobalWindTurbulence", windZone.windTurbulence);
    }
}
