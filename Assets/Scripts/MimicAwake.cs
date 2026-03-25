using UnityEngine;
using GiantAI; // Подключаем пространство имен GiantAI

// Атрибут вешается на класс, а не на переменные
[RequireComponent(typeof(Animator))]
public class MimicAwake : MonoBehaviour
{
    private Animator animator;
    private GiantAI.GiantAI enemyAi; // Ссылка на компонент GiantAI

    [SerializeField] private ColliderStarter starter;
    [SerializeField] private string mimicLayerName = "MimicLayer"; // Чтобы не угадывать индекс слоя

    bool letsGo = false;
    float weight = 1;
    int layerIndex = -1;
    void Start()
    {
        animator = GetComponent<Animator>();
        enemyAi = GetComponent<GiantAI.GiantAI>();
        enemyAi.enabled = false;
        starter.OnEnter += AwakeEnemy;
    }

    void AwakeEnemy(Collider other)
    {
        // Нам нужно выключить слой МИМИКА (с весом 1.0), а не базовый (0)!
        layerIndex = animator.GetLayerIndex(mimicLayerName);
        starter.OnEnter -= AwakeEnemy;
        enemyAi.enabled = true;
        enemyAi.wasSeen = true;
        enemyAi.wasHit = true;
        enemyAi.CallNearEnemies();
        letsGo = true;
    }

    private void Update()
    {
        if (letsGo)
        {
            weight -= Time.deltaTime * 2;
            weight = Mathf.Clamp(weight, 0, 1);
            animator.SetLayerWeight(layerIndex, weight);
            if(weight == 0)
            {              
                Destroy(this);
            }
        }
    }

    private void OnDestroy()
    {
        starter.OnEnter -= AwakeEnemy;
    }
}
