using UnityEngine;
using KinematicCharacterController;

[RequireComponent(typeof(PhysicsMover))]
public class PushTrap : MonoBehaviour, IMoverController
{
    [SerializeField] float speed = 1; 
    [SerializeField] GameObject start;
    [SerializeField] GameObject end;

    private PhysicsMover _mover;

    Vector3 positionStart = new(), positionEnd = new();
    bool isGoingToEnd = true;

    private void Awake()
    {
        _mover = GetComponent<PhysicsMover>();
        _mover.MoverController = this;
    }

    void Start()
    {
        positionStart = start.transform.position;
        positionEnd = end.transform.position;
        
        if (start != null) start.SetActive(false);
        if (end != null) end.SetActive(false);
    }

    // This is called by the PhysicsMover to determine the next position and rotation
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        goalRotation = transform.rotation;

        Vector3 target = isGoingToEnd ? positionEnd : positionStart;
        
        // Use move towards on the current position
        goalPosition = Vector3.MoveTowards(transform.position, target, speed * deltaTime);

        if (Vector3.Distance(goalPosition, target) < 0.1f)
        {
            isGoingToEnd = !isGoingToEnd;
        }
    }
}
