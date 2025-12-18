using KinematicCharacterController.Examples;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Fan : TriggerAction
{
    ExampleCharacterController controller;
    [SerializeField] float speed = 10;
    private void Start()
    {
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<ExampleCharacterController>();
    }
    public override void Action() 
    {
        if (controller.Gravity.y < 1)
        {
            controller.Gravity.y *= -1;
        }
    }
    public override void ExitAction()
    {
        if (controller.Gravity.y > 1)
        {
            controller.Gravity.y *= -1;
        }
    }
}
