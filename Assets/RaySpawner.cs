using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpawner : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] SpawnMode spawnMode = SpawnMode.Circle;

    public enum RayType { Gold, Silver, Physical }
    [Header("Ray Sequence")]
    [SerializeField] int series = 1; // how many times to fire the same type before switching

    [Header("Circle / Spin Logic")]
    [SerializeField] int angleCount = 1;
    [SerializeField] bool spin = false;
    [SerializeField] float maxAngle = 90f;

    [Header("Beam Settings")]
    [SerializeField] GameObject goldRayPrefab; // Visual for the ray
    [SerializeField] GameObject goldRayEndPrefab;
    [Space(10)]
    [SerializeField] GameObject raySilverPrefab; // Visual for the ray
    [SerializeField] GameObject raySilverEndPrefab;
    [Space(10)]
    [SerializeField] GameObject rayPhysPrefab; // Visual for the ray
    [SerializeField] GameObject rayPhysEndPrefab;
    [Space(10)]
    [SerializeField] float activeDuration = 2.0f; // How long it shoots (seconds)
    [SerializeField] float period = 4.0f; // Time between shooting cycles
    [SerializeField] float rayRadius = 0.5f; // Radius for SphereCast
    [SerializeField] float maxDistance = 50f;
    [SerializeField] bool ignoreHeight = false;
    [SerializeField] LayerMask hitLayers = ~0; // Default to Everything

    private Transform player;
    private float timer = 0;
    private bool isFiring = false;

    private int currentSeriesCount = 0;
    private int currentTypeIndex = 0;
    private List<RayType> availableTypes = new List<RayType>();

    void Start()
    {
        if (goldRayPrefab != null) availableTypes.Add(RayType.Gold);
        if (raySilverPrefab != null) availableTypes.Add(RayType.Silver);
        if (rayPhysPrefab != null) availableTypes.Add(RayType.Physical);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
        // Start with timer ready to fire
        timer = period;
    }

    void Update()
    {
        // Continuous spin if Spin is checked and we are in Circle mode
        if (spin && spawnMode == SpawnMode.Circle)
        {
            transform.Rotate(0, maxAngle * Time.deltaTime, 0);
        }

        // Timer logic runs ONLY when not actively firing
        if (!isFiring)
        {
            timer += Time.deltaTime;
            if (timer >= period)
            {
                StartCoroutine(FireRoutine());
            }
        }
    }

    IEnumerator FireRoutine()
    {
        isFiring = true;

        RayType currentRayType = RayType.Gold;
        if (availableTypes.Count > 0)
        {
            currentRayType = availableTypes[currentTypeIndex];
            currentSeriesCount++;
            if (currentSeriesCount >= series)
            {
                currentSeriesCount = 0;
                currentTypeIndex = (currentTypeIndex + 1) % availableTypes.Count;
            }
        }

        // Aim at player initially if possible, similar to BulletSpawner logic
        if (player != null)
        {
            Vector3 dirToPlayer = GetDirectionToPlayer();
            // In BulletSpawner, distinct logic for Sniper vs Circle. 
            // Circle mode NEVER aims at player now.
            if (spawnMode == SpawnMode.Sniper)
            {
                transform.forward = dirToPlayer;
            }
            // Circle mode: Use current rotation as startRotation (set above)
        }

        // 2. Instantiate Visuals (LineRenderers) and End Effects
        List<GameObject> beams = new List<GameObject>();
        List<GameObject> endEffects = new List<GameObject>();
        int raysToSpawn = (spawnMode == SpawnMode.Circle) ? angleCount : 1;

        for (int i = 0; i < raysToSpawn; i++)
        {
            GameObject currentRayPrefab = goldRayPrefab;
            GameObject currentEndPrefab = goldRayEndPrefab;

            if (currentRayType == RayType.Silver)
            {
                currentRayPrefab = raySilverPrefab;
                currentEndPrefab = raySilverEndPrefab;
            }
            else if (currentRayType == RayType.Physical)
            {
                currentRayPrefab = rayPhysPrefab;
                currentEndPrefab = rayPhysEndPrefab;
            }

            if (currentRayPrefab != null)
            {
                // Prevent recursive spawning: Don't spawn if the prefab is THIS object or has this component
                // To be safe, we check if the prefab has a RaySpawner component which would cause infinite loops
                if (currentRayPrefab.gameObject == this.gameObject)
                {
                    Debug.LogError("CRITICAL: RayPrefab cannot be the RaySpawner object itself! Infinite loop prevented.");
                    yield break;
                }

                GameObject beam = Instantiate(currentRayPrefab, transform.position, Quaternion.identity, transform);

                // Extra safety: Verify the spawned object doesn't have a RaySpawner, or disable it
                if (beam.TryGetComponent<RaySpawner>(out var recursiveSpawner))
                {
                    Debug.LogWarning("Spawned ray has a RaySpawner component. Defaulting to destroying the component to prevent infinite loop.");
                    Destroy(recursiveSpawner);
                }

                beams.Add(beam);

                // Instantiate End Effect if available
                if (currentEndPrefab != null)
                {
                    GameObject endEffect = Instantiate(currentEndPrefab, transform.position, Quaternion.identity, transform);
                    Debug.Log($"[RaySpawner] Instantiated EndEffect: {endEffect.name}");
                    PrepareEndEffect(endEffect);
                    endEffect.SetActive(false);
                    endEffects.Add(endEffect);
                }
                else
                {
                    Debug.Log("[RaySpawner] rayEndPrefab is NULL");
                    endEffects.Add(null);
                }
            }
        }

        // 3. Loop for Duration
        float elapsedTime = 0;
        while (elapsedTime < activeDuration)
        {
            elapsedTime += Time.deltaTime;

            if (spawnMode == SpawnMode.Sniper && player != null)
            {
                // Constantly track the player in Sniper mode while firing!
                transform.forward = GetDirectionToPlayer();
            }

            // Update Rays
            for (int i = 0; i < beams.Count; i++)
            {
                // Calculate direction for this specific ray
                Vector3 rayDir = transform.forward;

                if (spawnMode == SpawnMode.Circle)
                {
                    // Distribute rays around circle
                    float angleOffset = i * (360f / angleCount);
                    rayDir = Quaternion.Euler(0, angleOffset, 0) * transform.forward;
                }

                // Perform Raycast and Update Visual
                UpdateRay(beams[i], endEffects[i], transform.position, rayDir, currentRayType);
            }

            yield return null;
        }

        // 4. Cleanup
        foreach (var beam in beams)
        {
            if (beam != null) Destroy(beam);
        }
        foreach (var ef in endEffects)
        {
            if (ef != null) Destroy(ef);
        }

        // Reset timer AFTER firing so `period` is exactly the pause duration
        timer = 0;
        isFiring = false;
    }

    void UpdateRay(GameObject beam, GameObject endEffect, Vector3 origin, Vector3 direction, RayType rayType)
    {
        if (beam == null) return;

        Vector3 endPos = origin + direction * maxDistance;

        // Use SphereCastAll to hit multiple objects and filter out the player if colors match
        RaycastHit[] hits = Physics.SphereCastAll(origin, rayRadius, direction, maxDistance, hitLayers);
        
        // Sort hits by distance
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hasHit = false;
        
        foreach (var hit in hits)
        {
            bool ignoreThisHit = false;

            // Check if hit object is the player
            if (hit.collider.CompareTag("Player") || (player != null && hit.collider.transform.root == player.root))
            {
                PlayerGameLogic pgl = hit.collider.GetComponentInParent<PlayerGameLogic>();
                if (pgl == null && player != null)
                {
                    pgl = player.GetComponent<PlayerGameLogic>();
                }

                if (pgl != null)
                {
                    bool isPlayerGold = pgl.isGold();
                    
                    // Ignore if player is gold and ray is gold
                    if (isPlayerGold && rayType == RayType.Gold)
                    {
                        ignoreThisHit = true;
                    }
                    // Ignore if player is silver and ray is silver
                    else if (!isPlayerGold && rayType == RayType.Silver)
                    {
                        ignoreThisHit = true;
                    }
                }
            }

            if (!ignoreThisHit)
            {
                // This is the first valid hit that we should stop at
                hasHit = true;
                endPos = hit.point;

                if (hit.collider.CompareTag("Player") || (player != null && hit.collider.transform.root == player.root))
                {
                    Debug.Log("врезался");
                }
                
                break;
            }
        }

        // Position the beam in the center
        float distance = Vector3.Distance(origin, endPos);
        Vector3 center = (origin + endPos) / 2f;
        beam.transform.position = center;
        
        // Orient the beam (Cylinder's Y-axis points forward)
        beam.transform.up = direction;

        // Scale the beam (preserve X/Z, stretch Y)
        Vector3 newScale = beam.transform.localScale;
        newScale.y = distance / 2f; // Default Unity Cylinder is 2 units tall
        beam.transform.localScale = newScale;

        // Update End Effect
        if (endEffect != null)
        {
            if (hasHit)
            {
                if (!endEffect.activeSelf)
                {
                    endEffect.SetActive(true);
                    // Force replay of particles to be safe
                    var systems = endEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in systems) ps.Play();
                }

                endEffect.transform.position = endPos;
                endEffect.transform.localScale = Vector3.one; // Force scale
                endEffect.transform.rotation = Quaternion.LookRotation(direction);
            }
            else if (endEffect.activeSelf)
            {
                endEffect.SetActive(false);
            }
        }
    }

    Vector3 GetDirectionToPlayer()
    {
        if (player == null) return transform.forward;

        Vector3 targetPos = player.position;
        if (ignoreHeight)
        {
            targetPos.y = transform.position.y;
        }
        else
        {
            // Default offset as in BulletSpawner
            targetPos.y += 1;
        }
        return (targetPos - transform.position).normalized;
    }

    void PrepareEndEffect(GameObject effect)
    {
        if (effect == null) return;

        // Cleanup recursively
        var allTransforms = effect.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            GameObject obj = t.gameObject;

            // 1. Remove Physics (Prevent falling / collisions)
            var rbs = obj.GetComponents<Rigidbody>();
            foreach (var rb in rbs) Destroy(rb);

            var rb2ds = obj.GetComponents<Rigidbody2D>();
            foreach (var rb2d in rb2ds) Destroy(rb2d);

            var cols = obj.GetComponents<Collider>();
            foreach (var col in cols) Destroy(col);

            var col2ds = obj.GetComponents<Collider2D>();
            foreach (var col2d in col2ds) Destroy(col2d);

            // 3. Ensure ParticleSystem loops and plays
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = true;
                main.playOnAwake = true; // IMPORTANT: Ensure it plays when SetActive(true) occurs
                ps.Play();
            }
        }
    }
}
