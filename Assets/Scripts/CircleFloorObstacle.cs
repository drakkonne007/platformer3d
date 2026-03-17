using KinematicCharacterController.Examples;
using UnityEngine;

public class CircleFloorObstacle : MonoBehaviour
{
    [SerializeField] GameObject colliderClockWise;
    [SerializeField] GameObject colliderContrClockWise;
    [SerializeField] float power = 10;
    [SerializeField] bool clockWise = true;
    [SerializeField] float speedRotate = 100;
    [SerializeField] bool isActive = true;
    GameObject parentPower;

    void Start()
    {
        parentPower = transform.root.Find("ParentPower").gameObject;
        colliderClockWise.GetComponent<ColliderStarter>().OnEnter += hitPlayer;
        colliderContrClockWise.GetComponent<ColliderStarter>().OnEnter += hitPlayer;
        ChangeActivity(isActive);
    }

    public void ChangeActivity(bool need)
    {
        isActive = need;
        if (need)
        {
            if (clockWise)
            {
                colliderContrClockWise.GetComponent<Collider>().enabled = false;
                colliderClockWise.GetComponent<Collider>().enabled = true;
            }
            else
            {
                colliderClockWise.GetComponent<Collider>().enabled = false;
                colliderContrClockWise.GetComponent<Collider>().enabled = true;
            }
        }
        else
        {
            colliderContrClockWise.GetComponent<Collider>().enabled = false;
            colliderClockWise.GetComponent<Collider>().enabled = false;
        }
    }
    private void OnDestroy()
    {
        colliderClockWise.GetComponent<ColliderStarter>().OnEnter -= hitPlayer;
        colliderContrClockWise.GetComponent<ColliderStarter>().OnEnter -= hitPlayer;
    }
    void hitPlayer(Collider other)
    {
        if (other.transform.root.CompareTag("Player"))
        {
            var controller = other.transform.root.GetComponent<ExampleCharacterController>();
            if (controller != null)
            {
                Vector3 pushDirection = other.transform.position - parentPower.transform.position;
                pushDirection.y = 0;
                controller.SetVelocity(pushDirection.normalized * power);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (isActive)
        {
            transform.Rotate((clockWise ? Vector3.up : Vector3.down) * (speedRotate * Time.deltaTime));
        }
    }
}
