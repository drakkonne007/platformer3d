using System.Collections.Generic;
using UnityEngine;
public class DefaulChestAnim : ActionParent
{
    [SerializeField] List<ItemInventarSO> loots = new();
    [SerializeField] Collider placeForLoot;
    [SerializeField] ColliderStarter objBox;
    [SerializeField] GameObject coin;
    [SerializeField] List<ItemInventarSO> neededItems = new();
    [SerializeField] Animator animator;

    [SerializeField] bool isOpen = false;


    void Start()
    {
        if (!isOpen)
        {
            doSmth = Open;
        }
        else
        {
            // Сразу переходим в состояние открытого сундука без анимации
            animator.Play("OpenedChest");
            objBox.GetComponent<Collider>().enabled = false;
        }
        objBox.OnEnter += SetActive;
        objBox.OnStay += SetActive;
        objBox.OnExit += SetDeactive;
    }
    private void OnDestroy()
    {
        objBox.OnEnter -= SetActive;
        objBox.OnExit -= SetDeactive;
    }
    void SetActive(Collider other)
    {
        MainHandler.Instance.setActiveAction(this);
    }
    void SetDeactive(Collider other)
    {
        if (MainHandler.Instance.currentActiveAction() == this)
        {
            MainHandler.Instance.setActiveAction(null);
        }
    }
    void Open()
    {
        if (neededItems.Count != 0)
        {
            var strItemIds = MainHandler.Instance.playerData.getItemIds();
            foreach (var item in neededItems)
            {
                if (!strItemIds.Contains(item))
                {
                    return;
                }
            }
        }
        
        isOpen = true;
        animator.SetBool("open", true);
        
        // Запускаем корутину, чтобы дождаться конца анимации перед спавном лута
        StartCoroutine(SpawnLootAfterAnimation());
        
        objBox.GetComponent<Collider>().enabled = false;
        SetDeactive(null);
    }

    private System.Collections.IEnumerator SpawnLootAfterAnimation()
    {
        // Даем аниматору время переключиться
        yield return new WaitForEndOfFrame();

        // Ждем пока аниматор реально перейдет в стейт открытия "OpenLikeChest"
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("IdleChest"))
        {
            yield return null;
        }

        // Ждем пока анимация открытия дойдет до конца (normalizedTime >= 1.0)
        // Если у вас есть переход в "OpenedChest", мы дождемся его. 
        // Если нет - просто дождемся конца клипа.
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("OpenLikeChest") && 
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        SpawnLoot();
    }


    void SpawnLoot()
    {
        if (placeForLoot == null) return;

        BoxCollider box = placeForLoot as BoxCollider;
        if (loots.Count == 0)
        {
            for (int i = 0; i < UnityEngine.Random.Range(5, 10); i++)
            {
                Vector3 spawnPos;
                if (box != null)
                {
                    Vector3 randomLocal = new Vector3(
                        UnityEngine.Random.Range(-box.size.x / 2f, box.size.x / 2f),
                        UnityEngine.Random.Range(-box.size.y / 2f, box.size.y / 2f),
                        UnityEngine.Random.Range(-box.size.z / 2f, box.size.z / 2f)
                    ) + box.center;

                    spawnPos = placeForLoot.transform.TransformPoint(randomLocal);
                }
                else
                {
                    // Fallback for non-box colliders
                    Bounds bounds = placeForLoot.bounds;
                    spawnPos = new Vector3(
                        UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                        UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                        UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                    );
                }

                Instantiate(coin, spawnPos, Quaternion.identity);
            }
        }
        else
        {
            foreach (var loot in loots)
            {
                if (loot == null) continue;

                Vector3 spawnPos;
                if (box != null)
                {
                    Vector3 randomLocal = new Vector3(
                        UnityEngine.Random.Range(-box.size.x / 2f, box.size.x / 2f),
                        UnityEngine.Random.Range(-box.size.y / 2f, box.size.y / 2f),
                        UnityEngine.Random.Range(-box.size.z / 2f, box.size.z / 2f)
                    ) + box.center;

                    spawnPos = placeForLoot.transform.TransformPoint(randomLocal);
                }
                else
                {
                    // Fallback for non-box colliders
                    Bounds bounds = placeForLoot.bounds;
                    spawnPos = new Vector3(
                        UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                        UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                        UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                    );
                }
                var temp = Instantiate(loot.prefab, spawnPos, Quaternion.identity);
                if (temp.GetComponent<ItemOnMap>() != null)
                {
                    temp.GetComponent<ItemOnMap>().thisPrefab = loot;
                }
            }
        }

    }
}

