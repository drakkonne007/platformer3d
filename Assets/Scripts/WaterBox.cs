using UnityEngine;
using System.Collections.Generic;

public class WaterBox : MonoBehaviour
{
    public static List<WaterBox> AllVolumes = new List<WaterBox>();

    void OnEnable()
    {
        if (!AllVolumes.Contains(this))
            AllVolumes.Add(this);
    }

    void OnDisable()
    {
        AllVolumes.Remove(this);
    }
}

