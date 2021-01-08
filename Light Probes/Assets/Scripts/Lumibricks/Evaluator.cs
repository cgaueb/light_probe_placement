using System.Collections.Generic;
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

    public enum LightProbesSolver
    {
        Absolute,
        LeastSquares
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
    public int[] evaluationTetrahedron;
    public List<Vector3> evaluationRandomDirections = new List<Vector3>();
    public List<Vector3> evaluationResults; 
    public float evaluationError = 0.0f;
    public float evaluationTotal = 0.0f;
    public float evaluationTotalDecimated = 0.0f;
    public LightProbesEvaluationType EvaluationType { get; set; } = LightProbesEvaluationType.FixedHigh;
    public LightProbesSolver EvaluationSolver { get; set; } = LightProbesSolver.Absolute;
    private SolverCallback EvaluationSolverCallback = null;

    public readonly int[] evaluationFixedCount = new int[] { 6, 14, 26 };
    public int evaluationRandomSamplingCount = 50;
    public int terminationMinLightProbes;
    public int terminationMaxLightProbes;

    public float terminationEvaluationError = 0.0f;

    public void Reset(int probesCount) {
        ResetLightProbeData(probesCount);
        EvaluationType = LightProbesEvaluationType.FixedHigh;
    }

    public void ResetLightProbeData(int probesCount) {
        terminationMinLightProbes = probesCount-1;
        terminationMaxLightProbes = probesCount-1;
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
            Vector3 evaluationRGB = ComputeCurrentValue(evaluationResults);
            evaluationTotal = RGBToFloat(evaluationRGB);
        }

        EditorGUILayout.LabelField(new GUIContent("Avg Irradiance (Before):", "The evaluation average irradiance target"), new GUIContent(evaluationTotal.ToString("0.00")));
        return clickedEvaluateEvaluationPoints;
    }

    public bool populateGUI_LightProbesDecimated() {
        EvaluationSolver = (LightProbesSolver)EditorGUILayout.EnumPopup(new GUIContent("Solver:", "The solver method"), EvaluationSolver);
        EvaluationSolverCallback = GetSolverCallback();
        terminationMinLightProbes  = EditorGUILayout.IntSlider(new GUIContent("Minimum set:", "The minimum desired number of light probes"), terminationMinLightProbes, 4, terminationMaxLightProbes);
        terminationEvaluationError = EditorGUILayout.Slider(new GUIContent("Minimum error (unused):", "The minimum desired evaluation percentage error"), terminationEvaluationError, 0.0f, 100.0f);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Decimate", "Decimate light probes"), GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Avg Irradiance (After): ", "The evaluation average irradiance after decimation"), new GUIContent(evaluationTotalDecimated.ToString("0.00")));
        EditorGUILayout.LabelField(new GUIContent("Error: ", "The resulting error compared to the original estimation"), new GUIContent(evaluationError.ToString("0.00")));
        return clickedDecimateLightProbes;
    }

    public Vector3 ComputeCurrentValue(List<Vector3> currentEvaluationResults) {
        Vector3 evaluationRGB = new Vector3(0, 0, 0);
        for (int j = 0; j < currentEvaluationResults.Count; j++) {
            evaluationRGB += currentEvaluationResults[j];
        }
        evaluationRGB /= currentEvaluationResults.Count;
        return evaluationRGB;
    }
    public float RGBToFloat(Vector3 value) {
        return (value.x + value.y + value.z) * 0.333f;
    }
    public float SquareError(Vector3 value, Vector3 reference) {
        float res = RGBToFloat(value) - RGBToFloat(reference);
        return res * res;
    }

    public float AbsoluteError(Vector3 value, Vector3 reference) {
        return Mathf.Abs(RGBToFloat(value) - RGBToFloat(reference));
    }

    private delegate float SolverCallback(Vector3 value, Vector3 reference);

    private SolverCallback GetSolverCallback() {
        if (EvaluationSolver == LightProbesSolver.Absolute) {
            return AbsoluteError;
        } else if (EvaluationSolver == LightProbesSolver.LeastSquares) {
            return SquareError;
        }
        return null;
    }

    public float ComputeCurrentCost(List<Vector3> estimates, List<Vector3> reference) {
        float cost = 0.0f;
        for (int j = 0; j < estimates.Count; j++) {
            cost += EvaluationSolverCallback(estimates[j], reference[j]);
        }
        cost /= estimates.Count;
        return cost;
    }

    public void EvaluateReferencePoints(SphericalHarmonicsL2[] bakedprobes, Vector3[] evalPositions) {
        evaluationResults = EvaluatePoints(bakedprobes, evalPositions);
    }

    public List<Vector3> EvaluatePoints(SphericalHarmonicsL2[] bakedprobes, Vector3[] evalPositions) {
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
        List<Vector3> currentEvaluationResults = new List<Vector3>(evalPositions.Length);
        foreach (Vector3 pos in evalPositions) {
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
            currentEvaluationResults.Add(uniformSampledEvaluation);
            j++;
        }
        return currentEvaluationResults;
    }
    public List<Vector3> DecimateBakedLightProbes(Vector3[] evaluationPoints, List<Vector3> posIn, List<SphericalHarmonicsL2> bakedProbes) {
        // TODO: add iterate
        // TODO: optimize, e.g. stochastic
        // TODO: modify cost function
        // TODO: verify result
        // TODO: potentially add multiple cost functions and error metrics
        // TODO: finalize plugin/UI software engineering

        // store the final result here
        List<Vector3>              finalPositionsDecimated   = new List<Vector3>(posIn);
        List<SphericalHarmonicsL2> finalLightProbesDecimated = new List<SphericalHarmonicsL2>(bakedProbes);

        float   maxError                        = 0.1f;
        float   currentEvaluationError          = 0.0f;
        int     iteration                       = 0;
        int     maxIterations                   = finalPositionsDecimated.Count - terminationMinLightProbes;
        bool    is_stochastic                   = false;

        while (currentEvaluationError < maxError && iteration < maxIterations) {

            // remove the Probe which contributes "the least" to the reference
            // Optimize: don't iterate against all every time
            // Step 1: Ideally use a stochastic approach, i.e. remove random N at each iteration. Done
            // Step 2: Only evaluate points in the vicinity of the probe. TODO:

            int     decimatedIndex      = -1;
            float   decimatedCostMin    = float.MaxValue;
            
            int     random_samples_each_iteration   = (is_stochastic) ? Mathf.Min(10, finalPositionsDecimated.Count) : finalPositionsDecimated.Count;
            for (int i = 0; i < random_samples_each_iteration; i++) {
                // 1. Remove Light Probe from Set
                List<Vector3>               probePositionsDecimated   = new List<Vector3>(finalPositionsDecimated);
                List<SphericalHarmonicsL2>  bakedLightProbesDecimated = new List<SphericalHarmonicsL2>(finalLightProbesDecimated);
                
                int random_index = (is_stochastic) ? Random.Range(0, random_samples_each_iteration) : i;

                probePositionsDecimated.RemoveAt(random_index);
                bakedLightProbesDecimated.RemoveAt(random_index);

                // 2. Map Evaluation Points to New Light Probe Set 
                MapEvaluationPointsToLightProbes(probePositionsDecimated.ToArray(), evaluationPoints);

                // 3. Evaluate
                List<Vector3> currentEvaluationResults = EvaluatePoints(bakedLightProbesDecimated.ToArray(), evaluationPoints);

                // 4. Compute Cost of current configuration
                float decimatedCost = ComputeCurrentCost(currentEvaluationResults, evaluationResults);

                // 5. Find light probe with the minimum error
                if (decimatedCost < decimatedCostMin) {
                    decimatedIndex           = i;
                    decimatedCostMin         = decimatedCost;
                    evaluationTotalDecimated = RGBToFloat(ComputeCurrentValue(currentEvaluationResults));
                }
            }

            if (decimatedIndex == -1) {
                Debug.LogError("No probe found during the iteration");
            }

            // 6. Remove light probe with the minimum error
            finalPositionsDecimated.RemoveAt(decimatedIndex);
            finalLightProbesDecimated.RemoveAt(decimatedIndex);
            currentEvaluationError = decimatedCostMin;
            
            Debug.Log("Iteration: " + iteration.ToString() + ". Cost: " + decimatedCostMin.ToString() + ". Removed probe: " + decimatedIndex.ToString());
            ++iteration;
        }
        evaluationError = currentEvaluationError;

        return finalPositionsDecimated;
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

    public int MapEvaluationPointsToLightProbes(Vector3[] probePositions, Vector3[] evalPositions) {
        Lightmapping.Tetrahedralize(probePositions, out tetrahedralizeIndices, out tetrahedralizePositions);

        if (probePositions.Length != tetrahedralizePositions.Length) {
            Debug.LogWarning("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
        }

        int mapped = 0;
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
                    mapped++;
                    break;
                }
            }
            if (evaluationTetrahedron[evaluationPositionIndex] < 0) {
                //Debug.LogWarning("Could not map EP " + evaluationPositionIndex.ToString() + ": " + evaluationPosition.ToString() + " to any tetrahedron");
            } else {
                //Debug.Log("Mapped EP " + evaluationPositionIndex.ToString() + ": " + evaluationPosition.ToString());
            }
        }
        return mapped;
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
