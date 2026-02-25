using UnityEngine;

public class Item : MonoBehaviour
{
    virtual public void getEffect(Collider other){}
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
