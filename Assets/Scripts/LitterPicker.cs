using UnityEngine;
using UnityEngine.Events;

// Goes on a grabbing tool that has an XR Grab Interactable component.
public class LitterPicker : MonoBehaviour
{
    [SerializeField] private Transform tip;
    [SerializeField] private float pickRadius = 0.15f;

    [Tooltip("On: press once to close, press again to release. Off: hold the trigger to keep hold.")]
    [SerializeField] private bool toggleMode = true;

    [Header("Jaw visuals (optional)")]
    [SerializeField] private Transform jawA;
    [SerializeField] private Transform jawB;
    [SerializeField] private float openAngle = 25f;
    [SerializeField] private float closedAngle = 2f;
    [SerializeField] private float jawSpeed = 8f;
    [SerializeField] private Vector3 jawAxis = Vector3.forward;   // the hinge axis in each jaw's own space
    private Quaternion restA, restB;

    private Item held;
    private bool closed;

    public UnityEvent<Transform> OnItemHeld;
    public UnityEvent<Transform> OnItemReleased;

    void Awake()
    {
        if (jawA != null) restA = jawA.localRotation;
        if (jawB != null) restB = jawB.localRotation;
    }


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
        if (held != null && held.transform.parent != tip)
        {
            OnItemReleased?.Invoke(held.transform);
            held = null;
        }

        float angle = closed ? closedAngle : openAngle;
        RotateJaw(jawA, restA, angle);
        RotateJaw(jawB, restB, -angle);
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

    private void RotateJaw(Transform jaw, Quaternion rest, float angle)
    {
        if (jaw == null) return;
        Quaternion target = rest * Quaternion.AngleAxis(angle, jawAxis);
        jaw.localRotation = Quaternion.Slerp(jaw.localRotation, target, Time.deltaTime * jawSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (tip != null) Gizmos.DrawWireSphere(tip.position, pickRadius);
    }


}