using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

public class StickWeaponTrailEffect : MonoBehaviour
{
    public struct Stickshot
    {
        public readonly float timeStamp;
        public readonly Vector3 top;
        public readonly Vector3 bottom;

        public Vector3 center => (top + bottom) * 0.5f;
        public Vector3 radius => (top - bottom) * 0.5f;

        public Stickshot(float timeStamp, Vector3 top, Vector3 bottom)
        {
            this.timeStamp = timeStamp;
            this.top = top;
            this.bottom = bottom;
        }
    }

    private const int CountOfPointOnStick = 3;

    private GrowingRingBuffer<Stickshot> m_StickshotBuffer;
    private GrowingRingBuffer<Stickshot> stickshotBuffer => m_StickshotBuffer ?? (m_StickshotBuffer = new GrowingRingBuffer<Stickshot>(0));

    [SerializeField] private float m_Duration;
    public float duration
    {
        get => m_Duration;
        set => m_Duration = Math.Max(0f, value);
    }

    [Range(1f, 30f)]
    [SerializeField] private float m_DegreeResolution = 1f;
    public float degreeResolution
    {
        get => m_DegreeResolution;
        set => m_DegreeResolution = Math.Max(1f, value);
    }

    [SerializeField] private Transform m_Top;
    public Transform top
    {
        get => m_Top;
        set => m_Top = value;
    }

    [SerializeField] private Transform m_Bottom;
    public Transform bottom
    {
        get => m_Bottom;
        set => m_Bottom = value;
    }

    [SerializeField] private Material m_Material;
    public Material material
    {
        get => m_Material;
        set => m_Material = value;
    }

    private Mesh m_Mesh = null;
    public Mesh mesh
    {
        get
        {
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh {name = "Trail Effect"};
                m_Mesh.MarkDynamic();
            }
            return m_Mesh;
        }
    }

    private void LateUpdate()
    {
        Profiler.BeginSample(nameof(LateUpdate));

        UpdateFrameBuffer();

        if (stickshotBuffer.Count > 1)
        {
            var verticesMaxCount = stickshotBuffer.Count * Mathf.CeilToInt(180f / degreeResolution);
            var vertices = new NativeArray<Vector3>(verticesMaxCount * CountOfPointOnStick, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var verticesCount = UpdateSegments(vertices);
            UpdateMesh(vertices, verticesCount);
            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, gameObject.layer, null, 0, null, false, false, false);
            vertices.Dispose();
        }
        else
        {
            mesh.Clear(true);
        }

        Profiler.EndSample();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var color = Gizmos.color;
        var vertices = mesh.vertices;
        var interpolatedCount = vertices.Length / CountOfPointOnStick;
        if (interpolatedCount > stickshotBuffer.Count)
        {
            Gizmos.color = Color.gray;
            for (int i = 0; i < interpolatedCount; ++i)
            {
                int baseIndex = i * CountOfPointOnStick;
                Gizmos.DrawLine(vertices[baseIndex], vertices[baseIndex + 1]);
                Gizmos.DrawLine(vertices[baseIndex + 1], vertices[baseIndex + 2]);
            }
        }
        Gizmos.color = Color.black;
        for (int i = 0; i < stickshotBuffer.Count; ++i)
        {
            var stickshot = stickshotBuffer[i];
            Gizmos.DrawLine(stickshot.bottom, stickshot.top);
        }
        Gizmos.color = color;
    }
