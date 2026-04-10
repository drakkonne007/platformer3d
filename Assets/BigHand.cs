using DG.Tweening;
using KinematicCharacterController.Examples;
using UnityEngine;

public enum HandState
{
    Ground,
    Idle,
    ToGround,
    ToIdle 
}

public class BigHand : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float groundTime = 2;
    [SerializeField] float damage = 1;
    [SerializeField] GameObject weaponHitbox;
    [SerializeField] GameObject hitbox;
    [SerializeField] Collider flyBridge;
    [SerializeField] float speedToPlayer = 25;
    [SerializeField] float speedFromPlayer = 15;
    [SerializeField] float health = 40;

    [Header("Idle Movement")]
    [SerializeField] float idleRadius = 2f;
    [SerializeField] float idleSpeed = 2f;
    [SerializeField] float idleVerticalAmpltude = 0.5f;

    Collider weapon_;
    Collider hitbox_;
    Vector3 startPos_;
    Vector3 playerPos_;
    HandState handState_ = HandState.Idle;

    float groundTimer_ = 0;
    float lastDistToTarget_ = float.MaxValue;
    const float TOLERANCE = 0.3f;
    bool playerHited_ = false;

    void Start()
    {
        if (flyBridge != null)
        {
            flyBridge.enabled = false;
        }
        weapon_ = weaponHitbox.GetComponent<Collider>();
        weapon_.GetComponent<ColliderStarter>().OnEnter += Attack;
        hitbox.GetComponent<ColliderStarter>().OnEnter += Hurt;
        hitbox_ = hitbox.GetComponent<Collider>();
        startPos_ = transform.position;
        StartIdle();
    }
    private void OnDestroy()
    {
        weapon_.GetComponent<ColliderStarter>().OnEnter -= Attack;
        hitbox.GetComponent<ColliderStarter>().OnEnter -= Hurt;
    }
    void Hurt(Collider other)
    {
        print("Hurt(((");
        health -= MainHandler.Instance.playerDamage;
        if (flyBridge != null)
        {
            flyBridge.enabled = true;
        }
    }
    void Attack(Collider other)
    {
        if (!playerHited_ && other.transform.root.CompareTag("Player"))
        {
            playerHited_ = true;
            print("Attack!!!");
            MainHandler.Instance.addHealth(-damage, DamageType.Phys);
        }
    }
    public bool IsIdle()
    {        
        return handState_ == HandState.Idle && health > 0;
    }
    public void StartHit()
    {
        if (flyBridge != null)
        {
            print("Set enbled to false");
            flyBridge.enabled = false;
        }
        if (handState_ != HandState.Idle) return;
        playerHited_ = false;

        playerPos_ = MainHandler.Instance.playerPosition();

        // Если игрок уже слишком близко (меньше погрешности), удар не начинаем
        if (Vector3.Distance(transform.position, playerPos_) < TOLERANCE)
        {
            return;
        }
        
        // Переход к атаке
        transform.DOKill();
        handState_ = HandState.ToGround;
        lastDistToTarget_ = Vector3.Distance(transform.position, playerPos_);
        
        weapon_.enabled = true;
        hitbox_.enabled = false;

        transform.DOMove(playerPos_, speedToPlayer)
            .SetSpeedBased()
            .SetEase(Ease.InQuad);
    }

    void StartIdle()
    {
        transform.DOKill();
        handState_ = HandState.Idle;
        weapon_.enabled = false;
        hitbox_.enabled = false;
    }

    void EnterGroundState()
    {
        transform.DOKill();
        handState_ = HandState.Ground;
        groundTimer_ = groundTime;
        
        weapon_.enabled = false;
        hitbox_.enabled = true;
    }

    void EnterToIdleState()
    {
        transform.DOKill();
        handState_ = HandState.ToIdle;
        lastDistToTarget_ = Vector3.Distance(transform.position, startPos_);
        
        weapon_.enabled = false;
        hitbox_.enabled = false;

        transform.DOMove(startPos_, speedFromPlayer)
            .SetSpeedBased()
            .SetEase(Ease.Linear);
    }

    private void Update()
    {
        switch (handState_)
        {
            case HandState.Idle:
                UpdateIdleMovement();
                break;

            case HandState.ToGround:
                CheckOvershoot(playerPos_, EnterGroundState);
                break;

            case HandState.Ground:
                groundTimer_ -= Time.deltaTime;
                if (groundTimer_ <= 0)
                {
                    EnterToIdleState();
                }
                break;

            case HandState.ToIdle:
                CheckOvershoot(startPos_, StartIdle);
                break;
        }
    }

    void UpdateIdleMovement()
    {
        if(health <= 0)
        {
            return;
        }
        // Плавное круговое движение вокруг точки старта
        float time = Time.time * idleSpeed;
        Vector3 targetPos = startPos_ + new Vector3(
            Mathf.Cos(time) * idleRadius,
            Mathf.Sin(time * 0.5f) * idleVerticalAmpltude,
            Mathf.Sin(time) * idleRadius
        );

        // Используем Lerp для устранения резкого скачка при переходе из ToIdle в Idle
        // Рука плавно "притянется" к своей орбите
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);
    }

    void CheckOvershoot(Vector3 target, System.Action onArrived)
    {
        float currentDist = Vector3.Distance(transform.position, target);
        
        // Если прилетели или начали удаляться (пролетели из-за FPS)
        if (currentDist < TOLERANCE || currentDist > lastDistToTarget_ + 0.05f)
        {
            onArrived?.Invoke();
        }
        
        lastDistToTarget_ = currentDist;
    }
}


