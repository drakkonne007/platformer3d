using UnityEngine;

public class Coin : Item
{
    [SerializeField] float value = 10;
    public override void getEffect() 
    {
        MainHandler.Instance.addMoney(10);
    }
}
