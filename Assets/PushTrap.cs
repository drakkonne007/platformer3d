using UnityEngine;

public class PushTrap : MonoBehaviour
{

    [SerializeField] float speed = 1;
    [SerializeField] GameObject start;
    [SerializeField] GameObject end;

    Vector3 positionStart = new(), positionEnd = new();
    bool isEnd = false;

    void Start()
    {
        positionStart = start.transform.position;
        positionEnd = end.transform.position;
        start.SetActive(false);
        end.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target;
        if (isEnd)
        {
            target = positionStart;
        }
        else
        {
            target = positionEnd;
        }
        transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * speed);
        if(Vector3.Distance(transform.position, target) < 0.5f)
        {
            isEnd = !isEnd;
        }
    }
}
