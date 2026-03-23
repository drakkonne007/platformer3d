using UnityEngine;

public class MagnetWalker : MonoBehaviour
{
    ColliderStarter starter;
    Vector3 plus = new Vector3(0,1.5f,0);
    private void Awake()
    {
        starter = GetComponent<ColliderStarter>();
        starter.OnStay += stayInMagnet;
    }
    private void OnDestroy()
    {
        starter.OnStay -= stayInMagnet;
    }
    void stayInMagnet(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Magnet"))
        {
            transform.position = Vector3.MoveTowards(transform.position, other.transform.position + plus, Time.deltaTime * 8);
        }
    }
}
