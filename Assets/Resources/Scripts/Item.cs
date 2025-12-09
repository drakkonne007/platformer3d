using UnityEngine;

public class Item : MonoBehaviour
{
    virtual public void getEffect(Collider2D other){}

    void startTrigger(Collider2D other)
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
