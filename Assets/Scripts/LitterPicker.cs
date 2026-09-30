using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Goes on a litter-picking tool that has an XR Grab Interactable component.
public class LitterPicker : MonoBehaviour
{
    [SerializeField] private Transform tip;
    [SerializeField] private float pickRadius = 0.15f;
    [SerializeField] private float regrabDelay = 0.75f;
    private float nextGrabTime;
    private bool wasActivating;

    private XRGrabInteractable grabInteractable;
    private XRBaseInputInteractor holdingInteractor;

    private Item held;

    public UnityEvent<Transform> OnItemHeld;
    public UnityEvent<Transform> OnItemReleased;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Update()
    {
        UpdateHoldingInteractor();

        // Something else (a bin, for example) took the item off the tip
        if (held != null && held.transform.parent != tip)
        {
            Item lost = held;
            held = null;
            nextGrabTime = Time.time + regrabDelay;
            OnItemReleased?.Invoke(lost.transform);
        }

        // Automatically pick up litter when the tip reaches it
        if (held == null && Time.time >= nextGrabTime)
        {
            Item nearest = FindNearestItem();

            if (nearest != null)
                HoldItem(nearest);
        }

        // Release on the press of the Activate input, not while it is held
        bool activating = holdingInteractor != null && holdingInteractor.shouldActivate;
        if (activating && !wasActivating)
            Release();
        wasActivating = activating;

        // If the player grabs the litter directly, the player takes priority
        if (held != null)
        {
            XRGrabInteractable itemGrab = held.GetComponent<XRGrabInteractable>();

            if (itemGrab != null && itemGrab.isSelected)
                RemoveHeldItem();
        }
    }

    private void UpdateHoldingInteractor()
    {
        holdingInteractor = null;

        if (!grabInteractable.isSelected)
            return;

        foreach (var interactor in grabInteractable.interactorsSelecting)
        {
            if (interactor is XRBaseInputInteractor inputInteractor)
            {
                holdingInteractor = inputInteractor;
                return;
            }
        }
    }

    private Item FindNearestItem()
    {
        Item nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in Physics.OverlapSphere(tip.position, pickRadius))
        {
            Item item = hit.GetComponentInParent<Item>();

            if (item == null)
                continue;
            
            XRGrabInteractable itemGrab = item.GetComponent<XRGrabInteractable>();
            if (itemGrab != null && itemGrab.isSelected)
                continue;

            Vector3 closestPoint = hit.ClosestPoint(tip.position);
            float distance = Vector3.Distance(tip.position, closestPoint);

            if (distance < nearestDistance)
            {
                nearest = item;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void HoldItem(Item item)
    {
        if (held != null)
            return;

        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = true;

        item.transform.SetParent(tip, true);

        held = item;

        OnItemHeld?.Invoke(held.transform);
    }

    public void Release()
    {
        if (held == null)
            return;

        RemoveHeldItem();
    }

    private void RemoveHeldItem()
    {
        Item item = held;

        held = null;

        item.transform.SetParent(null, true);

        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = false;

        OnItemReleased?.Invoke(item.transform);
        nextGrabTime = Time.time + regrabDelay;
    }

    private void OnDrawGizmosSelected()
    {
        if (tip != null)
            Gizmos.DrawWireSphere(tip.position, pickRadius);
    }

    public void Drop(Item item)
    {
        if (item != null && item == held)
            RemoveHeldItem();
    }
}

