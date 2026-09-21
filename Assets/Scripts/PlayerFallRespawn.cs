using UnityEngine;

// Attach to the XR Origin root. Teleports the rig back to a spawn point if it
// falls below a threshold. Fade reference is more pleasant in headsset than blinking.
// Reusable across scenes: drop it on any rig, and if
// no spawnPoint is assigned it just remembers wherever it started.
public class PlayerFallRespawn : MonoBehaviour
{
    [Header("Fall detection")]
    [SerializeField] private float fallThreshold = -5f;
    [SerializeField] private Transform spawnPoint; // falls back to this object's start position

    [Header("Movement (assign whichever the rig actually uses)")]
    [SerializeField] private CharacterController characterController; // most XR Origin locomotion setups

    private Vector3 defaultSpawnPosition;
    private Quaternion defaultSpawnRotation;
    private bool isRespawning = false;

    private void Start()
    {
        defaultSpawnPosition = transform.position;
        defaultSpawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (isRespawning) return;

        if (transform.position.y < fallThreshold)
        {
            RespawnRoutine();
        }
    }

    private void RespawnRoutine()
    {
        isRespawning = true;

        Vector3 targetPosition = spawnPoint != null ? spawnPoint.position : defaultSpawnPosition;
        Quaternion targetRotation = spawnPoint != null ? spawnPoint.rotation : defaultSpawnRotation;

        Teleport(targetPosition, targetRotation);

        isRespawning = false;
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        // CharacterController fights a direct transform write while it's enabled and mid-collision, so disable it briefly for a clean teleport.
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }

    }

    
}