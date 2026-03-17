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
    float refresh = 0.5f;

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
        if (refresh >= 0.5f && other.transform.root.CompareTag("Player"))
        {
            var controller = other.transform.root.GetComponent<ExampleCharacterController>();
            if (controller != null)
            {
                Vector3 pushDirection = other.transform.position - parentPower.transform.position;
                pushDirection.y = 0;
                controller.AddVelocity(pushDirection.normalized * power);
                refresh = 0;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        refresh += Time.deltaTime;
        if (isActive)
        {
            transform.Rotate((clockWise ? Vector3.up : Vector3.down) * (speedRotate * Time.deltaTime));
        }
    }
}
