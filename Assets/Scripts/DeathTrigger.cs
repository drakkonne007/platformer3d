using UnityEngine;

public class DeathTrigger : TriggerAction
{
    public override void Action(Collider collider)
    {
        if (collider.transform.root.CompareTag("Player"))
        {
            MainHandler.Instance.KillPlayer();
        }
    }
}
