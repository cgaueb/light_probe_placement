#define FAST_IMPL

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;


class Evaluator
{
    public enum LightProbesEvaluationType
    {
        FixedLow,
        FixedMedium,
        FixedHigh,
        Random
    }

    List<Vector3> evaluationFixedDirections = new List<Vector3> { 
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
    public int evaluationRandomSamplingCount;
    public List<Vector3> evaluationRandomDirections = new List<Vector3>();

    // tetrahedral
    public int[] tetrahedralizeIndices;
    public Vector3[] tetrahedralizePositions;
    public List<int> evaluationTetrahedron;
    public List<Vector4> evaluationTetrahedronWeights;

#if FAST_IMPL
    public List<bool> evaluationTetrahedronChanged;
    public List<List<int>> mappingEPtoLP;
    public List<List<int>> mappingEPtoLPDecimated;
#endif

    // evaluation
    public List<Color> evaluationResults;
    public double evaluationError = 0.0f;
    public float evaluationTotal = 0.0f;
    public float terminationEvaluationError = 0.0f;
    public int terminationMinLightProbes = 0;
    public int terminationCurrentLightProbes = 0;
    public int terminationMaxLightProbes = 0;
    public int startingLightProbes = 0;
    public int decimatedLightProbes = 0;
    public int finalLightProbes = 0;
    public float totalTime = 0.0f;

    SolverManager solvers = null;

    public LightProbesEvaluationType EvaluationType { get; set; }


    #region Constructor Functions
    public Evaluator() {
        LumiLogger.Logger.Log("Evaluator Constructor");
        evaluationRandomSamplingCount = 50;
        EvaluationType = LightProbesEvaluationType.FixedHigh;
        solvers = new SolverManager();
        EvaluationType = LightProbesEvaluationType.FixedHigh;
        for (int i = 0; i < evaluationFixedDirections.Count; ++i) {
            Vector3 v1 = evaluationFixedDirections[i];
            v1.Normalize();
            evaluationFixedDirections[i] = v1;
        }
        Reset(4);
    }
    #endregion

    public void ResetTime() {
        totalTime = 0.0f;
    }

    public void Reset(int probesCount) {
        ResetLightProbeData(probesCount);
        EvaluationType = LightProbesEvaluationType.FixedHigh;
        evaluationRandomSamplingCount = 50;
        solvers.Reset();
    }

    public void ResetLightProbeData(int maxProbes) {
        startingLightProbes = maxProbes;
        finalLightProbes = 0;
        terminationMinLightProbes = 4;
        terminationMaxLightProbes = maxProbes;
        terminationCurrentLightProbes = Mathf.Clamp(terminationMaxLightProbes / 2, terminationMinLightProbes, terminationMaxLightProbes);
        tetrahedralizeIndices = null;
        tetrahedralizePositions = null;
        ResetEvaluationData();
    }

    public void ResetEvaluationData() {
        evaluationResults = null;
        evaluationTetrahedron = null;
        evaluationTetrahedronWeights = null;
#if FAST_IMPL        
        evaluationTetrahedronChanged = null;
        mappingEPtoLP = null;
        mappingEPtoLPDecimated = null;
#endif

        evaluationRandomDirections = new List<Vector3>();
        evaluationTotal = 0.0f;
    }
    private void populateGUI_EvaluateDirections() {
        EvaluationType = (LightProbesEvaluationType)EditorGUILayout.EnumPopup(new GUIContent("Type:", "The probe evaluation method"), EvaluationType, LumibricksScript.defaultOption);
        if (EvaluationType == LightProbesEvaluationType.Random) {
            int prevCount = evaluationRandomSamplingCount;
            evaluationRandomSamplingCount = EditorGUILayout.IntField(new GUIContent("Number of Directions:", "The total number of uniform random sampled directions"), evaluationRandomSamplingCount, LumibricksScript.defaultOption);
            evaluationRandomSamplingCount = Mathf.Clamp(evaluationRandomSamplingCount, 1, 1000000);
            if (prevCount != evaluationRandomSamplingCount) {
                GenerateUniformSphereSampling();
            }
        } else {
            EditorGUILayout.LabelField(new GUIContent("Number of Directions:", "The total number of evaluation directions"), new GUIContent(evaluationFixedCount[(int)EvaluationType].ToString()), LumibricksScript.defaultOption);
        }
    }
    public bool populateGUI_GenerateReferenceEvaluationPoints() {
        populateGUI_EvaluateDirections();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedGenerateReferenceEvaluationPoints = GUILayout.Button(new GUIContent("Generate Reference EP", "Generate Reference Evaluation Points"), LumibricksScript.defaultOption);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        return clickedGenerateReferenceEvaluationPoints;
    }

    public void populateGUI_DecimateSettings() {
        populateGUI_EvaluateDirections();
        solvers.populateGUI();

        terminationCurrentLightProbes = EditorGUILayout.IntSlider(new GUIContent("Minimum LP set:", "The minimum desired number of light probes"), terminationCurrentLightProbes, terminationMinLightProbes, terminationMaxLightProbes, LumibricksScript.defaultOption);
        terminationEvaluationError = EditorGUILayout.Slider(new GUIContent("Minimum error (unused):", "The minimum desired evaluation percentage error"), terminationEvaluationError, 0.0f, 100.0f, LumibricksScript.defaultOption);
    }

    public bool populateGUI_Decimate(LumibricksScript script, GeneratorInterface currentEvaluationPointsGenerator) {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedDecimateLightProbes = false;
            clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Run Optimizer", "Optimizes light probes"), LumibricksScript.defaultOption);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("LPs (Orig/Final):", "The total number of light probes (Placed/Optimal LPs after decimation)"),
            new GUIContent(
                  startingLightProbes.ToString() + "/"
                + finalLightProbes.ToString()), LumibricksScript.defaultOption);
        EditorGUILayout.LabelField(new GUIContent("EPs:", "The total number of evaluation points"), new GUIContent(currentEvaluationPointsGenerator.TotalNumProbes.ToString()), LumibricksScript.defaultOption);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Error: ", "The resulting error compared to the original estimation"), new GUIContent(evaluationError.ToString("0.00") + "%"), LumibricksScript.defaultOption);
        EditorGUILayout.LabelField(new GUIContent("Time: ", "The total time taken for Decimation"), new GUIContent(totalTime.ToString("0.00s")), LumibricksScript.defaultOption);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        return clickedDecimateLightProbes;
    }

