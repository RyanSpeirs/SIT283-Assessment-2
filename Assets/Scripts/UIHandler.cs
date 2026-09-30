using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text hudText;
    [SerializeField] private GraffitiTask graffitiTask;

    [Header("XR Hands")]
    [SerializeField] private XRBaseInteractor leftHand;
    [SerializeField] private XRBaseInteractor rightHand;

    [Header("Litter Picker")]
    [SerializeField] private LitterPicker picker;

private readonly List<Transform> pickerHeld = new List<Transform>();

    private readonly Dictionary<ItemType, int> binned = new Dictionary<ItemType, int>();
    private readonly List<Transform> held = new List<Transform>();
    private readonly List<Transform> previousHeld = new List<Transform>();

    private int totalToSort;

    public int TotalBinned { get; private set; }
    public int WrongAttempts { get; private set; }

    private void Start()
    {
        if (graffitiTask != null)
            graffitiTask.OnProgress.AddListener(HandleGraffitiProgress);

        if (picker != null)
        {
            picker.OnItemHeld.AddListener(HandlePickerHeld);
            picker.OnItemReleased.AddListener(HandlePickerReleased);
        }
    }

    // Called by the GameManager at the start of a round
    public void Begin(int total)
    {
        binned.Clear();
        held.Clear();
        previousHeld.Clear();
        pickerHeld.Clear();

        TotalBinned = 0;
        WrongAttempts = 0;
        totalToSort = total;

        Refresh();
    }

    private void Update()
    {
        UpdateHeld();

        if (pickerHeld.RemoveAll(t => t == null) > 0)
            Refresh();
    }

    private void UpdateHeld()
    {
        held.Clear();

        AddSelectedFromHand(leftHand);
        AddSelectedFromHand(rightHand);

        if (!HeldObjectsChanged())
            return;

        previousHeld.Clear();
        previousHeld.AddRange(held);

        Refresh();
    }

    private void AddSelectedFromHand(XRBaseInteractor hand)
    {
        if (hand == null)
            return;

        foreach (IXRSelectInteractable interactable in hand.interactablesSelected)
        {
            Transform obj = interactable.transform;

            if (obj != null && !held.Contains(obj))
                held.Add(obj);
        }
    }

    private bool HeldObjectsChanged()
    {
        if (held.Count != previousHeld.Count)
            return true;

        for (int i = 0; i < held.Count; i++)
        {
            if (!previousHeld.Contains(held[i]))
                return true;
        }

        return false;
    }

    private string HeldNames()
    {
        List<string> names = new List<string>();

        foreach (Transform t in held)
            if (t != null) names.Add(CleanName(t.name));

        foreach (Transform t in pickerHeld)
            if (t != null) names.Add(CleanName(t.name));

        return names.Count == 0 ? "-" : string.Join(", ", names);
    }

    private static string CleanName(string raw)
    {
        string name = raw.Replace("(Clone)", "").Trim();

        return Regex.Replace(
            name,
            "(?<=[a-z])(?=[A-Z])",
            " "
        );
    }

    public void Show(bool visible)
    {
        hudRoot.SetActive(visible);
    }

    // Listens to ItemBin.OnCountChanged
    public void HandleCountChanged(ItemType type, int count)
    {
        binned[type] = count;

        TotalBinned = 0;

        foreach (int c in binned.Values)
            TotalBinned += c;

        Refresh();
    }

    // Listens to ItemBin.OnWrongItem
    public void HandleWrongItem(ItemType type)
    {
        WrongAttempts++;
        Refresh();
    }

    // Listens to GraffitiTask.OnProgress
    private void HandleGraffitiProgress(float progress)
    {
        Refresh();
    }

    private void Refresh()
    {
        StringBuilder text = new StringBuilder();

        text.AppendLine($"<b>Sorted</b>  {TotalBinned} / {totalToSort}");

        if (graffitiTask != null)
            text.AppendLine($"<b>Cleaned</b>  {graffitiTask.Progress:P0}");

        text.AppendLine();

        if (binned.Count > 0)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<ItemType, int> pair in binned)
                parts.Add($"{pair.Key} {pair.Value}");

            text.AppendLine($"<size=80%>{string.Join("   ", parts)}</size>");
            text.AppendLine();
        }

        text.AppendLine($"<color=#FF8A8A>Mistakes  {WrongAttempts}</color>");
        text.AppendLine();
        text.Append($"<b>Holding</b>\n{HeldNames()}");

        hudText.text = text.ToString();
    }

    private void HandlePickerHeld(Transform item)
    {
        if (!pickerHeld.Contains(item)) pickerHeld.Add(item);
        Refresh();
    }

    private void HandlePickerReleased(Transform item)
    {
        pickerHeld.Remove(item);
        Refresh();
    }
}