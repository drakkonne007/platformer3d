using UnityEngine;

public class ShadowFriend : MonoBehaviour
{
    [SerializeField] Material friendMat;
    [SerializeField] Material defaultMat;
    [SerializeField] SpriteRenderer render;
    public void setFriend(bool friend)
    {
        render.material = friend ? friendMat : defaultMat;
        if (!friend && true) //todo ADD dungeon info
        {
            gameObject.SetActive(false);
        }
    }
}