    public double ComputeCurrentCost(List<Color> estimates, List<Color> reference) {
        return solvers.computeLoss(estimates, reference);
    }
    public double EvaluateReference(List<Color> reference) {
        return solvers.evaluate(reference);
    }

    public void GenerateReferenceEvaluationPoints(List<SphericalHarmonicsL2> bakedprobes, List<Vector3> evalPositions) {
        evaluationResults = EvaluatePoints(bakedprobes, evalPositions, null);
    }

    public List<Color> EvaluatePoints(List<SphericalHarmonicsL2> bakedprobes, List<Vector3> evalPositions, List<Color> oldEvaluationResults) {
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
        bool is_avg = true;
        List<Color> currentEvaluationResults;
        if (is_avg) {
            currentEvaluationResults = new List<Color>(evalPositions.Count);
        } else {
            currentEvaluationResults = new List<Color>(evalPositions.Count * directionsCount);
        }
        foreach (Vector3 pos in evalPositions) {
            if (evaluationTetrahedron[j] == -1) {
                if (is_avg) {
                    currentEvaluationResults.Add(new Color(0, 0, 0));
                } else {
                    currentEvaluationResults.AddRange(new Color[directionsCount]);
                }
            }
#if FAST_IMPL            
            else if (evaluationTetrahedronChanged[j]) {
                SphericalHarmonicsL2 sh2 = new SphericalHarmonicsL2();
                GetInterpolatedLightProbe(pos, evaluationTetrahedron[j], evaluationTetrahedronWeights[j], bakedprobes, ref sh2);
                sh2.Evaluate(directions, evaluationResultsPerDir);
                if (is_avg) {
                    Color uniformSampledEvaluation = new Color(0, 0, 0);
                    for (int i = 0; i < directionsCount; i++) {
                        uniformSampledEvaluation.r += evaluationResultsPerDir[i].r;
                        uniformSampledEvaluation.g += evaluationResultsPerDir[i].g;
                        uniformSampledEvaluation.b += evaluationResultsPerDir[i].b;
                    }
                    uniformSampledEvaluation /= directionsCount;
                    currentEvaluationResults.Add(uniformSampledEvaluation);
                } else {
                    currentEvaluationResults.AddRange(evaluationResultsPerDir);
                }
            }
#endif            
            else {
#if !FAST_IMPL
                SphericalHarmonicsL2 sh2 = new SphericalHarmonicsL2();
                GetInterpolatedLightProbe(pos, evaluationTetrahedron[j], evaluationTetrahedronWeights[j], bakedprobes, ref sh2);
                sh2.Evaluate(directions, evaluationResultsPerDir);
                if (is_avg) {
                    Color uniformSampledEvaluation = new Color(0, 0, 0);
                    for (int i = 0; i < directionsCount; i++) {
                        uniformSampledEvaluation.r += evaluationResultsPerDir[i].r;
                        uniformSampledEvaluation.g += evaluationResultsPerDir[i].g;
                        uniformSampledEvaluation.b += evaluationResultsPerDir[i].b;
                    }
                    uniformSampledEvaluation /= directionsCount;
                    currentEvaluationResults.Add(uniformSampledEvaluation);
                } else {
                    currentEvaluationResults.AddRange(evaluationResultsPerDir);
                }
#else
                if (is_avg) {
                    currentEvaluationResults.Add(oldEvaluationResults[j]);
                } else {
                   currentEvaluationResults.AddRange(oldEvaluationResults.GetRange(j*directionsCount, directionsCount));
                }
#endif                
            }
            j++;
        }
        return currentEvaluationResults;
    }
    public List<Vector3> DecimateBakedLightProbes(LumibricksScript script, List<Vector3> evaluationPoints, List<Vector3> posIn, List<SphericalHarmonicsL2> bakedProbes) {
        // TODO: add iterate [DONE]
        // TODO: optimize [NOT], e.g. stochastic [DONE]
        // TODO: modify cost function [DONE]
        // TODO: verify result [DONE]
        // TODO: potentially add multiple cost functions and error metrics [DONE]
        // TODO: finalize plugin/UI software engineering [ALMOST DONE]

        solvers.SetCurrentSolver();
        solvers.SetCurrentMetric();

        // store the final result here
        List<Vector3> finalPositionsDecimated = new List<Vector3>(posIn);
        List<SphericalHarmonicsL2> finalLightProbesDecimated = new List<SphericalHarmonicsL2>(bakedProbes);

        double maxError = 0.1;
        double currentEvaluationError = 0.0;
        int iteration = 0;
        int remaining_probes = terminationCurrentLightProbes;
        bool is_stochastic = false;
        int num_stochastic_samples = 20;

        LumiLogger.Logger.Log("Starting Decimation: maxError: " + maxError.ToString() + ", minimum probes: " + remaining_probes + ", stochastic: " + (is_stochastic ? "True" : "False"));
        LumiLogger.Logger.Log("Settings: " +
            "LPs: " + script.currentLightProbesGenerator.TotalNumProbes +
            ", EPs: " + script.currentEvaluationPointsGenerator.TotalNumProbes +
            ", LP Evaluation method: " + EvaluationType.ToString() + "(" + (EvaluationType == LightProbesEvaluationType.Random ? evaluationRandomSamplingCount : evaluationFixedCount[(int)EvaluationType]) + ")" +
            ", Solver: " + solvers.CurrentSolverType.ToString() +
            ", Metric: " + solvers.CurrentMetricType.ToString());


        long step1 = 0;
        long step2 = 0;
        long step3 = 0;
        long step4 = 0;
        long step5 = 0;
        long step6 = 0;
        tetr = 0;
        System.Diagnostics.Stopwatch stopwatch;
        mapping = 0;

#if FAST_IMPL
        List<Color> decimatedEvaluationResults = evaluationResults.ConvertAll(res => new Color(res.r, res.g, res.b));
#endif

        while (/*currentEvaluationError < maxError && */remaining_probes < finalPositionsDecimated.Count) {
            // remove the Probe which contributes "the least" to the reference
            // Optimize: don't iterate against all every time
            // Step 1: Ideally use a stochastic approach, i.e. remove random N at each iteration. [Done]
            // Step 2: Only perform mapping in the vicinity of the removed light probe. [NOT]
            // Step 3: Only evaluate points in the vicinity of the probe. [DONE]
            int decimatedIndex = -1;
            double decimatedCostMin = double.MaxValue;

            int random_samples_each_iteration = (is_stochastic) ? Mathf.Min(num_stochastic_samples, finalPositionsDecimated.Count) : finalPositionsDecimated.Count;

#if FAST_IMPL
            List<Color> prevEvaluationResults = decimatedEvaluationResults.ConvertAll(res => new Color(res.r, res.g, res.b));
            List<List<int>> mappingEPtoLPDecimatedMin = new List<List<int>>(finalLightProbesDecimated.Count - 1);
#endif
            for (int i = 0; i < random_samples_each_iteration; i++) {
                // 1. Remove Light Probe from Set
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int random_index = (is_stochastic) ? Random.Range(0, random_samples_each_iteration) : i;
                Vector3 last_position_removed = finalPositionsDecimated[random_index];
                finalPositionsDecimated.RemoveAt(random_index);
                SphericalHarmonicsL2 last_SH_removed = finalLightProbesDecimated[random_index];
                finalLightProbesDecimated.RemoveAt(random_index);
                stopwatch.Stop();
                step1 += stopwatch.ElapsedMilliseconds;

                // 2. Map Evaluation Points to New Light Probe Set 
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                Tetrahedralize(finalPositionsDecimated);
                MapEvaluationPointsToLightProbesLocal(finalPositionsDecimated, evaluationPoints);
                stopwatch.Stop();
                step2 += stopwatch.ElapsedMilliseconds;

                // 3. Evaluate 
                stopwatch = System.Diagnostics.Stopwatch.StartNew();

#if FAST_IMPL
                // Evaluate only the points that have been changed
                evaluationTetrahedronChanged = new List<bool>(new bool[evaluationPoints.Count]);
                foreach (int j in mappingEPtoLP[random_index])
                    evaluationTetrahedronChanged[j] = true;
                List<Color> currentEvaluationResults = EvaluatePoints(finalLightProbesDecimated, evaluationPoints, prevEvaluationResults);
#else
                List<Color> currentEvaluationResults = EvaluatePoints(finalLightProbesDecimated, evaluationPoints, null);
#endif
                stopwatch.Stop();
                step3 += stopwatch.ElapsedMilliseconds;

                // 4. Compute Cost of current configuration
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                double decimatedCost = ComputeCurrentCost(currentEvaluationResults, evaluationResults);
                stopwatch.Stop();
                step4 += stopwatch.ElapsedMilliseconds;

                // 5. Find light probe with the minimum error
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (decimatedCost < decimatedCostMin) {
                    decimatedIndex = i;
                    decimatedCostMin = decimatedCost;

#if FAST_IMPL
                    decimatedEvaluationResults = prevEvaluationResults.ConvertAll(res => new Color(res.r, res.g, res.b));
                    foreach (int j in mappingEPtoLP[random_index])
                        decimatedEvaluationResults[j] = new Color(currentEvaluationResults[j].r, currentEvaluationResults[j].g, currentEvaluationResults[j].b);

                    mappingEPtoLPDecimatedMin = mappingEPtoLPDecimated.ConvertAll(res => new List<int>(res.ToArray()));
#endif                        
                }
                stopwatch.Stop();
                step5 += stopwatch.ElapsedMilliseconds;

                // add back the removed items O(n)
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                finalPositionsDecimated.Insert(random_index, last_position_removed);
                finalLightProbesDecimated.Insert(random_index, last_SH_removed);
                stopwatch.Stop();
                step6 += stopwatch.ElapsedMilliseconds;
            }

            if (decimatedIndex == -1) {
                LumiLogger.Logger.LogError("No probe found during the iteration");
            }

            // 6. Remove light probe with the minimum error
            finalPositionsDecimated.RemoveAt(decimatedIndex);
            finalLightProbesDecimated.RemoveAt(decimatedIndex);
            currentEvaluationError = decimatedCostMin;

#if FAST_IMPL
            mappingEPtoLP = mappingEPtoLPDecimatedMin.ConvertAll(res => new List<int>(res.ToArray()));
#endif
            LumiLogger.Logger.Log("Iteration: " + iteration.ToString() + ". Cost: " + decimatedCostMin.ToString() + ". Removed probe: " + decimatedIndex.ToString());
            ++iteration;
        }
        evaluationError = currentEvaluationError;

        LumiLogger.Logger.Log("Finished after " + iteration.ToString() + " iterations. Final error: " + evaluationError.ToString("0.00"));
        LumiLogger.Logger.Log("1. Remove LP: " + step1 / 1000.0 + "s");
        LumiLogger.Logger.Log("2. Remap EPs: " + step2 / 1000.0 + "s");
        LumiLogger.Logger.Log("2.1 Tetrahed: " + tetr / 1000.0 + "s");
        LumiLogger.Logger.Log("2.2 Mappings: " + mapping / 1000.0 + "s");
        LumiLogger.Logger.Log("3. Eval  EPs: " + step3 / 1000.0 + "s");
        LumiLogger.Logger.Log("4. Calc Cost: " + step4 / 1000.0 + "s");
        LumiLogger.Logger.Log("5. Find Min : " + step5 / 1000.0 + "s");
        LumiLogger.Logger.Log("6. Insert LP: " + step6 / 1000.0 + "s");
        LumiLogger.Logger.Log("Total: " + (step1 + step2 + step3 + step4 + step5 + step6) / 1000.0 + "s");

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
                    // LumiLogger.Logger.Log("EP" + i + "-> Collision with " + hit.point);
                    invalidPoints[i] = true;
                    break;
                }
            }
        }
    }

    void GetInterpolatedLightProbe(Vector3 evalPosition, int evalTetrahedron, Vector4 evalWeights, List<SphericalHarmonicsL2> bakedprobes, ref SphericalHarmonicsL2 sh2) {
        // GetTetrahedronSHs
        SphericalHarmonicsL2[] tetrahedronSH2 = new SphericalHarmonicsL2[4];
        tetrahedronSH2[0] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 0]];
        tetrahedronSH2[1] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 1]];
        tetrahedronSH2[2] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 2]];
        tetrahedronSH2[3] = bakedprobes[tetrahedralizeIndices[evalTetrahedron * 4 + 3]];

        Vector4 weights = evalWeights;
        if(weights.Equals(Vector4.zero)) // If is inside
        {
            // Get Barycentric Weights
            Vector3[] tetrahedronPositions;
            GetTetrahedronPositions(evalTetrahedron, out tetrahedronPositions);
        
            weights = GetTetrahedronWeights(tetrahedronPositions, evalPosition);
        }

        // Interpolate
        sh2 = weights.x * tetrahedronSH2[0] + weights.y * tetrahedronSH2[1] + weights.z * tetrahedronSH2[2] + weights.w * tetrahedronSH2[3];
    }

    long tetr = 0;
    long mapping = 0;
    public void Tetrahedralize(List<Vector3> probePositions) {
        System.Diagnostics.Stopwatch stopwatch;
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Lightmapping.Tetrahedralize(probePositions.ToArray(), out tetrahedralizeIndices, out tetrahedralizePositions);
        stopwatch.Stop();
        tetr += stopwatch.ElapsedMilliseconds;

        if (probePositions.Count != tetrahedralizePositions.Length) {
            LumiLogger.Logger.LogWarning("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
        }
    }

    public void MapEvaluationPointsToLightProbesLocal(List<Vector3> probePositions, List<Vector3> evalPositions) {
        System.Diagnostics.Stopwatch stopwatch;
        stopwatch = System.Diagnostics.Stopwatch.StartNew();

        int tetrahedronCount = tetrahedralizeIndices.Length / 4;
        Vector3[] tetrahedronPositions;
        List<Vector3[]> tetrahedronPositionsList = new List<Vector3[]>(tetrahedronCount);
        for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
            GetTetrahedronPositions(tetrahedronIndex, out tetrahedronPositions);
            tetrahedronPositionsList.Add(tetrahedronPositions);
        }

        // Compute LP center -> TODO Faster, keep previous center 
        Vector3 lightProbesCentroid = new Vector3();
        for (int lpIndex = 0; lpIndex < probePositions.Count; lpIndex++)
            lightProbesCentroid += probePositions[lpIndex];
        lightProbesCentroid /= probePositions.Count;

#if FAST_IMPL
        mappingEPtoLPDecimated = new List<List<int>>(probePositions.Count);
        for (int j = 0; j < probePositions.Count; j++) {
            mappingEPtoLPDecimated.Add(new List<int>());
        }
#endif

        evaluationTetrahedron = new List<int>(evalPositions.Count);
        evaluationTetrahedronWeights = new List<Vector4>(evalPositions.Count);
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++) {
            evaluationTetrahedron.Add(-1);
            evaluationTetrahedronWeights.Add(Vector4.zero);

            // 1. Relate Evaluation Point with one Tetrahedron
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
                if (IsInsideTetrahedronWeights(tetrahedronPositionsList[tetrahedronIndex], evalPositions[evaluationPositionIndex])) {
                    evaluationTetrahedron[evaluationPositionIndex] = tetrahedronIndex;
#if FAST_IMPL
                    mappingEPtoLPDecimated[tetrahedralizeIndices[tetrahedronIndex * 4 + 0]].Add(evaluationPositionIndex);
                    mappingEPtoLPDecimated[tetrahedralizeIndices[tetrahedronIndex * 4 + 1]].Add(evaluationPositionIndex);
                    mappingEPtoLPDecimated[tetrahedralizeIndices[tetrahedronIndex * 4 + 2]].Add(evaluationPositionIndex);
                    mappingEPtoLPDecimated[tetrahedralizeIndices[tetrahedronIndex * 4 + 3]].Add(evaluationPositionIndex);
#endif                    
                    break;
                }
            }

            if(evaluationTetrahedron[evaluationPositionIndex] == -1)
            {   
                int   min_index = -1;
                float min_t     = float.MaxValue;

                Vector3 ray_origin    = evalPositions[evaluationPositionIndex];
                Vector3 ray_direction = lightProbesCentroid - ray_origin;
                Vector4 weights       = new Vector4();

                for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) { 
                    for (int j = 0; j < 4; j++) {
                        Vector3 v0 = tetrahedronPositionsList[tetrahedronIndex][(j + 0)%4];
                        Vector3 v1 = tetrahedronPositionsList[tetrahedronIndex][(j + 1)%4];
                        Vector3 v2 = tetrahedronPositionsList[tetrahedronIndex][(j + 2)%4];

                        float t=0, u=0, v=0;
                        if (IntersectRay_Triangle(ray_origin, ray_direction, v0, v1, v2, ref t, ref u, ref v))
                        {
                            if (t > 0 && t < min_t){
                                min_t     = t;
                                min_index = tetrahedronIndex;
                                // Set Weights
                                weights[(j + 0)%4] = u;
                                weights[(j + 1)%4] = v;
                                weights[(j + 2)%4] = 1-u-v;
                                weights[(j + 3)%4] = 0;
                            }
                        }
                    }
                }
                evaluationTetrahedron[evaluationPositionIndex]        = min_index;
                evaluationTetrahedronWeights[evaluationPositionIndex] = new Vector4(weights.x, weights.y, weights.z, weights.w);
