using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GraffitiTask : MonoBehaviour
{
    [Header("Furniture by difficulty")]
    [SerializeField] private GameObject[] difficulty0Furniture = new GameObject[2];
    [SerializeField] private GameObject[] difficulty1Furniture = new GameObject[2];
    [SerializeField] private GameObject[] difficulty2Furniture = new GameObject[2];

    public UnityEvent<float> OnProgress;
    public UnityEvent OnAllCleaned;

    public float Progress { get; private set; }
    public bool IsComplete { get; private set; }

    private readonly List<GameObject> activeFurniture = new List<GameObject>();
    private readonly List<GraffitiSurface> activeSurfaces = new List<GraffitiSurface>();

    private void Awake()
    {
        AddListeners(difficulty0Furniture);
        AddListeners(difficulty1Furniture);
        AddListeners(difficulty2Furniture);
    }

    private void AddListeners(GameObject[] furniture)
    {
        foreach (GameObject obj in furniture)
        {
            if (obj == null)
                continue;

            GraffitiSurface[] surfaces = obj.GetComponentsInChildren<GraffitiSurface>();

            foreach (GraffitiSurface surface in surfaces)
                surface.OnProgress.AddListener(_ => Refresh());
        }
    }

    public void Begin(int difficulty)
    {
        activeFurniture.Clear();
        activeSurfaces.Clear();

        // Tables are always required.
        AddFurniture(difficulty0Furniture);

        // Add the first two benches at difficulty 1+.
        if (difficulty >= 1)
            AddFurniture(difficulty1Furniture);

        // Add the final two benches at difficulty 2.
        if (difficulty >= 2)
            AddFurniture(difficulty2Furniture);

        // Sets furniture to be active, inactive furniture doesn't get vandalised.
        SetFurnitureActive(difficulty0Furniture, true);
        SetFurnitureActive(difficulty1Furniture, difficulty >= 1);
        SetFurnitureActive(difficulty2Furniture, difficulty >= 2);

        // Reset all six furniture objects.
        ResetFurniture(difficulty0Furniture);
        ResetFurniture(difficulty1Furniture);
        ResetFurniture(difficulty2Furniture);

        IsComplete = false;
        Progress = 0f;

        Refresh();
    }

    private void AddFurniture(GameObject[] furniture)
    {
        foreach (GameObject obj in furniture)
        {
            if (obj == null || activeFurniture.Contains(obj))
                continue;

            activeFurniture.Add(obj);

            GraffitiSurface[] surfaces = obj.GetComponentsInChildren<GraffitiSurface>();

            foreach (GraffitiSurface surface in surfaces)
                activeSurfaces.Add(surface);
        }
    }

    private void ResetFurniture(GameObject[] furniture)
    {
        foreach (GameObject obj in furniture)
        {
            if (obj == null)
                continue;

            GraffitiSurface[] surfaces = obj.GetComponentsInChildren<GraffitiSurface>();

            foreach (GraffitiSurface surface in surfaces)
                surface.ResetSurface();
        }
    }

    //
    private void SetFurnitureActive(GameObject[] furniture, bool active)
    {
        foreach (GameObject obj in furniture)
        {
            if (obj == null)
                continue;

            GraffitiSurface[] surfaces = obj.GetComponentsInChildren<GraffitiSurface>();

            foreach (GraffitiSurface surface in surfaces)
                surface.SetCleaningEnabled(active);
        }
    }

    //  checks all the active surfaces and updates their percentage for the UI to display
    private void Refresh()
    {
        if (activeSurfaces.Count == 0)
            return;   // Begin() hasn't run, so nothing is being tracked

        float total = 0f;
        bool allDone = true;

        foreach (GraffitiSurface surface in activeSurfaces)
        {
            if (surface.IsDone)
            {
                total += 1f;
            }
            else
            {
                total += surface.Cleaned;
                allDone = false;
            }
        }

        Progress = total / activeSurfaces.Count;
        OnProgress?.Invoke(Progress);

        if (allDone && !IsComplete)
        {
            IsComplete = true;
            OnAllCleaned?.Invoke();
        }
    }
}