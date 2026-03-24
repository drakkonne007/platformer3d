using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public List<ItemInventarSO> inventarItems = new();


    public HashSet<ItemInventarSO> getItemIds()
    {
        HashSet<ItemInventarSO> str = new();
        foreach(ItemInventarSO item in inventarItems)
        {
            str.Add(item);
        }
        return str;
    }

    public void AddItem(ItemInventarSO obj)
    {
        inventarItems.Add(obj);
    }
}
