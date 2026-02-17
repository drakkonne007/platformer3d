using UnityEngine;

public class TriggerAction : MonoBehaviour
{
    public virtual void Action(Collider other) { }
    public virtual void ExitAction(Collider other) { }
}
