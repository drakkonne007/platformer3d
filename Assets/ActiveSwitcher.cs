using System.Collections.Generic;
using UnityEngine;

public class ActiveSwitcher : MonoBehaviour
{
    [SerializeField] List<GameObject> objects = new();
    public void Enable()
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(true);
        }
    }
    public void Disable()
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }
    }

}
