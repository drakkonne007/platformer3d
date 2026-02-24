using System;
using System.Collections;
using UnityEngine;

public static class CoroutineUtils
{
    public static Coroutine Start(this MonoBehaviour mono, Action action, float delay = 0f)
    {
        return mono.StartCoroutine(Run(action, delay));
    }

    private static IEnumerator Run(Action action, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        action?.Invoke();
    }
}