#endif

    private void OnDestroy()
    {
        if (m_Mesh != null)
        {
            Destroy(m_Mesh);
        }
    }

    private void OnDisable()
    {
        m_StickshotBuffer?.Clear();
    }

    private void UpdateFrameBuffer()
    {
        Profiler.BeginSample(nameof(UpdateFrameBuffer));
        var time = Time.time;

        while (!stickshotBuffer.IsEmpty)
        {
            if (time < stickshotBuffer[0].timeStamp + duration)
                break;

            stickshotBuffer.Pop();
        }

        stickshotBuffer.Add(new Stickshot(time, top.position, bottom.position));
        Profiler.EndSample();
    }

    private int UpdateSegments(NativeArray<Vector3> vertices)
    {
        Profiler.BeginSample(nameof(UpdateSegments));
        int count = stickshotBuffer.Count;
        int verticesCount = 0;
        for (int i = 0, j = 1; j < count; ++i, ++j)
        {
            var stickshotP0 = stickshotBuffer[i];
            var stickshotP1 = stickshotBuffer[j];
            var stickshotC0 = i > 0 ? stickshotBuffer[i - 1] : stickshotP0;
            var stickshotC1 = j + 1 < count ? stickshotBuffer[j + 1] : stickshotP1;
            Vector3 centerP0 = stickshotP0.center, radiusP0 = stickshotP0.radius;
            Vector3 centerP1 = stickshotP1.center, radiusP1 = stickshotP1.radius;
            Vector3 centerC0 = stickshotC0.center, radiusC0 = stickshotC0.radius;
            Vector3 centerC1 = stickshotC1.center, radiusC1 = stickshotC1.radius;
            var deltaDegrees = Math.Max(
                Vector3.Angle(centerP1 - centerC0, centerC1 - centerP0),
                Vector3.Angle(radiusP0, radiusP1)
            );
            var interpolations = Mathf.CeilToInt(deltaDegrees / degreeResolution) + 1;
            if (interpolations > 1)
            {
                for (int k = 0; k < interpolations; ++k)
                {
                    var t = (float)k / interpolations;
                    Vector3 center = CatmullRom.Sample(centerC0, centerP0, centerP1, centerC1, t);
                    Vector3 radius = CatmullRom.Sample(radiusC0, radiusP0, radiusP1, radiusC1, t);
                    vertices[verticesCount++] = center - radius; // bottom
                    vertices[verticesCount++] = center; // center
                    vertices[verticesCount++] = center + radius; // top
                }
            }
            else
            {
                var center = stickshotP0.center;
                var radius = stickshotP0.radius;
                vertices[verticesCount++] = center - radius; // bottom
                vertices[verticesCount++] = center; // center
                vertices[verticesCount++] = center + radius; // top
            }
        }

        {
            var stickshot = stickshotBuffer[count - 1];
            var center = stickshot.center;
            var radius = stickshot.radius;
            vertices[verticesCount++] = center - radius; // bottom
            vertices[verticesCount++] = center; // center
            vertices[verticesCount++] = center + radius; // top
        }
        Profiler.EndSample();

        return verticesCount;
    }

    private void UpdateMesh(NativeArray<Vector3> vertices, int verticesCount)
    {
        int stickshotsCount = verticesCount / CountOfPointOnStick;
        int segmentsCount = stickshotsCount - 1;
        int indicesCountPerSegment = (CountOfPointOnStick - 1) * 6;
        var indices = new NativeArray<int>(segmentsCount * indicesCountPerSegment, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        var uv0 = new NativeArray<Vector2>(verticesCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        Profiler.BeginSample(nameof(UpdateMesh));
        for (int i = 0; i < segmentsCount; ++i)
        {
            int leftBottom = i * CountOfPointOnStick;
            int leftCenter = i * CountOfPointOnStick + 1;
            int leftTop = i * CountOfPointOnStick + 2;
            int rightBottom = (i + 1) * CountOfPointOnStick;
            int rightCenter = (i + 1) * CountOfPointOnStick + 1;
            int rightTop = (i + 1) * CountOfPointOnStick + 2;

            int baseIndex = i * indicesCountPerSegment;
            indices[baseIndex++] = leftBottom;
            indices[baseIndex++] = leftCenter;
            indices[baseIndex++] = rightCenter;
            indices[baseIndex++] = rightCenter;
            indices[baseIndex++] = rightBottom;
            indices[baseIndex++] = leftBottom;
            indices[baseIndex++] = leftCenter;
            indices[baseIndex++] = leftTop;
            indices[baseIndex++] = rightTop;
            indices[baseIndex++] = rightTop;
            indices[baseIndex++] = rightCenter;
            indices[baseIndex] = leftCenter;
        }

        for (int i = 0; i < stickshotsCount; ++i)
        {
            float u = 1f - (float)i / segmentsCount;
            uv0[i * CountOfPointOnStick] = new Vector2(u, 0f);
            uv0[i * CountOfPointOnStick + 1] = new Vector2(u, 0.5f);
            uv0[i * CountOfPointOnStick + 2] = new Vector2(u, 1f);
        }

        mesh.Clear(true);
        mesh.SetVertices(vertices, 0, verticesCount);
        mesh.SetIndices(indices, 0, segmentsCount * indicesCountPerSegment, MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uv0, 0, verticesCount);
        Profiler.EndSample();

        indices.Dispose();
        uv0.Dispose();
    }
}