#if FAST_IMPL
                mappingEPtoLPDecimated[tetrahedralizeIndices[min_index * 4 + 0]].Add(evaluationPositionIndex);
                mappingEPtoLPDecimated[tetrahedralizeIndices[min_index * 4 + 1]].Add(evaluationPositionIndex);
                mappingEPtoLPDecimated[tetrahedralizeIndices[min_index * 4 + 2]].Add(evaluationPositionIndex);
                mappingEPtoLPDecimated[tetrahedralizeIndices[min_index * 4 + 3]].Add(evaluationPositionIndex);
#endif                
            }
        }
        stopwatch.Stop();
        mapping += stopwatch.ElapsedMilliseconds;
    }

    public int MapEvaluationPointsToLightProbes(List<Vector3> probePositions, List<Vector3> evalPositions) {
        System.Diagnostics.Stopwatch stopwatch;
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int mapped = 0;
#if FAST_IMPL
        evaluationTetrahedronChanged = new List<bool>(evalPositions.Count);
        mappingEPtoLP = new List<List<int>>(probePositions.Count);
        for (int j = 0; j < probePositions.Count; j++) {
            mappingEPtoLP.Add(new List<int>());
        }
#endif

        int tetrahedronCount = tetrahedralizeIndices.Length / 4;
        Vector3[] tetrahedronPositions;
        List<Vector3[]> tetrahedronPositionsList = new List<Vector3[]>(tetrahedronCount);
        for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
            GetTetrahedronPositions(tetrahedronIndex, out tetrahedronPositions);
            tetrahedronPositionsList.Add(tetrahedronPositions);
        }

        // Compute LP center
        Vector3 lightProbesCentroid = new Vector3();
        for (int lpIndex = 0; lpIndex < probePositions.Count; lpIndex++)
            lightProbesCentroid += probePositions[lpIndex];
        lightProbesCentroid /= probePositions.Count;

        evaluationTetrahedron = new List<int>(evalPositions.Count);
        evaluationTetrahedronWeights = new List<Vector4>(evalPositions.Count);
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++) {
            evaluationTetrahedron.Add(-1);
            evaluationTetrahedronWeights.Add(Vector4.zero);
#if FAST_IMPL
            evaluationTetrahedronChanged.Add(true);
#endif
            // 1. Relate Evaluation Point with one Tetrahedron
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
                if (IsInsideTetrahedronWeights(tetrahedronPositionsList[tetrahedronIndex], evalPositions[evaluationPositionIndex])) {
#if FAST_IMPL
                    mappingEPtoLP[tetrahedralizeIndices[tetrahedronIndex * 4 + 0]].Add(evaluationPositionIndex);
                    mappingEPtoLP[tetrahedralizeIndices[tetrahedronIndex * 4 + 1]].Add(evaluationPositionIndex);
                    mappingEPtoLP[tetrahedralizeIndices[tetrahedronIndex * 4 + 2]].Add(evaluationPositionIndex);
                    mappingEPtoLP[tetrahedralizeIndices[tetrahedronIndex * 4 + 3]].Add(evaluationPositionIndex);
#endif
                    evaluationTetrahedron[evaluationPositionIndex] = tetrahedronIndex;
                    mapped++;
                    break;
                }
            }

            if(evaluationTetrahedron[evaluationPositionIndex] == -1)
            {   
                int   min_index = -1;
                float min_t     = float.MaxValue;

                Vector3 ray_origin    = evalPositions[evaluationPositionIndex];
                Vector3 ray_direction = lightProbesCentroid - ray_origin;
                Vector4 weights       = new Vector4();

                for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) { 
                    
                    for (int j = 0; j < 4; j++) {
                        Vector3 v0 = tetrahedronPositionsList[tetrahedronIndex][(j + 0)%4];
                        Vector3 v1 = tetrahedronPositionsList[tetrahedronIndex][(j + 1)%4];
                        Vector3 v2 = tetrahedronPositionsList[tetrahedronIndex][(j + 2)%4];

                        float t=0, u=0, v=0;
                        if (IntersectRay_Triangle(ray_origin, ray_direction, v0, v1, v2, ref t, ref u, ref v))
                        {
                            if (t > 0 && t < min_t){
                                min_t     = t;
                                min_index = tetrahedronIndex;
                                // Set Weights
                                weights[(j + 0)%4] = u;
                                weights[(j + 1)%4] = v;
                                weights[(j + 2)%4] = 1-u-v;
                                weights[(j + 3)%4] = 0;
                            }
                        }
                    }
                }
                evaluationTetrahedron[evaluationPositionIndex]        = min_index;
                evaluationTetrahedronWeights[evaluationPositionIndex] = new Vector4(weights.x, weights.y, weights.z, weights.w);
