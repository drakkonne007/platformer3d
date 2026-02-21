using KinematicCharacterController.Examples;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ReduceHealth : TriggerAction
{
    [SerializeField] DamageType damageType = DamageType.Phys;
    [SerializeField] float damage = 1;
    public override void Action(Collider other) {
        try
        {
            var obj = other.GetComponentsInParent<ExampleCharacterController>();
            MainHandler.Instance.addHealth(-damage, damageType);
        }
        catch
        {

        }        
    }

    public override void ActionStay(Collider other, float time) {
        try
        {
            var obj = other.GetComponentsInParent<ExampleCharacterController>();
            MainHandler.Instance.addHealth(-damage * time, damageType);
        }
        catch
        {

        }
    }
}
