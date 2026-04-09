using System.Collections.Generic;
using UnityEngine;

public class BossBigHand : MonoBehaviour
{
    [SerializeField] List<BigHand> hands = new();
    [SerializeField] ColliderStarter playerChecker;
    [SerializeField] ColliderStarter playerWeapon;
    [SerializeField] float attackSpeedReaction = 2;
    [SerializeField] int handAttackCount = 1;
    [SerializeField] float health = 10; 

    int nextHandIndex_ = 0;
    bool isPlayer_ = false;
    float currentTime_ = 0;

    void Start()
    {
        playerChecker.OnEnter += addPlayer;
        playerChecker.OnExit += removePlayer;
        playerWeapon.OnEnter += hitMyself;
    }

    void hitMyself(Collider other)
    {
        health--;
    }

        void addPlayer(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            isPlayer_ = true;
        }
    }

    void removePlayer(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            isPlayer_ = false;
        }
    }

    private void OnDestroy()
    {
        if (playerChecker != null)
        {
            playerChecker.OnEnter -= addPlayer;
            playerChecker.OnExit -= removePlayer;
        }
        if (playerWeapon != null)
        {
            playerWeapon.OnEnter -= hitMyself;
        }
    }

        void Update()
    {
        if (isPlayer_ && health > 0)
        {
            if (currentTime_ <= 0)
            {
                currentTime_ = attackSpeedReaction;
                ExecuteAttackWave();
            }
        }
        
        if (currentTime_ > 0)
        {
            currentTime_ -= Time.deltaTime;
        }
    }

    void ExecuteAttackWave()
    {
        int attackCount = 0;
        int totalHandsChecked = 0;

        // Пытаемся запустить handAttackCount рук
        while (attackCount < handAttackCount && totalHandsChecked < hands.Count)
        {
            // Проверка на корректность индекса
            if (nextHandIndex_ >= hands.Count) nextHandIndex_ = 0;

            if (hands[nextHandIndex_].IsIdle())
            {
                hands[nextHandIndex_].StartHit();
                attackCount++;
            }

            // Переходим к следующей руке для следующей итерации или волны
            nextHandIndex_ = (nextHandIndex_ + 1) % hands.Count;
            totalHandsChecked++;
        }
    }
}
