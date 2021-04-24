using UnityEngine;
using System.Collections.Generic;

class MathUtilities
{
    private static float epsilon = 0.000001f;

    public static Vector3 GetCentroid(List<Vector3> positions) {
        Vector3 centroid = new Vector3();
        for (int lpIndex = 0; lpIndex < positions.Count; lpIndex++)
            centroid += positions[lpIndex];
        centroid /= positions.Count;
        return centroid;
    }

    // based on Morgan McGuire's sample: https://graphicscodex.com/Sample2-RayTriangleIntersection.pdf
    public static bool IntersectRay_Triangle(Vector3 ray_origin, Vector3 ray_direction,
                                        Vector3 v0, Vector3 v1, Vector3 v2,
                                        out float t,
                                        out Vector3 barycentric) {
        // find vectors for two edges sharing v0
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        Vector3 q = Vector3.Cross(ray_direction, edge2);
        float a = Vector3.Dot(edge1, q);

        t = 0;
        barycentric = Vector3.zero;

        // nearly parallel
        if (Mathf.Abs(a) < epsilon) {
            return false;
        }

        Vector3 s = (ray_origin - v0) / a;
        Vector3 r = Vector3.Cross(s, edge1);
        barycentric.x = Vector3.Dot(s, q);
        barycentric.y = Vector3.Dot(r, ray_direction);
        barycentric.z = 1.0f - barycentric.x - barycentric.y;

        if (barycentric.x > -epsilon && barycentric.y > -epsilon && barycentric.z > -epsilon) {
            t = Vector3.Dot(edge2, r);
            return t >= -epsilon;
        }
        return false;
    }

    public static void GenerateUniformSphereSampling(out List<Vector3> directions, int numDirections) {
        directions = new List<Vector3>(numDirections);
        for (int i = 0; i < numDirections; i++) {
            Vector2 r = new Vector2(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
            float phi = r.x * 2.0f * Mathf.PI;
            float cosTheta = 1.0f - 2.0f * r.y;
            float sinTheta = Mathf.Sqrt(1.0f - cosTheta * cosTheta);
            Vector3 vec = new Vector3(Mathf.Cos(phi) * sinTheta, Mathf.Sin(phi) * sinTheta, cosTheta);
            vec.Normalize();
            directions.Add(vec);
        }
    }
    public static bool PointPlaneSameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p) {
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);
        float dotV4 = Vector3.Dot(normal, v4 - v1);
        float dotP = Vector3.Dot(normal, p - v1);
        return Mathf.Sign(dotV4) == Mathf.Sign(dotP);
    }

    public static bool IsInsideTetrahedronPlanes(Vector3[] v, Vector3 p) {
        return PointPlaneSameSide(v[0], v[1], v[2], v[3], p) &&
                PointPlaneSameSide(v[1], v[2], v[3], v[0], p) &&
                PointPlaneSameSide(v[2], v[3], v[0], v[1], p) &&
                PointPlaneSameSide(v[3], v[0], v[1], v[2], p);
    }

    public static Vector4 GetTetrahedronWeights_SLOW(Vector3[] v, Vector3 p) {
        Matrix4x4 mat = Matrix4x4.identity;
        mat.SetColumn(0, v[0] - v[3]);
        mat.SetColumn(1, v[1] - v[3]);
        mat.SetColumn(2, v[2] - v[3]);
        Vector4 v_new = p - v[3];
        Vector4 weights = mat.inverse * v_new;
        weights.w = 1 - weights.x - weights.y - weights.z;
        return weights;
    }

    private static float ScTP(Vector3 a, Vector3 b, Vector3 c) {
        return Vector3.Dot(a, Vector3.Cross(b, c));
    }

    public static Vector4 GetTetrahedronWeights(Vector3[] v, Vector3 p) {
        Vector3 vap = p - v[0];
        Vector3 vbp = p - v[1];
        Vector3 vab = v[1] - v[0];
        Vector3 vac = v[2] - v[0];
        Vector3 vad = v[3] - v[0];
        Vector3 vbc = v[2] - v[1];
        Vector3 vbd = v[3] - v[1];

        // ScTP computes the scalar triple product
        float va6 = ScTP(vbp, vbd, vbc);
        float vb6 = ScTP(vap, vac, vad);
        float vc6 = ScTP(vap, vad, vab);
        float v6 = 1 / ScTP(vab, vac, vad);

        float w1 = va6 * v6;
        float w2 = vb6 * v6;
        float w3 = vc6 * v6;
        float w4 = 1 - w1 - w2 - w3;

        return new Vector4(w1, w2, w3, w4);
    }

    public static bool IsInsideTetrahedronWeights(Vector3[] v, Vector3 p, out Vector4 weights) {
        weights = GetTetrahedronWeights(v, p);
        return weights.x >= -epsilon && weights.y >= -epsilon && weights.z >= -epsilon && weights.w >= -epsilon;
    }
}
