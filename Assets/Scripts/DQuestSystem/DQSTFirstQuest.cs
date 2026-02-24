using UnityEngine;

public class DQSTFirstQuest : DQuestTriggerParent
{
    [SerializeField] ActionParent gate;
    protected override void Start()
    {
        base.Start();
        callbacks[97] = () => { gate.doAction(); };
    }
}
