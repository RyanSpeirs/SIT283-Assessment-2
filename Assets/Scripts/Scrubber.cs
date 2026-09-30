using System.Collections.Generic;
using UnityEngine;

public class Scrubber : MonoBehaviour
{
    [SerializeField] private Transform tip;                 // centre of the sponge face
    [SerializeField] private LayerMask graffitiMask = ~0;   // layers the furniture is on
    [SerializeField] private float contactRadius = 0.25f;   // how close the tip must be to count as touching
    [SerializeField] private float brushRadius = 0.2f;      // metres
    [SerializeField] private float minSlideSpeed = 0.01f;   // metres per second over the surface

    // A struct for handling the contact and sponge stroke info
    private class Stroke
    {
        public Vector3 lastLocal;
        public Vector2 lastUV;
        public bool hasUV;
        public bool touched;
    }

    private readonly Dictionary<GraffitiSurface, Stroke> strokes = new Dictionary<GraffitiSurface, Stroke>();
    private readonly List<GraffitiSurface> stale = new List<GraffitiSurface>();
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        if (tip == null) { Debug.LogError("Scrubber: tip not assigned.", this); enabled = false; return; }
        if (graffitiMask.value == 0) graffitiMask = ~0;
    }

    // Because we are touching physics we want FixedUpdate instead of regular or Late
    private void FixedUpdate()
    {
        foreach (Stroke st in strokes.Values) st.touched = false;

        int count = Physics.OverlapSphereNonAlloc(tip.position, contactRadius, hits, graffitiMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            GraffitiSurface s = c.GetComponentInParent<GraffitiSurface>();
            if (s == null || !s.enabled || s.IsDone) continue;

            Vector3 point = (c is MeshCollider mc && !mc.convex)
                ? c.bounds.ClosestPoint(tip.position)
                : c.ClosestPoint(tip.position);

            if (!strokes.TryGetValue(s, out Stroke stroke))
            {
                strokes[s] = new Stroke { lastLocal = s.transform.InverseTransformPoint(point), touched = true };
                continue;
            }

            stroke.touched = true;

            // Slide is measured in the surface's own space, so turning the table under a still sponge counts as scrubbing too
            float slide = Vector3.Distance(s.transform.TransformPoint(stroke.lastLocal), point);
            stroke.lastLocal = s.transform.InverseTransformPoint(point);

            if (slide / Time.fixedDeltaTime >= minSlideSpeed)
            {
                Vector2? from = stroke.hasUV ? stroke.lastUV : (Vector2?)null;
                stroke.hasUV = s.Erase(point, brushRadius, from, out stroke.lastUV);
            }
            else stroke.hasUV = false;
        }

        // Forget surfaces the sponge is no longer touching
        stale.Clear();
        foreach (var pair in strokes)
            if (!pair.Value.touched) stale.Add(pair.Key);
        foreach (GraffitiSurface s in stale) strokes.Remove(s);
    }

    private void OnDisable() => strokes.Clear();
}