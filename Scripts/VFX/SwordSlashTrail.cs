using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SwordSlashTrail : MonoBehaviour
{
    [Header("Sword Points")]
    [SerializeField] private Transform trailBase;
    [SerializeField] private Transform trailTip;

    [Header("Trail Timing")]
    [SerializeField] private float pointLifetime = 0.18f;
    [SerializeField] private int maxPoints = 24;
    [SerializeField] private float minDistanceBetweenPoints = 0.03f;

    [Header("Shape")]
    [SerializeField] private AnimationCurve widthOverLifetime = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private AnimationCurve alphaOverLifetime = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Color")]
    [SerializeField] private Color baseColor = new Color(0.35f, 0.85f, 1f, 1f);
    [SerializeField] private Color tipColor = Color.white;

    private readonly List<TrailPoint> points = new();

    private Mesh mesh;
    private bool emitting;
    private Vector3 lastBasePosition;
    private Vector3 lastTipPosition;
    private bool hasLastPoint;
    private object currentOwner;

    private struct TrailPoint
    {
        public Vector3 basePosition;
        public Vector3 tipPosition;
        public float spawnTime;

        public TrailPoint(Vector3 basePosition, Vector3 tipPosition, float spawnTime)
        {
            this.basePosition = basePosition;
            this.tipPosition = tipPosition;
            this.spawnTime = spawnTime;
        }
    }

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Sword Slash Trail Mesh";
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void LateUpdate()
    {
        if (emitting)
        {
            TryAddPoint();
        }

        RemoveExpiredPoints();
        BuildMesh();
    }

    public void BeginTrail(object owner = null)
    {
        currentOwner = owner;
        emitting = true;
        points.Clear();
        mesh.Clear();

        hasLastPoint = false;

        TryAddPoint(force: true);
    }

    public void EndTrail(object owner = null)
    {
        if (owner != null && currentOwner != owner)
            return;

        emitting = false;
    }

    public void ClearTrail()
    {
        emitting = false;
        currentOwner = null;
        points.Clear();
        mesh.Clear();
        hasLastPoint = false;
    }

    private void TryAddPoint(bool force = false)
    {
        if (trailBase == null || trailTip == null)
            return;

        Vector3 currentBase = trailBase.position;
        Vector3 currentTip = trailTip.position;

        if (!force && hasLastPoint)
        {
            float baseDistance = Vector3.Distance(currentBase, lastBasePosition);
            float tipDistance = Vector3.Distance(currentTip, lastTipPosition);

            if (baseDistance < minDistanceBetweenPoints && tipDistance < minDistanceBetweenPoints)
                return;
        }

        points.Add(new TrailPoint(currentBase, currentTip, Time.time));

        if (points.Count > maxPoints)
            points.RemoveAt(0);

        lastBasePosition = currentBase;
        lastTipPosition = currentTip;
        hasLastPoint = true;
    }

    private void RemoveExpiredPoints()
    {
        float now = Time.time;

        for (int i = points.Count - 1; i >= 0; i--)
        {
            if (now - points[i].spawnTime > pointLifetime)
            {
                points.RemoveAt(i);
            }
        }
    }

    private void BuildMesh()
    {
        mesh.Clear();

        if (points.Count < 2)
            return;

        int pointCount = points.Count;

        Vector3[] vertices = new Vector3[pointCount * 2];
        Vector2[] uvs = new Vector2[pointCount * 2];
        Color[] colors = new Color[pointCount * 2];
        int[] triangles = new int[(pointCount - 1) * 6];

        float now = Time.time;

        for (int i = 0; i < pointCount; i++)
        {
            TrailPoint point = points[i];

            float normalizedAge = Mathf.Clamp01((now - point.spawnTime) / pointLifetime);

            float widthMultiplier = widthOverLifetime.Evaluate(normalizedAge);
            float alpha = alphaOverLifetime.Evaluate(normalizedAge);

            Vector3 bladeCenter = (point.basePosition + point.tipPosition) * 0.5f;
            Vector3 baseOffset = point.basePosition - bladeCenter;
            Vector3 tipOffset = point.tipPosition - bladeCenter;

            Vector3 adjustedBase = bladeCenter + baseOffset * widthMultiplier;
            Vector3 adjustedTip = bladeCenter + tipOffset * widthMultiplier;

            int baseIndex = i * 2;
            int tipIndex = baseIndex + 1;

            vertices[baseIndex] = transform.InverseTransformPoint(adjustedBase);
            vertices[tipIndex] = transform.InverseTransformPoint(adjustedTip);

            float u = i / (float)(pointCount - 1);

            uvs[baseIndex] = new Vector2(u, 0f);
            uvs[tipIndex] = new Vector2(u, 1f);

            Color baseVertexColor = baseColor;
            Color tipVertexColor = tipColor;

            baseVertexColor.a *= alpha;
            tipVertexColor.a *= alpha;

            colors[baseIndex] = baseVertexColor;
            colors[tipIndex] = tipVertexColor;
        }

        int triangleIndex = 0;

        for (int i = 0; i < pointCount - 1; i++)
        {
            int currentBase = i * 2;
            int currentTip = currentBase + 1;
            int nextBase = currentBase + 2;
            int nextTip = currentBase + 3;

            triangles[triangleIndex++] = currentBase;
            triangles[triangleIndex++] = currentTip;
            triangles[triangleIndex++] = nextTip;

            triangles[triangleIndex++] = currentBase;
            triangles[triangleIndex++] = nextTip;
            triangles[triangleIndex++] = nextBase;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
    }
}
