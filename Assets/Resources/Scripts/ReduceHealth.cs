using KinematicCharacterController.Examples;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ReduceHealth : TriggerAction
{
    public override void Action(Collider other) {
        try
        {
            var obj = other.GetComponentsInParent<ExampleCharacterController>();
            MainHandler.Instance.addHealth(-5);
        }
        catch
        {

        }        
    }
}
