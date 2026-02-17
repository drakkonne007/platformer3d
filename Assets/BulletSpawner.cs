using System.Collections.Generic;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> bullets;
    [SerializeField] float period = 2;
    [SerializeField] int bulletCount = 3;
    [SerializeField] float microTick = 0.5f;
    [SerializeField] float bulletSpeed = 1f;
    [SerializeField] bool ignoreHeight = false;
    int currentCount = 0;
    float lastAction = 0;
    float lastMicroTick = 0;
    Transform player;
    int bulletIndex = 0;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        lastAction = period;
    }

    // Update is called once per frame
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
                }
            }
        }
    }

    void Shoot()
    {
        if (bullets == null || bullets.Count == 0 || player == null) return;

        GameObject prefab = bullets[bulletIndex];
        bulletIndex = (bulletIndex + 1) % bullets.Count;

        GameObject bullet = Instantiate(prefab, transform.position, Quaternion.identity);
        
        Vector3 targetPos = player.position;
        if (ignoreHeight)
        {
            targetPos.y = transform.position.y;
        }
        else
        {
            targetPos.y += 1;
        }

            Vector3 direction = (targetPos - transform.position).normalized;

        // Apply velocity if Rigidbody exists
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = direction * bulletSpeed;
        }

        // Align bullet to look at player
        bullet.transform.forward = direction;
    }
}
