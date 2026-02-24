using System;
using UnityEngine;

public class ActionParent : MonoBehaviour
{
    public Action doSmth = null;
    public Action negativeAnswer = null;
    public Func<bool> canTriggerCallback = null;
    public void doAction()
    {
        if (!checkIfGood())
        {
            negativeAnswer?.Invoke();
        }
        else
        {
            doSmth?.Invoke();
        }
    }
    public virtual void deactivate() { }
    public bool checkIfGood()
    {
        if (canTriggerCallback != null)
        {
            return canTriggerCallback.Invoke();
        }
        return true;
    }

}
