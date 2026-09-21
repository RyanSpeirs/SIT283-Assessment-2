using UnityEngine;
using UnityEngine.Events;

// Goes on a grabbable tool that has an XR Grab Interactable.
// In the interactable's events, hook:  Activated -> OnActivated()   and   Deactivated -> OnDeactivated()
// No physics simulation: the nearest item near the tip is made kinematic and parented to the tip.
public class LitterPicker : MonoBehaviour
{
    [SerializeField] private Transform tip;
    [SerializeField] private float pickRadius = 0.12f;

    [Tooltip("On: press once to close, press again to release. Off: hold the trigger to keep hold.")]
    [SerializeField] private bool toggleMode = true;

    [Header("Jaw visuals (optional)")]
    [SerializeField] private Transform jawA;
    [SerializeField] private Transform jawB;
    [SerializeField] private float openAngle = 25f;
    [SerializeField] private float closedAngle = 2f;
    [SerializeField] private float jawSpeed = 8f;

    private Item held;
    private bool closed;

    public UnityEvent<Transform> OnItemHeld;
    public UnityEvent<Transform> OnItemReleased;

    public void OnActivated()
    {
        if (toggleMode && closed) Release();
        else Grip();
    }

    public void OnDeactivated()
    {
        if (!toggleMode) Release();
    }

    void Update()
    {
        // The bin can take the item away (destroy or eject), so drop our reference if it is no longer on the tip
        if (held != null && held.transform.parent != tip)
        {
            OnItemReleased?.Invoke(held.transform);
            held = null;
        }

        float angle = closed ? closedAngle : openAngle;
        RotateJaw(jawA, angle);
        RotateJaw(jawB, -angle);
    }

    private void Grip()
    {
        closed = true;
        if (held != null) return;

        Item nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in Physics.OverlapSphere(tip.position, pickRadius))
        {
            Item item = hit.GetComponentInParent<Item>();
            if (item == null) continue;

            float distance = Vector3.Distance(tip.position, item.transform.position);
            if (distance < nearestDistance)
            {
                nearest = item;
                nearestDistance = distance;
            }
        }

        if (nearest == null) return;

        Rigidbody rb = nearest.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        nearest.transform.SetParent(tip, true);
        held = nearest;
        OnItemHeld?.Invoke(held.transform);
    }

    private void Release()
    {
        closed = false;
        if (held == null) return;

        held.transform.SetParent(null, true);
        OnItemReleased?.Invoke(held.transform);
        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        held = null;
    }

    private void RotateJaw(Transform jaw, float angle)
    {
        if (jaw == null) return;
        Quaternion target = Quaternion.Euler(0f, 0f, angle);   // change the axis to match your model
        jaw.localRotation = Quaternion.Slerp(jaw.localRotation, target, Time.deltaTime * jawSpeed);
    }
}