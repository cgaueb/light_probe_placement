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

    // sampling
    public readonly int[] evaluationFixedCount = new int[] { 6, 14, 26 };
    public int evaluationRandomSamplingCount = 50;
    public List<Vector3> evaluationRandomDirections = new List<Vector3>();

    // tetrahedral
    public int[] tetrahedralizeIndices;
    public Vector3[] tetrahedralizePositions;
    public List<int> evaluationTetrahedron;

    // evaluation
    public List<Vector3> evaluationResults; 
    public float evaluationError = 0.0f;
    public float evaluationTotal = 0.0f;
    public float evaluationTotalDecimated = 0.0f;
    public float terminationEvaluationError = 0.0f;
    public int terminationMinLightProbes = 0;
    public int terminationMaxLightProbes = 0;

    public LightProbesEvaluationType EvaluationType { get; set; } = LightProbesEvaluationType.FixedHigh;
    public LightProbesSolver EvaluationSolver { get; set; } = LightProbesSolver.Absolute;
    private SolverCallback EvaluationSolverCallback = null;

    GUILayoutOption[] defaultOption = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500) };

    public void Reset(int probesCount) {
        ResetLightProbeData(probesCount);
        EvaluationType = LightProbesEvaluationType.FixedHigh;
    }

    public void ResetLightProbeData(int probesCount) {
        terminationMinLightProbes = probesCount-1;
        terminationMaxLightProbes = terminationMinLightProbes;
        tetrahedralizeIndices = null;
        tetrahedralizePositions = null;
        ResetEvaluationData();
    }

    public void ResetEvaluationData() {
        evaluationResults = null;
        evaluationTetrahedron = null;
        evaluationRandomDirections = new List<Vector3>();
        evaluationTotal = 0.0f;
        evaluationTotalDecimated = 0.0f;
    }
    private void populateGUI_EvaluateDirections() {
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
    }
    public bool populateGUI_LightProbesEvaluated() {
        populateGUI_EvaluateDirections();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedEvaluateEvaluationPoints = GUILayout.Button(new GUIContent("Evaluate", "Evaluate evaluation points"), defaultOption);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (evaluationResults != null) {
            Vector3 evaluationRGB = ComputeCurrentValue(evaluationResults);
            evaluationTotal = RGBToFloat(evaluationRGB);
        }

        EditorGUILayout.LabelField(new GUIContent("Avg Irradiance (Before):", "The evaluation average irradiance target"), new GUIContent(evaluationTotal.ToString("0.00")));

        return clickedEvaluateEvaluationPoints;
    }

    public bool populateGUI_LightProbesDecimated(GeneratorInterface currentLightProbesGenerator, GeneratorInterface currentEvaluationPointsGenerator, bool executeAll) {
        if (executeAll) {
            populateGUI_EvaluateDirections();
        }
        EvaluationSolver = (LightProbesSolver)EditorGUILayout.EnumPopup(new GUIContent("Solver:", "The solver method"), EvaluationSolver);
        EvaluationSolverCallback = GetSolverCallback();
        terminationMinLightProbes  = EditorGUILayout.IntSlider(new GUIContent("Minimum set:", "The minimum desired number of light probes"), terminationMinLightProbes, 4, terminationMaxLightProbes);
        terminationEvaluationError = EditorGUILayout.Slider(new GUIContent("Minimum error (unused):", "The minimum desired evaluation percentage error"), terminationEvaluationError, 0.0f, 100.0f);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedDecimateLightProbes = false;
        if (executeAll) {
            clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Run Optimizer (needs Bake first)", "Optimizes light probes"), defaultOption);
        } else {
            clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Decimate", "Decimate light probes"), defaultOption);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Error: ", "The resulting error compared to the original estimation"), new GUIContent(evaluationError.ToString("0.00")));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Avg Irrad (Before):", "The evaluation average irradiance target"), new GUIContent(evaluationTotal.ToString("0.00")));
        EditorGUILayout.LabelField(new GUIContent("Avg Irrad (After): ", "The evaluation average irradiance after decimation"), new GUIContent(evaluationTotalDecimated.ToString("0.00")));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("LP Before:", "The total number of light probes before Simplification"), new GUIContent(currentLightProbesGenerator.TotalNumProbes.ToString()));
        EditorGUILayout.LabelField(new GUIContent("LP After :", "The total number of light probes after  Simplification"), new GUIContent(currentLightProbesGenerator.TotalNumProbesSimplified.ToString()));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("EP Before:", "The total number of evaluation points before Simplification"), new GUIContent(currentEvaluationPointsGenerator.TotalNumProbes.ToString()));
        EditorGUILayout.LabelField(new GUIContent("EP After :", "The total number of evaluation points after  Simplification"), new GUIContent(currentEvaluationPointsGenerator.TotalNumProbesSimplified.ToString()));
        EditorGUILayout.EndHorizontal();

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

    public void EvaluateReferencePoints(List<SphericalHarmonicsL2> bakedprobes, List<Vector3> evalPositions) {
        evaluationResults = EvaluatePoints(bakedprobes, evalPositions);
    }

    public List<Vector3> EvaluatePoints(List<SphericalHarmonicsL2> bakedprobes, List<Vector3> evalPositions) {
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
        List<Vector3> currentEvaluationResults = new List<Vector3>(evalPositions.Count);
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
    public List<Vector3> DecimateBakedLightProbes(List<Vector3> evaluationPoints, List<Vector3> posIn, List<SphericalHarmonicsL2> bakedProbes) {
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

            long totalms = 0;
            long step1 = 0;
            long step1b = 0;
            long step2 = 0;
            long step3 = 0;
            long step4 = 0;
            long step5 = 0;
            System.Diagnostics.Stopwatch stopwatch;
            int     random_samples_each_iteration   = (is_stochastic) ? Mathf.Min(10, finalPositionsDecimated.Count) : finalPositionsDecimated.Count;
            Vector3 last_position_removed = new Vector3();
            SphericalHarmonicsL2 last_SH_removed = new SphericalHarmonicsL2();
            for (int i = 0; i < random_samples_each_iteration; i++) {
                // 1. Remove Light Probe from Set
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                //List<Vector3>               probePositionsDecimated   = new List<Vector3>(finalPositionsDecimated);
                //List<SphericalHarmonicsL2>  bakedLightProbesDecimated = new List<SphericalHarmonicsL2>(finalLightProbesDecimated);
                stopwatch.Stop();
                step1 += stopwatch.ElapsedMilliseconds; 
                totalms += stopwatch.ElapsedMilliseconds;

                int random_index = (is_stochastic) ? Random.Range(0, random_samples_each_iteration) : i;

                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                last_position_removed = finalPositionsDecimated[random_index];
                finalPositionsDecimated.RemoveAt(random_index);
                last_SH_removed = finalLightProbesDecimated[random_index];
                finalLightProbesDecimated.RemoveAt(random_index);
                stopwatch.Stop();
                step1b += stopwatch.ElapsedMilliseconds;
                totalms += stopwatch.ElapsedMilliseconds;

                // 2. Map Evaluation Points to New Light Probe Set 
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                MapEvaluationPointsToLightProbes(finalPositionsDecimated, evaluationPoints);
                stopwatch.Stop();
                step2 += stopwatch.ElapsedMilliseconds;
                totalms += stopwatch.ElapsedMilliseconds;

                // 3. Evaluate
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                List<Vector3> currentEvaluationResults = EvaluatePoints(finalLightProbesDecimated, evaluationPoints);
                stopwatch.Stop();
                step3 += stopwatch.ElapsedMilliseconds;
                totalms += stopwatch.ElapsedMilliseconds;

                // 4. Compute Cost of current configuration
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                float decimatedCost = ComputeCurrentCost(currentEvaluationResults, evaluationResults);
                step4 += stopwatch.ElapsedMilliseconds;
                totalms += stopwatch.ElapsedMilliseconds;

                // 5. Find light probe with the minimum error
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (decimatedCost < decimatedCostMin) {
                    decimatedIndex           = i;
                    decimatedCostMin         = decimatedCost;
                    evaluationTotalDecimated = RGBToFloat(ComputeCurrentValue(currentEvaluationResults));
                }
                step5 += stopwatch.ElapsedMilliseconds;
                totalms += stopwatch.ElapsedMilliseconds;

                // add back the removed items O(n)
                finalPositionsDecimated.Insert(random_index, last_position_removed);
                finalLightProbesDecimated.Insert(random_index, last_SH_removed);
            }
            Debug.Log("Step 1: " + step1 / 1000.0 + "s");
            Debug.Log("Step 1B: " + step1b / 1000.0 + "s");
            Debug.Log("Step 2: " + step2 / 1000.0 + "s");
            Debug.Log("Step 3: " + step3 / 1000.0 + "s");
            Debug.Log("Step 4: " + step4 / 1000.0 + "s");
            Debug.Log("Step 5: " + step5 / 1000.0 + "s");
            Debug.Log("Total: " + totalms / 1000.0 + "s");

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
    public void EvaluateVisibilityPoints(List<Vector3> posIn, out List<bool> invalidPoints) {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8; // Corresponds to "Environment"

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        //layerMask = ~layerMask;

        invalidPoints = new List<bool>(posIn.Count);
        for (int i = 0; i < posIn.Count; i++) {
            invalidPoints.Add(false);

            // 1. Remove Outside of Tetrahedralize
            if (evaluationTetrahedron[i] == -1) {
                invalidPoints[i] = true;
                continue;
            }

            // 2. Remove Occluded  (if cannot be seen by just one LP)
            Vector3[] tetrahedronPositions;
            GetTetrahedronPositions(evaluationTetrahedron[i], out tetrahedronPositions);

            foreach (Vector3 pos in tetrahedronPositions) {
                Ray visRay = new Ray(posIn[i], pos - posIn[i]);
                RaycastHit hit;
                if (Physics.Raycast(visRay, out hit, float.MaxValue, layerMask)) {
                    // Collision Found
                    // Debug.Log("EP" + i + "-> Collision with " + hit.point);
                    invalidPoints[i] = true;
                    break;
                }
            }
        }
    }

    void GetInterpolatedLightProbe(Vector3 evalPosition, int evalTetrahedron, List<SphericalHarmonicsL2> bakedprobes, ref SphericalHarmonicsL2 sh2) {
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

    public int MapEvaluationPointsToLightProbes(List<Vector3> probePositions, List<Vector3> evalPositions) {
        Lightmapping.Tetrahedralize(probePositions.ToArray(), out tetrahedralizeIndices, out tetrahedralizePositions);

        if (probePositions.Count != tetrahedralizePositions.Length) {
            Debug.LogWarning("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
        }

        int mapped = 0;
        Vector3[] tetrahedronPositions;
        evaluationTetrahedron = new List<int>(evalPositions.Count);
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++) {
            evaluationTetrahedron.Add(-1);

            Vector3 evaluationPosition = evalPositions[evaluationPositionIndex];
            // 1. Relate Evaluation Point with one Tetrahedron
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
    public void EvaluateBakedLightProbes(List<SphericalHarmonicsL2> bakedProbes, out List<bool> invalidPoints) {
        SphericalHarmonicsL2 shZero = new SphericalHarmonicsL2();
        shZero.Clear();

        invalidPoints = new List<bool>(bakedProbes.Count);
        foreach (SphericalHarmonicsL2 sh2 in bakedProbes) {
            invalidPoints.Add(sh2.Equals(shZero));
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
