using UnityEngine;

public class FoliageInteractor : MonoBehaviour
{
    [Header("Settings")]
    public float interactionSize = 2f;
    public LayerMask interactionLayer = 1 << 31; // Layer 31: Interaction
    
    private GameObject _marker;

    void Start()
    {
        // Create a dedicated interaction marker child
        _marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _marker.name = "FoliageInteractionMarker";
        _marker.transform.SetParent(transform);
        _marker.transform.localPosition = Vector3.zero;
        _marker.transform.localScale = Vector3.one * interactionSize;
        
        // Remove collision, keep only Renderer
        Destroy(_marker.GetComponent<SphereCollider>());
        
        // Move to the interaction layer
        int targetLayer = GetLayerFromMask(interactionLayer);
        _marker.layer = targetLayer;
        
        // Set all children (if any) to the same layer
        foreach (Transform child in _marker.transform) {
            child.gameObject.layer = targetLayer;
        }

        // Use a simple unlit white material
        Renderer rend = _marker.GetComponent<Renderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = Color.white;
        rend.sharedMaterial = mat;
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int bitmask = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if (((bitmask >> i) & 1) == 1) return i;
        }
        return 0;
    }

    private void Update()
    {
        // Maintain local scale in case parent scale changes
        _marker.transform.localScale = Vector3.one * interactionSize;
    }
}
