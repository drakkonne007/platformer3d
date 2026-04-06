using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PerformanceAnalyzer : MonoBehaviour
{
    [Header("Настройки анализа")]
    public bool analyzeOnStart = true;
    public int topResultsCount = 10;

    private void Start()
    {
        if (analyzeOnStart)
        {
            AnalyzeScene();
        }
    }

    [ContextMenu("Analyze Scene")]
    public void AnalyzeScene()
    {
        Debug.Log("<color=cyan>--- НАЧАЛО АНАЛИЗА ПРОИЗВОДИТЕЛЬНОСТИ ---</color>");

        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        
        var groups = renderers
            .GroupBy(r => new { 
                mesh = r.GetComponent<MeshFilter>()?.sharedMesh, 
                mat = r.sharedMaterial,
                shadows = r.shadowCastingMode
            })
            .Select(g => new {
                MeshName = g.Key.mesh ? g.Key.mesh.name : "Missing Mesh",
                MaterialName = g.Key.mat ? g.Key.mat.name : "Missing Material",
                HasInstancing = g.Key.mat != null && g.Key.mat.enableInstancing,
                ShadowMode = g.Key.shadows,
                Count = g.Count(),
                TotalTris = (g.Key.mesh ? g.Key.mesh.triangles.Length / 3 : 0) * g.Count()
            })
            .OrderByDescending(g => g.Count)
            .Take(topResultsCount)
            .ToList();

        Debug.Log($"<color=white>Найдено всего MeshRenderers: {renderers.Length}</color>");

        foreach (var group in groups)
        {
            string instancingStatus = group.HasInstancing ? "<color=green>ВКЛЮЧЕН</color>" : "<color=red>ВЫКЛЮЧЕН</color>";
            string shadowStatus = group.ShadowMode != UnityEngine.Rendering.ShadowCastingMode.Off ? "<color=orange>ДА</color>" : "НЕТ";

            Debug.Log($"<b>Объект:</b> {group.MeshName} | <b>Мат:</b> {group.MaterialName}\n" +
                      $"Количество: <b>{group.Count}</b> | Инстансинг: {instancingStatus} | Тени: {shadowStatus}\n" +
                      $"Примерные треугольники: {group.TotalTris:N0}");
            
            if (group.Count > 50 && !group.HasInstancing)
            {
                Debug.LogWarning($"<color=yellow>ВНИМАНИЕ:</color> Объект {group.MeshName} встречается {group.Count} раз, но <b>GPU Instancing</b> выключен! Это ломает батчинг.");
            }
        }

        CheckCommonBatchBreakers(renderers);

        Debug.Log("<color=cyan>--- АНАЛИЗ ЗАВЕРШЕН ---</color>");
    }

    private void CheckCommonBatchBreakers(MeshRenderer[] renderers)
    {
        int lightmapCount = renderers.Count(r => r.lightmapIndex >= 0);
        if (lightmapCount > 0)
        {
            Debug.Log($"<color=lightblue>Инфо:</color> {lightmapCount} объектов используют Lightmaps. Разные лайтмапы мешают батчингу (кроме Static Batching).");
        }

        int motionVectorCount = renderers.Count(r => r.motionVectorGenerationMode != MotionVectorGenerationMode.Camera);
        if (motionVectorCount > 0)
        {
             Debug.Log($"<color=lightblue>Инфо:</color> {motionVectorCount} объектов используют кастомные Motion Vectors (может мешать некоторым видам оптимизации).");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PerformanceAnalyzer))]
public class PerformanceAnalyzerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PerformanceAnalyzer analyzer = (PerformanceAnalyzer)target;
        if (GUILayout.Button("Запустить анализ прямо сейчас (Analyze Scene)"))
        {
            analyzer.AnalyzeScene();
        }
    }
}
#endif
