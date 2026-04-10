using KinematicCharacterController.Examples;
using UnityEngine;

public class TeleportToStablePoint : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        ExampleCharacterController cc = other.transform.root.GetComponent<ExampleCharacterController>();
        if (cc)
        {
            cc.TeleportToLastStablePoint();
        }
    }
}
