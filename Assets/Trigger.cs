using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] List<TriggerAction> action;
    [SerializeField] bool isOnce = true;
    [SerializeField] bool isConitues = false;
    [SerializeField] bool isActive = true;
    [SerializeField] bool isDeleteAfter = false;
    bool isTriggered = false;

    private void OnTriggerEnter(Collider other) => Enter(other);
    private void OnTriggerStay(Collider other) => Stay(other);
    private void OnCollisionEnter(Collision other) => Enter(other.collider);
    private void OnCollisionStay(Collision other) => Stay(other.collider);

    bool Check()
    {
        if(action == null)
        {
            Debug.LogWarning("NO ACTION IN TRIGGER");
            return false;
        }
        if (!isActive)
        {
            return false;
        }
        if (isOnce && isTriggered)
        {
            return false;
        }
        isTriggered = true;
        return true;
    }
    void Enter(Collider other)
    {
        if (!Check())
        {
            return;
        }
        action.Action();
        if (isDeleteAfter)
        {
            Destroy(gameObject);
        }
    }
    void Stay(Collider other)
    {
        if (!Check() || !isConitues)
        {
            return;
        }
        action.Action();
        if (isDeleteAfter)
        {
            Destroy(gameObject);
        }
    }
}
