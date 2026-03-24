using Unity.VisualScripting;
using UnityEngine;

public class Coin : ItemOnMap
{
    [SerializeField] int value = 10;
    public override void getEffect(Collider other)
    {
        MainHandler.Instance.addMoney(value); 
    }
}
