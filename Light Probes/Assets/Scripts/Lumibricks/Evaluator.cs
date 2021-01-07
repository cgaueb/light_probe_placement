using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

class Evaluator {
    public enum LightProbesEvaluationType
    {
        FixedLow,
        FixedMedium,
        FixedHigh,
        Random
    }

    readonly List<Vector3> evaluationFixedDirections = new List<Vector3> { 
            // LOW  (6)
            new Vector3( 1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3( 0.0f, 1.0f, 0.0f),
            new Vector3( 0.0f,-1.0f, 0.0f),
            new Vector3( 0.0f, 0.0f, 1.0f),
            new Vector3( 0.0f, 0.0f,-1.0f),

            // MEDIUM (8)
            new Vector3( 1.0f, 1.0f, 1.0f),
            new Vector3( 1.0f, 1.0f,-1.0f),
            new Vector3(-1.0f, 1.0f, 1.0f),
            new Vector3(-1.0f, 1.0f,-1.0f),
            new Vector3( 1.0f,-1.0f, 1.0f),
            new Vector3( 1.0f,-1.0f,-1.0f),
            new Vector3(-1.0f,-1.0f, 1.0f),
            new Vector3(-1.0f,-1.0f,-1.0f),

            // HIGH (12)
            new Vector3( 0.0f, 1.0f, 1.0f),
            new Vector3( 0.0f, 1.0f,-1.0f),
            new Vector3( 0.0f,-1.0f, 1.0f),
            new Vector3( 0.0f,-1.0f,-1.0f),
            new Vector3( 1.0f, 0.0f, 1.0f),
            new Vector3( 1.0f, 0.0f,-1.0f),
            new Vector3( 1.0f, 1.0f, 0.0f),
            new Vector3( 1.0f,-1.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 1.0f),
            new Vector3(-1.0f, 0.0f,-1.0f),
            new Vector3(-1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f,-1.0f, 0.0f)
        };

    public int[] tetrahedralizeIndices;
    public Vector3[] tetrahedralizePositions;
    public Vector3[] evaluationResults;
    public int[] evaluationTetrahedron;
    public List<Vector3> evaluationRandomDirections = new List<Vector3>();
    public float evaluationTotal = 0.0f;
    public float evaluationTotalDecimated = 0.0f;
    public LightProbesEvaluationType EvaluationType { get; set; } = LightProbesEvaluationType.FixedHigh;

    public readonly int[] evaluationFixedCount = new int[] { 6, 14, 26 };
    public int evaluationRandomSamplingCount = 50;
    public int terminationMinLightProbes;
    public int terminationMaxLightProbes;

    public float terminationEvaluationError = 0.0f;

    public void Reset() {
        ResetLightProbeData();
        EvaluationType = LightProbesEvaluationType.FixedHigh;
    }

    public void ResetLightProbeData() {
        terminationMinLightProbes = 0;
        terminationMaxLightProbes = 0;
        ResetEvaluationData();
    }
    public void ResetEvaluationData() {
        tetrahedralizeIndices = null;
        tetrahedralizePositions = null;
        evaluationResults = null;
        evaluationTetrahedron = null;
        evaluationRandomDirections = new List<Vector3>();
        evaluationTotal = 0.0f;
        evaluationTotalDecimated = 0.0f;
    }

    public bool populateGUI_LightProbesEvaluated() {
        EvaluationType = (LightProbesEvaluationType)EditorGUILayout.EnumPopup(new GUIContent("Type:", "The probe evaluation method"), EvaluationType);
        if (EvaluationType == LightProbesEvaluationType.Random) {
            int prevCount = evaluationRandomSamplingCount;
            evaluationRandomSamplingCount = EditorGUILayout.IntField(new GUIContent("Number of Directions:", "The total number of uniform random sampled directions"), evaluationRandomSamplingCount);
            evaluationRandomSamplingCount = Mathf.Clamp(evaluationRandomSamplingCount, 1, 1000000);
            if (prevCount != evaluationRandomSamplingCount) {
                GenerateUniformSphereSampling();
            }
        } else {
            EditorGUILayout.LabelField(new GUIContent("Number of Directions:", "The total number of evaluation directions"), new GUIContent(evaluationFixedCount[(int)EvaluationType].ToString()));
        }

        GUILayout.BeginHorizontal();
        bool clickedEvaluateEvaluationPoints = GUILayout.Button(new GUIContent("Evaluate", "Evaluate evaluation points"), GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500));
        GUILayout.EndHorizontal();

        if (evaluationResults != null) {
            Vector3 evaluationRGB = ComputeCurrentCost();
            evaluationTotal = evaluationRGB.magnitude;
        }

        EditorGUILayout.LabelField(new GUIContent("Avg Irradiance (Before):", "The evaluation average irradiance target"), new GUIContent(evaluationTotal.ToString("0.00")));
        return clickedEvaluateEvaluationPoints;
    }

