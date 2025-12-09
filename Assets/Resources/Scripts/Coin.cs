using UnityEngine;

public class Coin : Item
{
    [SerializeField] int value = 10;
    public override void getEffect(Collider other) 
    {
        MainHandler.Instance.addMoney(value);
    }
}
