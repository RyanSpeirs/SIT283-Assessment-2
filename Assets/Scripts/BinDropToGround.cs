using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Goes on a movable bin
public class BinDropToGround : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float startHeight = 2f;    
    [SerializeField] private float heightOffset = 0f;  

    public void Drop(SelectExitEventArgs args)
    {
        Vector3 pos = transform.position;

        if (Physics.Raycast(pos + Vector3.up * startHeight, Vector3.down, out RaycastHit hit,
                startHeight + 50f, groundMask))
        {
            transform.position = new Vector3(pos.x, hit.point.y + heightOffset, pos.z);
        }

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);   // stand upright again
    }
}
