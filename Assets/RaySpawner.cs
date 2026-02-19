using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpawner : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] SpawnMode spawnMode = SpawnMode.Circle;

    [Header("Circle / Spin Logic")]
    [SerializeField] int angleCount = 1;
    [SerializeField] bool spin = false;
    [SerializeField] float maxAngle = 90f;

    [Header("Beam Settings")]
    [SerializeField] LineRenderer rayPrefab; // Visual for the ray
    [SerializeField] float activeDuration = 2.0f; // How long it shoots (seconds)
    [SerializeField] float period = 4.0f; // Time between shooting cycles
    [SerializeField] float rayRadius = 0.5f; // Radius for SphereCast
    [SerializeField] float maxDistance = 50f;
    [SerializeField] bool ignoreHeight = false;
    [SerializeField] LayerMask hitLayers = ~0; // Default to Everything

    private Transform player;
    private float timer = 0;
    private bool isFiring = false;

    void Start()
    {
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
        // Timer logic
        timer += Time.deltaTime;
        if (timer >= period && !isFiring)
        {
            StartCoroutine(FireRoutine());
        }
    }

    IEnumerator FireRoutine()
    {
        isFiring = true;
        timer = 0; // Reset main timer

        // 1. Initial Setup & Rotation
        Quaternion startRotation = transform.rotation;
        
        // Aim at player initially if possible, similar to BulletSpawner logic
        if (player != null)
        {
            Vector3 dirToPlayer = GetDirectionToPlayer();
            // In BulletSpawner, distinct logic for Sniper vs Circle. 
            // Circle with Spin aligns to player first.
            if (spawnMode == SpawnMode.Circle && spin)
            {
                transform.forward = dirToPlayer;
                startRotation = transform.rotation;
            }
            else if (spawnMode == SpawnMode.Sniper)
            {
                transform.forward = dirToPlayer;
                startRotation = transform.rotation;
            }
            // If Circle and no spin, we just keep current rotation? 
            // BulletSpawner just spawns angles based on current valid transform.forward.
        }

        // 2. Instantiate Visuals (LineRenderers)
        List<LineRenderer> beams = new List<LineRenderer>();
        int raysToSpawn = (spawnMode == SpawnMode.Circle) ? angleCount : 1;
        
        for (int i = 0; i < raysToSpawn; i++)
        {
            if (rayPrefab != null)
            {
                // Prevent recursive spawning: Don't spawn if the prefab is THIS object or has this component
                // To be safe, we check if the prefab has a RaySpawner component which would cause infinite loops
                if (rayPrefab.gameObject == this.gameObject)
                {
                    Debug.LogError("CRITICAL: RayPrefab cannot be the RaySpawner object itself! Infinite loop prevented.");
                    yield break; 
                }

                LineRenderer lr = Instantiate(rayPrefab, transform.position, Quaternion.identity, transform);
                
                // Extra safety: Verify the spawned object doesn't have a RaySpawner, or disable it
                if (lr.TryGetComponent<RaySpawner>(out var recursiveSpawner))
                {
                    Debug.LogWarning("Spawned ray has a RaySpawner component. Defaulting to destroying the component to prevent infinite loop.");
                    Destroy(recursiveSpawner);
                }

                beams.Add(lr);
            }
        }

        // 3. Loop for Duration
        float elapsedTime = 0;
        while (elapsedTime < activeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / activeDuration);

            // Handle Rotation (Spinning)
            if (spawnMode == SpawnMode.Circle && spin)
            {
                // Rotate over time from 0 to maxAngle
                float currentAngle = progress * maxAngle;
                transform.rotation = startRotation * Quaternion.Euler(0, currentAngle, 0);
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
                UpdateRay(beams[i], transform.position, rayDir);
            }

            yield return null;
        }

        // 4. Cleanup
        foreach (var lr in beams)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        
        isFiring = false;
    }

    void UpdateRay(LineRenderer lr, Vector3 origin, Vector3 direction)
    {
        if (lr == null) return;

        lr.positionCount = 2;
        lr.SetPosition(0, origin);

        Vector3 endPos = origin + direction * maxDistance;

        // Use SphereCast for radius collision
        RaycastHit hit;
        // SphereCastOrigin should be slightly backed up or handled carefully
        // Unity SphereCast sweeps a sphere along the ray.
        if (Physics.SphereCast(origin, rayRadius, direction, out hit, maxDistance, hitLayers))
        {
            endPos = hit.point; // Visual stops at hit

            // Check for Player
            // If the collider is the player or child of player logic
            if (hit.collider.CompareTag("Player") || (player != null && hit.collider.transform.root == player.root))
            {
                 Debug.Log("врезался");
            }
        }

        lr.SetPosition(1, endPos);
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
}
