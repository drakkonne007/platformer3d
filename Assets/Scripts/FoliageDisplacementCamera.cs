using UnityEngine;

[ExecuteAlways]
public class FoliageDisplacementCamera : MonoBehaviour
{
    [Header("Settings")]
    public RenderTexture displacementTexture;
    public float orthographicSize = 10f;
    public LayerMask interactionLayer = 1 << 31; // Layer 31: Interaction
    
    [Header("Shader Properties")]
    public string textureName = "_FoliageDisplacementMap";
    public string posName = "_FoliageDisplacementPos"; // Center and Size
    
    private Camera _cam;

    private void OnEnable()
    {
        SetupCamera();
    }

    private void Start()
    {
        SetupCamera();
        HideLayerFromMainCamera();
    }

    private void HideLayerFromMainCamera()
    {
        Camera main = Camera.main;
        if (main != null)
        {
            // Remove the interaction layer from the main camera's culling mask
            main.cullingMask &= ~(interactionLayer);
            Debug.Log($"[FoliageSystem] Hidden {LayerMask.LayerToName(31)} layer from Main Camera.");
        }
    }

    private void Update()
    {
        if (_cam == null) SetupCamera();
        
        // Follow the parent horizontally but stay HIGH in the sky looking down
        Vector3 followPos = transform.position;
        _cam.transform.position = new Vector3(followPos.x, followPos.y + 50f, followPos.z);
        _cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        _cam.orthographicSize = orthographicSize;
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = 100f;
        
        // Update Global Shader Variables
        Shader.SetGlobalTexture(textureName, displacementTexture);
        
        // Pass World Center (XYZ) and Ortho Size (W)
        Vector4 posBuffer = new Vector4(followPos.x, followPos.y, followPos.z, orthographicSize);
        Shader.SetGlobalVector(posName, posBuffer);
    }

    private void SetupCamera()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null)
        {
            GameObject camObj = new GameObject("FoliageDisplacementCamera");
            camObj.transform.SetParent(transform);
            _cam = camObj.AddComponent<Camera>();
        }

        _cam.orthographic = true;
        _cam.clearFlags = CameraClearFlags.Color;
        _cam.backgroundColor = Color.black;
        _cam.cullingMask = interactionLayer;
        _cam.targetTexture = displacementTexture;
        _cam.allowHDR = false;
        _cam.allowMSAA = false;
    }
}
