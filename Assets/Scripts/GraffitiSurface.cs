using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshFilter))]
public class GraffitiSurface : MonoBehaviour
{
    [SerializeField] private Texture2D graffitiTexture;   // Transparent alpha channel, Read/Write on, Compression None
    [SerializeField] private Material overlayMaterial;    // URP Unlit, Surface Type Transparent
    [Range(0.5f, 1f)] [SerializeField] private float doneThreshold = 0.80f;  // we want less than 100% due to concealed surfaces.
    [Range(1, 128)] [SerializeField] private int dirtyAlpha = 40;

    public UnityEvent<GraffitiSurface> OnProgress;

    public float Cleaned => 1f - (float)dirtyLeft / Mathf.Max(1, dirtyTotal);
    public bool IsDone { get; private set; }

    private Texture2D tex;
    private Color32[] pixels;
    private int width, height, dirtyTotal, dirtyLeft;
    private bool needsApply;

    private Transform overlay;
    private Renderer rend;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    void Awake()
    {
        if (graffitiTexture == null || !graffitiTexture.isReadable)
        {
            Debug.LogError("GraffitiSurface needs a graffiti texture with Read/Write enabled.", this);
            enabled = false;
            return;
        }

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        vertices = mesh.vertices;
        uvs = mesh.uv;
        triangles = mesh.triangles;

        GameObject go = new GameObject("GraffitiOverlay");
        overlay = go.transform;
        overlay.SetParent(transform, false);
        overlay.localScale = Vector3.one * 1.002f;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        rend = go.AddComponent<MeshRenderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        width = graffitiTexture.width;
        height = graffitiTexture.height;
        pixels = graffitiTexture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i].a > dirtyAlpha) dirtyTotal++;
        dirtyLeft = dirtyTotal;

        tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(pixels);
        tex.Apply(false);

        Material m = new Material(overlayMaterial);
        m.SetTexture("_BaseMap", tex);
        rend.sharedMaterial = m;
    }

    //  erases the png graffiti from the overlay material around the objects
    public bool Erase(Vector3 worldPoint, float radiusMetres, Vector2? from, out Vector2 uv)
    {
        uv = default;
        if (!enabled || IsDone || !TryGetUV(worldPoint, out uv, out int tri)) return false;

        GetMetresPerUV(tri, out float mPerU, out float mPerV);
        float rx = Mathf.Clamp(radiusMetres / mPerU * width, 1f, 200f);
        float ry = Mathf.Clamp(radiusMetres / mPerV * height, 1f, 200f);

        if (from.HasValue && (from.Value - uv).sqrMagnitude < 0.04f)
        {
            float dx = Mathf.Abs(uv.x - from.Value.x) * width;
            float dy = Mathf.Abs(uv.y - from.Value.y) * height;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(dx / (rx * 0.5f), dy / (ry * 0.5f))));
            for (int s = 1; s <= steps; s++)
                Stamp(Vector2.Lerp(from.Value, uv, s / (float)steps), rx, ry);
        }
        else Stamp(uv, rx, ry);

        needsApply = true;
        return true;
    }

    //  running these checks just after update
    void LateUpdate()
    {
        if (!needsApply) return;
        needsApply = false;

        tex.SetPixels32(pixels);
        tex.Apply(false);

        if (Cleaned >= doneThreshold)
        {
            IsDone = true;
            rend.enabled = false;
        }
        OnProgress?.Invoke(this);
    }

    // 
    private void Stamp(Vector2 uv, float rx, float ry)
    {
        int cx = Mathf.RoundToInt(uv.x * width);
        int cy = Mathf.RoundToInt(uv.y * height);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
        int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry));

        for (int y = y0; y <= y1; y++)
        {
            float ny = (y - cy) / ry;
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x - cx) / rx;
                if (nx * nx + ny * ny > 1f) continue;

                int i = y * width + x;
                byte a = pixels[i].a;
                if (a == 0) continue;

                pixels[i].a = 0;
                if (a > dirtyAlpha) dirtyLeft--;
            }
        }
    }

    // This gets metres covered by one UV unit on the hit triangle, so the brush stays round on any face
    // and we don't get weird jagged areas when cleaning
    private void GetMetresPerUV(int tri, out float mPerU, out float mPerV)
    {
        int i0 = triangles[tri * 3], i1 = triangles[tri * 3 + 1], i2 = triangles[tri * 3 + 2];
        Vector3 e1 = overlay.TransformVector(vertices[i1] - vertices[i0]);
        Vector3 e2 = overlay.TransformVector(vertices[i2] - vertices[i0]);
        Vector2 d1 = uvs[i1] - uvs[i0];
        Vector2 d2 = uvs[i2] - uvs[i0];

        float det = d1.x * d2.y - d1.y * d2.x;
        if (Mathf.Abs(det) < 1e-8f) { mPerU = mPerV = 1f; return; }

        mPerU = ((e1 * d2.y - e2 * d1.y) / det).magnitude;
        mPerV = ((e2 * d1.x - e1 * d2.x) / det).magnitude;
    }

    // Finds the triangle under the hit point and interpolates its UVs
    private bool TryGetUV(Vector3 worldPoint, out Vector2 uv, out int tri)
    {
        Vector3 p = transform.InverseTransformPoint(worldPoint);

        float bestDist = float.MaxValue;
        uv = default;
        tri = -1;

        //  so here we're actually going over the polys as we did with the mesh cutting exercise
        for (int t = 0; t < triangles.Length / 3; t++)
        {

            int i0 = triangles[t * 3];
            int i1 = triangles[t * 3 + 1];
            int i2 = triangles[t * 3 + 2];

            Vector3 a = vertices[i0];
            Vector3 b = vertices[i1];
            Vector3 c = vertices[i2];

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;

            Vector3 normal = Vector3.Cross(ab, ac).normalized;
            Vector3 projected = p - normal * Vector3.Dot(ap, normal);

            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = projected - a;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;

            if (Mathf.Abs(denom) < 1e-12f)
                continue;

            float wb = (d11 * d20 - d01 * d21) / denom;
            float wc = (d00 * d21 - d01 * d20) / denom;
            float wa = 1f - wb - wc;

            // Only accept points actually inside this triangle
            if (wa < -0.001f || wb < -0.001f || wc < -0.001f)
                continue;

            float dist = (p - projected).sqrMagnitude;

            if (dist >= bestDist)
                continue;

            bestDist = dist;
            tri = t;

            uv =
                uvs[i0] * wa +
                uvs[i1] * wb +
                uvs[i2] * wc;
        }

        return tri >= 0;
    }

    //  enables objects to be cleaned
    public void SetCleaningEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (overlay != null)
            overlay.gameObject.SetActive(enabled);
    }

    // when we end a round we need to reset everything to dirty.
    public void ResetSurface()
    {
        if (graffitiTexture == null || tex == null)
            return;

        pixels = graffitiTexture.GetPixels32();

        dirtyTotal = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > dirtyAlpha)
                dirtyTotal++;
        }

        dirtyLeft = dirtyTotal;

        IsDone = false;
        needsApply = false;

        tex.SetPixels32(pixels);
        tex.Apply(false);

        if (rend != null)
            rend.enabled = true;

    }
}