#if FAST_IMPL
                mappingEPtoLP[tetrahedralizeIndices[min_index * 4 + 0]].Add(evaluationPositionIndex);
                mappingEPtoLP[tetrahedralizeIndices[min_index * 4 + 1]].Add(evaluationPositionIndex);
                mappingEPtoLP[tetrahedralizeIndices[min_index * 4 + 2]].Add(evaluationPositionIndex);
                mappingEPtoLP[tetrahedralizeIndices[min_index * 4 + 3]].Add(evaluationPositionIndex);
#endif
            }

        }
        stopwatch.Stop();
        mapping += stopwatch.ElapsedMilliseconds;
        return mapped;
    }


    bool IntersectRay_Triangle( Vector3 ray_origin, Vector3 ray_direction, 
                                        Vector3 v0, Vector3 v1, Vector3 v2,
                                        ref float t, ref float u, ref float v)
    {
        // find vectors for two edges sharing v0
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        // begin calculating determinant - also used to calculate U parameter
        Vector3 pvec  = Vector3.Cross(ray_direction, edge2);
        
        // if determinant is near zero, ray lies in plane of triangle
        float  det   = Vector3.Dot(edge1, pvec);

        // use backface culling
        // if (det < RT_EPSILON)
        //   return false;

        float  inv_det = 1.0f / det;
        // calculate distance from v0 to ray origin
        Vector3 tvec    = ray_origin - v0;

        // calculate U parameter and test bounds
        u = Vector3.Dot(tvec, pvec) * inv_det;
        if (u < 0.0 || u > 1.0f)
            return false;

        // prepare to test V parameter
        Vector3 qvec    = Vector3.Cross(tvec, edge1);

        // calculate V parameter and test bounds
        v = Vector3.Dot(ray_direction, qvec) * inv_det;
        if (v < 0.0 || u + v > 1.0f)
            return false;

        // calculate t, ray intersects triangle
        t = Vector3.Dot(edge2, qvec) * inv_det;

        return true;
    }
    
    private void GenerateUniformSphereSampling() {
        evaluationRandomDirections.Clear();
        for (int i = 0; i < evaluationRandomSamplingCount; i++) {
            Vector2 r = new Vector2(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
            float phi = r.x * 2.0f * Mathf.PI;
            float cosTheta = 1.0f - 2.0f * r.y;
            float sinTheta = Mathf.Sqrt(1.0f - cosTheta * cosTheta);
            Vector3 vec = new Vector3(Mathf.Cos(phi) * sinTheta, Mathf.Sin(phi) * sinTheta, cosTheta);
            vec.Normalize();
            evaluationRandomDirections.Add(vec);
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

    Vector4 GetTetrahedronWeights_SLOW(Vector3[] v, Vector3 p) {
        Matrix4x4 mat = Matrix4x4.identity;
        mat.SetColumn(0, v[0] - v[3]);
        mat.SetColumn(1, v[1] - v[3]);
        mat.SetColumn(2, v[2] - v[3]);
        Vector4 v_new = p - v[3];
        Vector4 weights = mat.inverse * v_new;
        weights.w = 1 - weights.x - weights.y - weights.z;
        return weights;
    }

    float ScTP(Vector3 a, Vector3 b, Vector3 c) {
        return Vector3.Dot(a, Vector3.Cross(b, c));
    }

    Vector4 GetTetrahedronWeights(Vector3[] v, Vector3 p) {
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

    bool IsInsideTetrahedronWeights(Vector3[] v, Vector3 p) {
        Vector4 weights = GetTetrahedronWeights(v, p);
        return weights.x >= 0 && weights.y >= 0 && weights.z >= 0 && weights.w >= 0
            && (weights.x + weights.y + weights.z + weights.w <= 1.0);
    }
}
