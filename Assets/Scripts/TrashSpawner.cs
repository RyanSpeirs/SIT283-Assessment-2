using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Spawn volume (keep it low to the ground)")]
    [SerializeField] private BoxCollider spawnVolume;

    [Header("One prefab per item type")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Header("Placement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask treeMask;
    [SerializeField] private float groundSearchDistance = 3f;
    [SerializeField] private float treeClearance = 0.08f;
    [SerializeField] private float heightAboveGround = 0.1f;
    [SerializeField] private int attemptsPerItem = 30;

    private readonly Dictionary<ItemType, int> spawnedByType = new Dictionary<ItemType, int>();
    private readonly List<GameObject> spawned = new List<GameObject>();


    public int Spawn(int count)
    {
        if (spawnVolume == null)
        {
            Debug.LogError("[TrashSpawner] Spawn volume is not assigned.");
            return 0;
        }

        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("[TrashSpawner] No item prefabs assigned.");
            return 0;
        }

        Physics.SyncTransforms();

        Bounds b = spawnVolume.bounds;

        Debug.Log(
            $"[TrashSpawner] Spawn volume bounds: " +
            $"min={b.min}, max={b.max}, center={b.center}, size={b.size}"
        );

        int placed = 0;

        for (int i = 0; i < count; i++)
        {
            if (!TryFindPoint(out Vector3 point))
            {
                Debug.LogWarning(
                    $"[TrashSpawner] Could not find a clear spot for item {i + 1}/{count}"
                );

                continue;
            }

            GameObject prefab = itemPrefabs[i % itemPrefabs.Length];

            Quaternion yaw = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );

            GameObject instance = Instantiate(prefab, point, yaw);
            spawned.Add(instance);

            Item item = instance.GetComponent<Item>();

            if (item != null)
            {
                spawnedByType.TryGetValue(item.type, out int n);
                spawnedByType[item.type] = n + 1;
            }

            placed++;
        }

        return placed;
    }


    public void ClearAll()
    {
        foreach (GameObject item in spawned)
        {
            if (item != null)
                Destroy(item);
        }

        spawned.Clear();
        spawnedByType.Clear();
    }


    private bool TryFindPoint(out Vector3 point)
    {
        Bounds b = spawnVolume.bounds;

        for (int i = 0; i < attemptsPerItem; i++)
        {
            Vector3 rayOrigin = new Vector3(
                Random.Range(b.min.x, b.max.x),
                b.max.y,
                Random.Range(b.min.z, b.max.z)
            );

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                b.size.y + groundSearchDistance,
                groundMask))
            {
                continue;
            }

            Vector3 candidate = hit.point + Vector3.up * heightAboveGround;

            // Safety check: reject suspicious origin placements.
            if (candidate.sqrMagnitude < 0.01f)
            {
                Debug.LogWarning(
                    $"[TrashSpawner] Rejected suspicious spawn point: {candidate}. " +
                    $"Ray origin was {rayOrigin}, hit {hit.collider.name}."
                );

                continue;
            }

            // Reject anything inside or too close to a tree.
            if (Physics.CheckSphere(candidate, treeClearance, treeMask))
            {
                continue;
            }

            point = candidate;
            return true;
        }

        point = Vector3.zero;
        return false;
    }


    public int CountOf(ItemType type)
    {
        return spawnedByType.TryGetValue(type, out int n) ? n : 0;
    }
}