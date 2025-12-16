using UnityEngine;

public class ReduceHealth : TriggerAction
{
    public override void Action() {
        MainHandler.Instance.addHealth(-Time.deltaTime * 10);
    }
}
