#if USE_BURST
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public static class CatmullRom
{
    public static float3 Sample(float3 c0, float3 p0, float3 p1, float3 c1, float t)
    {
        const float alpha = 0.5f;
        Sample(ref c0, ref p0, ref p1, ref c1, alpha, t, out var result);
        return result;
    }

    [BurstCompile]
    private static void Sample(ref float3 c0, ref float3 p0, ref float3 p1, ref float3 c1, float a, float t, out float3 result)
    {
        var C0 = float4(c0, 0f);
        var P0 = float4(p0, 0f);
        var P1 = float4(p1, 0f);
        var C1 = float4(c1, 0f);
        a = clamp(a, 0f, 1f);

        var op = mul(
            float4x4(C0, P0, P1, C1),
            float4x4(
                0, -a, 2 * a, -a,
                1, 0, a - 3, 2 - a,
                0, a, 3 - 2 * a, a - 2,
                0, 0, -a, a
            )
        );
        float t2 = t * t;
        float t3 = t2 * t;
        result = mul(op, new float4(1, t, t2, t3)).xyz;
    }
}
#else
using UnityEngine;

public static class CatmullRom
{
    public static Vector3 Sample(Vector3 C0, Vector3 P0, Vector3 P1, Vector3 C1, float t)
    {
        const float m00 = 0f, m01 = -0.5f, m02 = 1f, m03 = -0.5f,
            m10 = 1f, m11 = 0f, m12 = -2.5f, m13 = 1.5f,
            m20 = 0f, m21 = 0.5f, m22 = 2f, m23 = -1.5f,
            m30 = 0f, m31 = 0f, m32 = -0.5f, m33 = 0.5f;

        float X0 = C0.x * m00 + P0.x * m10 + P1.x * m20 + C1.x * m30;
        float X1 = C0.x * m01 + P0.x * m11 + P1.x * m21 + C1.x * m31;
        float X2 = C0.x * m02 + P0.x * m12 + P1.x * m22 + C1.x * m32;
        float X3 = C0.x * m03 + P0.x * m13 + P1.x * m23 + C1.x * m33;
        float Y0 = C0.y * m00 + P0.y * m10 + P1.y * m20 + C1.y * m30;
        float Y1 = C0.y * m01 + P0.y * m11 + P1.y * m21 + C1.y * m31;
        float Y2 = C0.y * m02 + P0.y * m12 + P1.y * m22 + C1.y * m32;
        float Y3 = C0.y * m03 + P0.y * m13 + P1.y * m23 + C1.y * m33;
        float Z0 = C0.z * m00 + P0.z * m10 + P1.z * m20 + C1.z * m30;
        float Z1 = C0.z * m01 + P0.z * m11 + P1.z * m21 + C1.z * m31;
        float Z2 = C0.z * m02 + P0.z * m12 + P1.z * m22 + C1.z * m32;
        float Z3 = C0.z * m03 + P0.z * m13 + P1.z * m23 + C1.z * m33;

        return new Vector3(
            X0 + t * (X1 + t * (X2 + t * X3)),
            Y0 + t * (Y1 + t * (Y2 + t * Y3)),
            Z0 + t * (Z1 + t * (Z2 + t * Z3))
        );
    }
}
#endif
