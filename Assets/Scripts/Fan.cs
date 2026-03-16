using KinematicCharacterController.Examples;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Fan : TriggerAction
{
    ExampleCharacterController controller;
    Rigidbody playerBody;
    [SerializeField] MeshRenderer mesh;      
    
    bool isTrigger = false;
    private void Start()
    {
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<ExampleCharacterController>();
        playerBody = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        if (mesh != null)
        {
            mesh.enabled = false;
        }
    }
    public override void Action(Collider other) 
    {
        if (!isTrigger)
        {
            controller.isFly++;
            isTrigger = true;
        }
        if (controller.Gravity.y < 1)
        {
            controller.Gravity.y *= -1;
        }
        if(playerBody.linearVelocity.y < 0)
        {
            var temp = playerBody.linearVelocity;
            playerBody.linearVelocity = new Vector3(temp.x,0,temp.z);
        }
    }
    public override void ExitAction(Collider other)
    {
        if (isTrigger)
        {
            controller.isFly--;
            isTrigger = false;
        }
        if (controller.Gravity.y > 1)
        {
            controller.Gravity.y *= -1;
        }
    }
}
