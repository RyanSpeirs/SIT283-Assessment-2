using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.XR.Interaction.Toolkit;


public class UIHandler : MonoBehaviour
{
    // hudRoot is the panel that gets shown and hidden; keep this script on a different object
    // (for example the Canvas or a manager) so it keeps running while the panel is hidden
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text hudText;

    private readonly Dictionary<ItemType, int> binned = new Dictionary<ItemType, int>();
    private int totalToSort;

    public int TotalBinned { get; private set; }
    public int WrongAttempts { get; private set; }

    private readonly List<Transform> held = new List<Transform>();

    // Hook to each grabbing interactor's Select Entered / Select Exited events
    public void HandleSelectEntered(SelectEnterEventArgs args) => AddHeld(args.interactableObject.transform);
    public void HandleSelectExited(SelectExitEventArgs args) => RemoveHeld(args.interactableObject.transform);


    // Called by the GameManager at the start of a round
    public void Begin(int total)
    {
        binned.Clear();
        TotalBinned = 0;
        WrongAttempts = 0;
        totalToSort = total;
        Refresh();
    }

    public void AddHeld(Transform obj)
    {
        if (obj != null && !held.Contains(obj)) held.Add(obj);
        Refresh();
    }

    public void RemoveHeld(Transform obj)
    {
        held.Remove(obj);
        Refresh();
    }

    void Update()
    {
        // a bin destroys correct items while they are still being held
        if (held.RemoveAll(t => t == null) > 0) Refresh();
    }

    private string HeldNames()
    {
        if (held.Count == 0) return "-";

        List<string> names = new List<string>();
        foreach (Transform t in held)
        {
            if (t != null) names.Add(CleanName(t.name));
        }
        return string.Join(", ", names);
    }

    private static string CleanName(string raw)
    {
        string name = raw.Replace("(Clone)", "").Trim();
        return Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");   // "LitterPicker" becomes "Litter Picker"
    }

    public void Show(bool visible)
    {
        hudRoot.SetActive(visible);
    }

    // Hook to ItemBin.OnCountChanged
    public void HandleCountChanged(ItemType type, int count)
    {
        binned[type] = count;

        TotalBinned = 0;
        foreach (int c in binned.Values) TotalBinned += c;

        Refresh();
    }

    // Hook to ItemBin.OnWrongItem
    public void HandleWrongItem(ItemType type)
    {
        WrongAttempts++;
        Refresh();
    }

    private void Refresh()
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine($"Sorted: {TotalBinned} / {totalToSort}");
        foreach (KeyValuePair<ItemType, int> pair in binned)
        {
            text.AppendLine($"{pair.Key}: {pair.Value}");
        }
        text.AppendLine($"Mistakes: {WrongAttempts}");
        text.Append($"Holding: {HeldNames()}");
        hudText.text = text.ToString();
    }
}
