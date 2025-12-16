using UnityEngine;

public class Item : MonoBehaviour
{
    virtual public void getEffect(Collider other){}

    void startTrigger(Collider other)
    {
        getEffect(other);
        Destroy(gameObject);
    }

    ColliderStarter starter;
    private void Awake()
    {
        starter = GetComponent<ColliderStarter>();
        starter.OnEnter += startTrigger;
    }
}
