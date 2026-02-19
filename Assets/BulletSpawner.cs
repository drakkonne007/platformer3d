using System.Collections.Generic;
using UnityEngine;

public enum SpawnMode
{
    Sniper,
    Circle
}
public class BulletSpawner : MonoBehaviour
{
    [Header("Circle")]
    [SerializeField] int angleCount = 2;
    [SerializeField] bool spin = false;
    [SerializeField] float maxAngle = 90f;

    [Header("Default")]
    [SerializeField] List<GameObject> bullets;
    [SerializeField] float period = 2;
    [SerializeField] int bulletCount = 3;
    [SerializeField] float microTick = 0.5f;
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField] bool ignoreHeight = false;    
    [SerializeField] SpawnMode spawnMode = SpawnMode.Circle;

    int currentCount = 0;
    float lastAction = 0;
    float lastMicroTick = 0;
    Transform player;
    int bulletIndex = 0;
    Quaternion startRotation;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastAction = period;
    }

    void Update()
    {
        lastAction += Time.deltaTime;
        lastMicroTick += Time.deltaTime;

        if (lastAction >= period)
        {
            if (currentCount < bulletCount && lastMicroTick >= microTick)
            {
                Shoot();
                currentCount++;
                lastMicroTick = 0;
                if (currentCount >= bulletCount)
                {
                    currentCount = 0;
                    lastAction = 0;
                    bulletIndex = (bulletIndex + 1) % bullets.Count;
                }
            }
        }
    }

    void Shoot()
    {
        if (bullets == null || bullets.Count == 0 || player == null) return;

        GameObject prefab = bullets[bulletIndex];
        if (spawnMode == SpawnMode.Sniper)
        {
            SpawnBullet(prefab, transform.position, GetDirectionToPlayer());
        }
        else if (spawnMode == SpawnMode.Circle)
        {
            if (spin)
            {
                if (currentCount == 0)
                {
                    // First shot: align first angle to player
                    Vector3 dir = GetDirectionToPlayer();
                    transform.forward = dir;
                    startRotation = transform.rotation;
                    SpawnBullet(prefab, transform.position, dir);
                }
                else
                {
                    // Subsequent shots: rotate and shoot from all angles
                    float currentRotationAngle = (currentCount / (float)bulletCount) * maxAngle;
                    transform.rotation = startRotation * Quaternion.Euler(0, currentRotationAngle, 0);
                    
                    for (int i = 0; i < angleCount; i++)
                    {
                        float angle = i * (360f / angleCount);
                        Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                        SpawnBullet(prefab, transform.position, direction);
                    }
                }
            }
            else
            {
                // No spin: just shoot from all angles
                for (int i = 0; i < angleCount; i++)
                {
                    float angle = i * (360f / angleCount);
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                    SpawnBullet(prefab, transform.position, direction);
                }
            }
        }
    }

    Vector3 GetDirectionToPlayer()
    {
        Vector3 targetPos = player.position;
        if (ignoreHeight)
        {
            targetPos.y = transform.position.y;
        }
        else
        {
            targetPos.y += 1; // Default offset
        }
        return (targetPos - transform.position).normalized;
    }

    void SpawnBullet(GameObject prefab, Vector3 position, Vector3 direction)
    {
        GameObject bullet = Instantiate(prefab, position, Quaternion.identity);
        Destroy(bullet, 10);

        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = direction * bulletSpeed;
        }
        bullet.transform.forward = direction;
    }
}
