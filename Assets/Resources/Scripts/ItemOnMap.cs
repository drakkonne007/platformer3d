using UnityEngine;

public class ItemOnMap : MonoBehaviour
{
    public ItemInventarSO thisPrefab;
    public bool needToInvenatr = true;
    virtual public void getEffect(Collider other)
    {
        if (thisPrefab != null && needToInvenatr)
        {
            MainHandler.Instance.playerData.AddItem(thisPrefab);
        }
    }
    ColliderStarter starter;

    void startTrigger(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Magnet"))
        {
            return;
        }
        getEffect(other);
        Destroy(gameObject);
    }

    private void Awake()
    {
        starter = GetComponent<ColliderStarter>();
        starter.OnEnter += startTrigger;
    }

    private void OnDestroy()
    {
        starter.OnEnter -= startTrigger;
    }
}
