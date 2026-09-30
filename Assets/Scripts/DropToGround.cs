using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Goes on movable bins, park furniture, or other objects that should stand on the ground when released.
public class DropToGround : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;

    public void Drop(SelectExitEventArgs args)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
            return;

        // Preserve the object's current facing direction.
        float yRotation = transform.eulerAngles.y;

        // Stand the object upright.
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        Physics.SyncTransforms();

        // Recalculate bounds after rotation.
        Bounds bounds = colliders[0].bounds;

        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }

        // Raycast from above the object.
        Vector3 rayStart = new Vector3(
            bounds.center.x,
            bounds.max.y + 1f,
            bounds.center.z
        );

        if (!Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                bounds.size.y + 50f,
                groundMask))
        {
            return;
        }

        // Find the lowest collider point.
        float lowestPoint = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            lowestPoint = Mathf.Min(lowestPoint, collider.bounds.min.y);
        }

        // Move the object so the lowest collider point touches the ground.
        float difference = hit.point.y - lowestPoint;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position += Vector3.up * difference;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position += Vector3.up * difference;
        }
    }
}