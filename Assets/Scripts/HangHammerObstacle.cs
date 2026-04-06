using KinematicCharacterController.Examples;
using UnityEngine;

public class HangHammerObstacle : MonoBehaviour
{
    [SerializeField] GameObject colliderClockWise;
    [SerializeField] GameObject colliderContrClockWise;
    [SerializeField] GameObject parentPower;
    [SerializeField] float power = 150;
    [SerializeField] bool isActive = true;

    public Vector3 rotationAxis = Vector3.up;
    public float rotationAngle = 45f;
    public float duration = 2f;
    public bool useRandomDelay = false; // Toggle random delay
    public float maxRandomDelay = 1f; // Maximum random delay

    private Quaternion startRotation;
    private float timeElapsed = 0f;
    private bool isReversing = false;
    private float randomDelay = 0f;
    float refresh = 0.5f;

    void Start()
    {
        startRotation = transform.rotation;
        if (useRandomDelay)
        {
            randomDelay = Random.Range(0f, maxRandomDelay);
        }
        colliderClockWise.GetComponent<ColliderStarter>().OnEnter += hitPlayer;
        colliderContrClockWise.GetComponent<ColliderStarter>().OnEnter += hitPlayer;
        ChangeActivity(isActive);
    }

    public void ChangeActivity(bool need)
    {
        isActive = need;
        if (need)
        {
            colliderContrClockWise.GetComponent<Collider>().enabled = true;
            colliderClockWise.GetComponent<Collider>().enabled = true;
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
    void Update()
    {
        refresh += Time.deltaTime;
        if (!isActive)
        {
            return;
        }
        if (timeElapsed < randomDelay)
        {
            timeElapsed += Time.deltaTime;
            return;
        }

        float progress = (timeElapsed - randomDelay) / (duration / 2f);
        progress = Mathf.Clamp01(progress);

        progress = EaseInOut(progress);

        float currentAngle = rotationAngle * (isReversing ? (0.5f - progress) : (progress - 0.5f));
        Quaternion currentRotation = startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);

        transform.rotation = currentRotation;

        timeElapsed += Time.deltaTime;

        if (timeElapsed >= duration / 2f + randomDelay)
        {
            timeElapsed = randomDelay;
            isReversing = !isReversing;
        }
    }

    private float EaseInOut(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
}
