using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] List<TriggerAction> actions;
    [SerializeField] bool isOnce = true;
    [SerializeField] bool isConitues = false;
    [SerializeField] bool isActive = true;
    [SerializeField] bool isDeleteAfter = false;
    bool isTriggered = false;

    private void OnTriggerExit(Collider other) => Exit(other);
    private void OnTriggerEnter(Collider other) => Enter(other);
    private void OnTriggerStay(Collider other) => Stay(other);
    private void OnCollisionExit(Collision other) => Exit(other.collider);
    private void OnCollisionEnter(Collision other) => Enter(other.collider);
    private void OnCollisionStay(Collision other) => Stay(other.collider);

    bool Check()
    {
        if (actions == null || actions.Count == 0)
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
        foreach (var action in actions)
        {
            action.Action();
        }
        if (isDeleteAfter)
        {
            Destroy(gameObject);
        }
    }
    void Exit(Collider other)
    {
        if (!Check() || !isConitues)
        {
            return;
        }
        foreach (var action in actions)
        {
            action.ExitAction();
        }
    }
    void Stay(Collider other)
    {
        if (!Check() || !isConitues)
        {
            return;
        }
        foreach (var action in actions)
        {
            action.Action();
        }
        if (isDeleteAfter)
        {
            Destroy(gameObject);
        }
    }
}
