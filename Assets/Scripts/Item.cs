using UnityEngine;
using UnityEngine.Events;

// this is an updated version of the one from week 1

//  We keep this here to prevent script bloat
public enum ItemType
{
    DrinkCan,
    TakeawayBox,
    PlasticBottle
}

public class Item : MonoBehaviour
{
    // defines the item's type
    public ItemType type;

}

//  Same as the ItemType enum, we keep these here so its in one place
[System.Serializable] public class ItemTypeEvent : UnityEvent<ItemType> { }
[System.Serializable] public class ItemTypeIntEvent : UnityEvent<ItemType, int> { }
