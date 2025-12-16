using KinematicCharacterController.Examples;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;
public class SmallGravity : TriggerAction
{
    ExampleCharacterController controller;
    private void Start()
    {
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<ExampleCharacterController>();
    }
    public override void Action()
    {
        controller.Gravity.y /= 2; 
        StartCoroutine(StopGravity());
    }
    public IEnumerator StopGravity()
    {
        yield return new WaitForSeconds(5f);
        controller.Gravity.y *= 2;
    }
}

