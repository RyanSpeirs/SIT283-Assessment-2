using UnityEngine;
using UnityEngine.Events;

// this is an updated version of the one from week 1
public class ItemBin : MonoBehaviour
{
    // set capacity and item type, exposed to communicate type
    [SerializeField] private ItemType acceptedItem;
    public ItemType AcceptedItem => acceptedItem;
    [SerializeField] private int capacity = 5;
    [SerializeField] private GameObject correctSoundPrefab;
    [SerializeField] private GameObject wrongSoundPrefab;
    [SerializeField] private float ejectDistance = 0.8f;
    [SerializeField] private float ejectSpeed = 2.5f;

    // notifies wrong item
    public ItemTypeEvent OnWrongItem;

    // notifies right item
    public ItemTypeIntEvent OnCountChanged;
    //  for possible future us
    public ItemTypeEvent OnBinFull;

    private int currentCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        // First we determine is the object an item or not
        Item item = other.GetComponent<Item>();
        if (item == null)
        {
            Debug.Log($"[ItemBin] Ignored non-item: {other.gameObject.name}");
            return;
        }

        // if its an item we need to check if its the wrong one, play the wrong sound and spit it back out
        if (item.type != acceptedItem)
        {
            Debug.Log($"[ItemBin] Wrong type ({item.type}, expected {acceptedItem}) — ejecting");
            PlaySound(wrongSoundPrefab);
            Eject(other.gameObject);
            OnWrongItem?.Invoke(item.type);
            return;
        }

        //  If the item is correct but the bin is full we log the contact but do nothing
        if (currentCount >= capacity)
        {
            Debug.Log($"[ItemBin] Correct type but bin already full ({currentCount}/{capacity})");
            Destroy(other.gameObject);
            return;
        }

        //  increment the count, plus log it, and then destroy it
        currentCount++;
        Debug.Log($"[ItemBin] Counted! {acceptedItem} now at {currentCount}/{capacity}");
        PlaySound(correctSoundPrefab);
        OnCountChanged?.Invoke(acceptedItem, currentCount);
        Destroy(other.gameObject);

        // Lastly if this fills the bin, we trigger the bin-full event for the scoreboard and the spawner
        if (currentCount >= capacity)
        {
            Debug.Log($"[ItemBin] {acceptedItem} bin full — invoking OnBinFull");
            OnBinFull?.Invoke(acceptedItem);
        }
    }

    public void SetCapacity(int newCapacity)
    {
        capacity = newCapacity;
    }
    
    

    private void PlaySound(GameObject soundPrefab)
    {
        if (soundPrefab != null) Instantiate(soundPrefab, transform.position, Quaternion.identity);
    }

    private void Eject(GameObject obj)
    {
        // out the way it came in: from the bin's centre through the item's current position
        Vector3 outward = Camera.main.transform.position - transform.position;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f) outward = transform.forward;   // dead centre, pick the bin's front
        outward.Normalize();

        obj.transform.SetParent(null, true);   // in case the litter picker is still holding it
        Vector3 spot = transform.position + outward * ejectDistance;
        obj.transform.position = new Vector3(spot.x, obj.transform.position.y, spot.z);   // keep the opening's height

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;   
        rb.angularVelocity = Vector3.zero;
        rb.AddForce((outward + Vector3.up * 0.5f).normalized * ejectSpeed, ForceMode.VelocityChange);
    }

    // GameManager calls this when returning to the start screen
    public void ResetBin()
    {
        currentCount = 0;
    }
}

