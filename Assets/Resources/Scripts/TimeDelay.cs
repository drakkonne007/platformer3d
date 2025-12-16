using System.Collections;
using System.Threading;
using UnityEngine;

public class TimeDelay : TriggerAction
{
    Mutex mutex;
    public override void Action()
    {
        Time.timeScale = 0.5f;
        StartCoroutine(StopDelay());
    }
    public IEnumerator StopDelay()
    {
        yield return new WaitForSeconds(1f);
        Time.timeScale = 1;
    }
}
