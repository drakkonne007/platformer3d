using UnityEngine;
using KinematicCharacterController;
using UnityEngine.Rendering;

[RequireComponent(typeof(PhysicsMover))]
public class PushTrap : MonoBehaviour, IMoverController
{
    [SerializeField] float speed = 1;
    [SerializeField] bool randomDelay = false; 
    [SerializeField] bool useEasing = false;
    [SerializeField] float duration = 2f;
    [SerializeField] GameObject start;
    [SerializeField] GameObject end;

    private PhysicsMover _mover;
    private float startDuration; 

    Vector3 positionStart = new(), positionEnd = new();
    bool isGoingToEnd = true;
    private float _currentTime = 0f;

    private void Awake()
    {
        startDuration = duration;
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

        if (useEasing)
        {
            _currentTime += deltaTime;
            float t = Mathf.Clamp01(_currentTime / duration);
            float easedT = EaseInOut(t);

            Vector3 a = isGoingToEnd ? positionStart : positionEnd;
            Vector3 b = isGoingToEnd ? positionEnd : positionStart;

            goalPosition = Vector3.Lerp(a, b, easedT);

            if (t >= 1f)
            { 
                _currentTime = 0f;
                if (randomDelay)
                {
                    duration = startDuration + (UnityEngine.Random.value * 1.5f - 0.75f);
                }
                isGoingToEnd = !isGoingToEnd;

            }
        }
        else
        {
            Vector3 target = isGoingToEnd ? positionEnd : positionStart;
            goalPosition = Vector3.MoveTowards(transform.position, target, speed * deltaTime);

            if (Vector3.Distance(goalPosition, target) < 0.1f)
            {
                isGoingToEnd = !isGoingToEnd;
            }
        }
    }

    private float EaseInOut(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
}
