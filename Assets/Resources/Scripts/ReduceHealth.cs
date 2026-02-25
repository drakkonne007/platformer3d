using KinematicCharacterController.Examples;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ReduceHealth : TriggerAction
{
    [SerializeField] DamageType damageType = DamageType.Phys;
    [SerializeField] float damage = 1;
    public override void Action(Collider other)
    {
        var obj = other.GetComponentsInParent<ExampleCharacterController>();
        if (obj != null)
        {
            MainHandler.Instance.addHealth(-damage, damageType);
        }
    }

    public override void ActionStay(Collider other, float time)
    {
        var obj = other.GetComponentsInParent<ExampleCharacterController>();
        if (obj != null)
        {
            MainHandler.Instance.addHealth(-damage * time, damageType);
        }

    }
}