    public bool populateGUI_LightProbesDecimated() {
        terminationMinLightProbes = EditorGUILayout.IntSlider(new GUIContent("Minimum set:", "The minimum desired number of light probes"), terminationMinLightProbes, 1, terminationMaxLightProbes);
        terminationEvaluationError = EditorGUILayout.Slider(new GUIContent("Minimum error:", "The minimum desired evaluation percentage error"), terminationEvaluationError, 0.0f, 100.0f);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Decimate", "Decimate light probes"), GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Avg Irradiance (After): ", "The evaluation average irradiance after decimation"), new GUIContent(evaluationTotalDecimated.ToString("0.00")));
        return clickedDecimateLightProbes;
    }

    public Vector3 ComputeCurrentCost() {
        Vector3 evaluationRGB = new Vector3(0, 0, 0);
        for (int j = 0; j < evaluationResults.Length; j++) {
            evaluationRGB += evaluationResults[j];
        }
        evaluationRGB /= evaluationResults.Length;
        return evaluationRGB;
    }

    public void EvaluatePoints(SphericalHarmonicsL2[] bakedprobes, Vector3[] evalPositions) {
        int directionsCount;
        Vector3[] directions;

        if (EvaluationType == LightProbesEvaluationType.Random) {
            directionsCount = evaluationRandomSamplingCount;
            directions = evaluationRandomDirections.ToArray();
        } else {
            directionsCount = evaluationFixedCount[(int)EvaluationType];
            directions = evaluationFixedDirections.GetRange(0, directionsCount).ToArray();
        }
        Color[] evaluationResultsPerDir = new Color[directionsCount];

        int j = 0;
        evaluationResults = new Vector3[evalPositions.Length];
        foreach (Vector3 pos in evalPositions) {
            //SphericalHarmonicsL2 sh2;
            //LightProbes.GetInterpolatedProbe(pos, null, out sh2);
            SphericalHarmonicsL2 sh2 = new SphericalHarmonicsL2();
            if (evaluationTetrahedron[j] == -1) {
                sh2.Clear();
            } else {
                GetInterpolatedLightProbe(pos, evaluationTetrahedron[j], bakedprobes, ref sh2);
            }
            sh2.Evaluate(directions, evaluationResultsPerDir);

            Vector3 uniformSampledEvaluation = new Vector3(0, 0, 0);
            for (int i = 0; i < directionsCount; i++) {
                uniformSampledEvaluation.x += evaluationResultsPerDir[i].r;
                uniformSampledEvaluation.y += evaluationResultsPerDir[i].g;
                uniformSampledEvaluation.z += evaluationResultsPerDir[i].b;
            }
            uniformSampledEvaluation /= directionsCount;

            evaluationResults[j++] = uniformSampledEvaluation;
        }
    }
    public void DecimateBakedLightProbes(Vector3[] evaluatonPoints, LightProbes lightProbes, out bool[] decimatedPoints) {
        int decimatedIndex = 0;
        float decimatedCostMin = float.MaxValue;
        float[] decimatedCost = new float[lightProbes.count];

        List<Vector3> probePositionsDecimated;
        List<SphericalHarmonicsL2> bakedLightProbesDecimated;

        decimatedPoints = new bool[lightProbes.count];
        for (int i = 0; i < lightProbes.count; i++) {
            // 1. Remove Light Probe from Set
            {
                probePositionsDecimated = new List<Vector3>(lightProbes.positions);
                probePositionsDecimated.RemoveAt(i);
                bakedLightProbesDecimated = new List<SphericalHarmonicsL2>(LightmapSettings.lightProbes.bakedProbes);
                bakedLightProbesDecimated.RemoveAt(i);
            }

            //Debug.Log("DECIMATE - LP" + i + "-> Before " + lightProbes.count);
            //Debug.Log("DECIMATE - LP" + i + "-> After " + probePositionsDecimated.Count);

            // 2. Tetrahedralize New Light Probe Set
            {
                // Set Positions to LightProbeGroup
                //LightProbeGroup.probePositions = probePositionsDecimated.ToArray();
                // Tetrahedralize - NOT WORKING - REQUIRES BAKE PROCESS
                //LightProbes.Tetrahedralize();
                //Lightmapping.Tetrahedralize(LightProbeGroup.probePositions, out tetrahedralizeIndices, out tetrahedralizePositions);
            }

            // 3. Map Evaluation Points to New Light Probe Set 
            // Not Needed
            MapEvaluationPointsToLightProbes(probePositionsDecimated.ToArray(), evaluatonPoints);

            // 4. Evaluate
            EvaluatePoints(bakedLightProbesDecimated.ToArray(), evaluatonPoints);

            // 5. Compute Cost
            Vector3 evaluationRGB = ComputeCurrentCost();
            decimatedCost[i] = Mathf.Abs(evaluationRGB.magnitude - evaluationTotal);

            // 6. Find light probe with the minimum error
            if (decimatedCost[i] < decimatedCostMin) {
                //Debug.Log("DECIMATE - LP" + i + "-> Eval " + evaluationRGB.magnitude);
                //Debug.Log("DECIMATE - LP" + i + "-> Cost " + decimatedCost[i]);
                decimatedIndex = i;
                decimatedCostMin = decimatedCost[i];
                evaluationTotalDecimated = evaluationRGB.magnitude;
            }

            decimatedPoints[i] = false;
        }

        // 7. Remove light probe with the minimum error
        {
            decimatedPoints[decimatedIndex] = true;
        }
    }
    public void EvaluateVisibilityPoints(Vector3[] posIn, out bool[] unlitPoints) {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8; // Corresponds to "Environment"

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        //layerMask = ~layerMask;

        unlitPoints = new bool[posIn.Length];
        for (int i = 0; i < posIn.Length; i++) {
            unlitPoints[i] = false;

            // 1. Remove Outsize of Tetrahedralize
            if (evaluationTetrahedron[i] == -1) {
                unlitPoints[i] = true;
                continue;
            }

            // 2. Remove Occluded  (if cannot be seen by just one LP)
            Vector3[] tetrahedronPositions;
            GetTetrahedronPositions(evaluationTetrahedron[i], out tetrahedronPositions);

            foreach (Vector3 pos in tetrahedronPositions) {
                RaycastHit hit;
                Ray visRay = new Ray(posIn[i], pos - posIn[i]);
                if (Physics.Raycast(visRay, out hit, float.MaxValue, layerMask)) {
                    // Collision Found
                    // Debug.Log("EP" + i + "-> Collision with " + hit.point);

                    unlitPoints[i] = true;
                    break;
                }
            }
        }
    }

    void GetInterpolatedLightProbe(Vector3 evalPosition, int evalTetrahedron, SphericalHarmonicsL2[] bakedprobes, ref SphericalHarmonicsL2 sh2) {
        // GetTetrahedronSHs
        SphericalHarmonicsL2[] tetrahedronSH2 = new SphericalHarmonicsL2[4];
        tetrahedronSH2[0] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 0]];
        tetrahedronSH2[1] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 1]];
        tetrahedronSH2[2] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 2]];
        tetrahedronSH2[3] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 3]];

        // Get Barycentric Weights
        Vector3[] tetrahedronPositions;
        GetTetrahedronPositions(evalTetrahedron, out tetrahedronPositions);
        Vector4 weights = GetTetrahedronWeights(tetrahedronPositions, evalPosition);

        // Interpolate
        sh2 = weights.x * tetrahedronSH2[0] + weights.y * tetrahedronSH2[1] + weights.z * tetrahedronSH2[2] + weights.w * tetrahedronSH2[3];
    }
    public void MapEvaluationPointsToLightProbes(Vector3[] probePositions, Vector3[] evalPositions) {
        Lightmapping.Tetrahedralize(probePositions, out tetrahedralizeIndices, out tetrahedralizePositions);

        if (probePositions.Length != tetrahedralizePositions.Length) {
            Debug.LogError("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
        }

        Vector3[] tetrahedronPositions;
        evaluationTetrahedron = new int[evalPositions.Length];

        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Length; evaluationPositionIndex++) {
            Vector3 evaluationPosition = evalPositions[evaluationPositionIndex];

            // 1. Relate Evaluation Point with one Tetrahedron
            evaluationTetrahedron[evaluationPositionIndex] = -1;
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedralizeIndices.Length / 4; tetrahedronIndex++) {
                GetTetrahedronPositions(tetrahedronIndex, out tetrahedronPositions);
                if (IsInsideTetrahedronWeights(tetrahedronPositions, evaluationPosition)) {
                    evaluationTetrahedron[evaluationPositionIndex] = tetrahedronIndex;
                    break;
                }
            }
            if (evaluationTetrahedron[evaluationPositionIndex] < 0) {
                Debug.LogWarning("Could not map EP " + evaluationPositionIndex.ToString() + ": " + evaluationPosition.ToString() + " to any tetrahedron");
            } else {
                Debug.Log("Mapped EP " + evaluationPositionIndex.ToString() + ": " + evaluationPosition.ToString());
            }
        }
    }
    public void EvaluateBakedLightProbes(SphericalHarmonicsL2[] bakedProbes, out bool[] unlitPoints) {
        SphericalHarmonicsL2 shZero = new SphericalHarmonicsL2();
        shZero.Clear();

        int j = 0;
        unlitPoints = new bool[bakedProbes.Length];
        foreach (SphericalHarmonicsL2 sh2 in bakedProbes) {
            unlitPoints[j++] = sh2.Equals(shZero);
        }
    }
    public void GenerateUniformSphereSampling() {
        evaluationRandomDirections.Clear();
        for (int i = 0; i < evaluationRandomSamplingCount; i++) {
            float z = 2.0f * UnityEngine.Random.Range(0.0f, 1.0f) - 1.0f;
            float phi = 2.0f * Mathf.PI * UnityEngine.Random.Range(0.0f, 1.0f);
            float r = Mathf.Sqrt(1.0f - z * z);
            float x = r * Mathf.Cos(phi);
            float y = r * Mathf.Sin(phi);

            evaluationRandomDirections.Add(new Vector3(x, y, z));
        }
    }
    void GetTetrahedronPositions(int j, out Vector3[] tetrahedronPositions) {
        tetrahedronPositions = new Vector3[4];
        tetrahedronPositions[0] = tetrahedralizePositions[tetrahedralizeIndices[j * 4 + 0]];
        tetrahedronPositions[1] = tetrahedralizePositions[tetrahedralizeIndices[j * 4 + 1]];
        tetrahedronPositions[2] = tetrahedralizePositions[tetrahedralizeIndices[j * 4 + 2]];
        tetrahedronPositions[3] = tetrahedralizePositions[tetrahedralizeIndices[j * 4 + 3]];
    }
    bool PointPlaneSameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p) {
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);
        float dotV4 = Vector3.Dot(normal, v4 - v1);
        float dotP = Vector3.Dot(normal, p - v1);
        return Mathf.Sign(dotV4) == Mathf.Sign(dotP);
    }

    bool IsInsideTetrahedronPlanes(Vector3[] v, Vector3 p) {
        return PointPlaneSameSide(v[0], v[1], v[2], v[3], p) &&
                PointPlaneSameSide(v[1], v[2], v[3], v[0], p) &&
                PointPlaneSameSide(v[2], v[3], v[0], v[1], p) &&
                PointPlaneSameSide(v[3], v[0], v[1], v[2], p);
    }

    Vector4 GetTetrahedronWeights(Vector3[] v, Vector3 p) {
        Matrix4x4 mat = Matrix4x4.identity;
        mat.SetColumn(0, v[0] - v[3]);
        mat.SetColumn(1, v[1] - v[3]);
        mat.SetColumn(2, v[2] - v[3]);
        Vector4 v_new = p - v[3];
        Vector4 weights = mat.inverse * v_new;
        weights.w = 1 - weights.x - weights.y - weights.z;
        return weights;
    }
    bool IsInsideTetrahedronWeights(Vector3[] v, Vector3 p) {
        Vector4 weights = GetTetrahedronWeights(v, p);
        return weights.x >= 0 && weights.y >= 0 && weights.z >= 0 && weights.w >= 0;
    }
}
