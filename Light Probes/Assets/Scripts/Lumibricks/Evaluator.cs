#define FAST_IMPL

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

class Evaluator
{
    // tetrahedral
    class TetrahedronGraph
    {
        public List<SphericalHarmonicsL2> LightProbesBakedProbes;
        public int[] tetrahedralizeIndices;
        public Vector3[] tetrahedralizePositions;

        public int[] evaluationTetrahedronIndices;
        public Vector4[] evaluationTetrahedronWeights;
#if FAST_IMPL
        public bool[] evaluationTetrahedronChanged;
#endif

        public void ResetTetrahedronData() {
            tetrahedralizeIndices = null;
            tetrahedralizePositions = null;
        }

        public void ResetEvaluationData() {
            evaluationTetrahedronIndices = null;
            evaluationTetrahedronWeights = null;
            evaluationTetrahedronChanged = null;
        }

        public void Tetrahedralize(List<Vector3> probePositions) {
            Lightmapping.Tetrahedralize(probePositions.ToArray(), out tetrahedralizeIndices, out tetrahedralizePositions);
            if (probePositions.Count != tetrahedralizePositions.Length) {
                LumiLogger.Logger.LogWarning("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
            }
        }
        public Vector4 GetTetrahedronIndices(int index) {
            Vector4 indices = Vector4.zero;
            indices[0] = tetrahedralizeIndices[index * 4 + 0];
            indices[1] = tetrahedralizeIndices[index * 4 + 1];
            indices[2] = tetrahedralizeIndices[index * 4 + 2];
            indices[3] = tetrahedralizeIndices[index * 4 + 3];
            return indices;
        }

        public SphericalHarmonicsL2[] GetTetrahedronSH(Vector4 tetrahedronIndices) {
            SphericalHarmonicsL2[] tetrahedronSH = new SphericalHarmonicsL2[4];
            tetrahedronSH[0] = LightProbesBakedProbes[(int)tetrahedronIndices[0]];
            tetrahedronSH[1] = LightProbesBakedProbes[(int)tetrahedronIndices[1]];
            tetrahedronSH[2] = LightProbesBakedProbes[(int)tetrahedronIndices[2]];
            tetrahedronSH[3] = LightProbesBakedProbes[(int)tetrahedronIndices[3]];
            return tetrahedronSH;
        }

        public SphericalHarmonicsL2[] GetTetrahedronSH(int index) {
            Vector4 tetrahedronIndices = GetTetrahedronIndices(index);
            return GetTetrahedronSH(tetrahedronIndices);
        }

        public Vector3[] GetTetrahedronPositions(int index) {
            Vector4 tetrahedronIndices = GetTetrahedronIndices(index);
            return GetTetrahedronPositions(tetrahedronIndices);
        }
        public Vector3[] GetTetrahedronPositions(Vector4 tetrahedronIndices) {
            Vector3[] tetrahedronPositions = new Vector3[4];
            tetrahedronPositions[0] = tetrahedralizePositions[(int)tetrahedronIndices[0]];
            tetrahedronPositions[1] = tetrahedralizePositions[(int)tetrahedronIndices[1]];
            tetrahedronPositions[2] = tetrahedralizePositions[(int)tetrahedronIndices[2]];
            tetrahedronPositions[3] = tetrahedralizePositions[(int)tetrahedronIndices[3]];
            return tetrahedronPositions;
        }

        public List<Vector3[]> GetPositionList() {
            int tetrahedronCount = tetrahedralizeIndices.Length / 4;
            List<Vector3[]> tetrahedronPositionsList = new List<Vector3[]>(tetrahedronCount);
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
                Vector3[] tetrahedronPositions = GetTetrahedronPositions(tetrahedronIndex);
                tetrahedronPositionsList.Add(tetrahedronPositions);
            }
            return tetrahedronPositionsList;
        }

        public void Init(int count) {
            evaluationTetrahedronIndices = new int[count];
            evaluationTetrahedronWeights = new Vector4[count];
            evaluationTetrahedronChanged = new bool[count];
        }

        public void ResetEvaluationTetrahedron(int index) {
            evaluationTetrahedronIndices[index] = -1;
            evaluationTetrahedronWeights[index] = Vector4.zero;
        }

        public int GetEvaluationTetrahedronIndex(int evaluationPositionIndex) {
            return evaluationTetrahedronIndices[evaluationPositionIndex];
        }
        public Vector4 GetEvaluationTetrahedronWeights(int evaluationPositionIndex) {
            return evaluationTetrahedronWeights[evaluationPositionIndex];
        }

        public void SetEvaluationTetrahedronIndex(int evaluationPositionIndex, int tetrahedronIndex) {
            evaluationTetrahedronIndices[evaluationPositionIndex] = tetrahedronIndex;
        }

        public void SetEvaluationTetrahedronWeights(int evaluationPositionIndex, Vector4 tetrahedronWeights) {
            evaluationTetrahedronWeights[evaluationPositionIndex] = tetrahedronWeights;
        }
        public bool UnMappedEvaluationState(int evaluationPositionIndex) {
            return evaluationTetrahedronIndices[evaluationPositionIndex] == -1;
        }
        public bool ChangedEvaluationState(int evaluationPositionIndex) {
            return evaluationTetrahedronChanged[evaluationPositionIndex] == true;
        }
        public void FlagΑsChanged(int index, bool changed) {
            evaluationTetrahedronChanged[index] = changed;
        }

        public int getNumTetrahedrons() {
            return tetrahedralizeIndices.Length / 4;
        }
    }

    public int getNumTetrahedrons() {
        return tetrahedronGraph.getNumTetrahedrons();
    }

    TetrahedronGraph tetrahedronGraph = null;

    public List<List<int>> mappingLPtoEP;
    public List<List<int>> mappingLPtoEPDecimated;

    // sampling
    bool is_stochastic = false;
    public int num_stochastic_samples;

    // direction generator
    bool averageDirections = false;
    DirectionSamplingGenerator directionSamplingGenerator = null;

    // evaluation
    public List<Color> referenceEvaluationResults;
    public double evaluationError = 0.0f;
    public float terminationEvaluationError = 0.0f;
    public bool isTerminationCurrentLightProbes = true;
    public bool isTerminationEvaluationError = true;
    public int terminationMinLightProbes = 0;
    public int terminationCurrentLightProbes = 0;
    public int terminationMaxLightProbes = 0;
    public int startingLightProbes = 0;
    public int decimatedLightProbes = 0;
    public int finalLightProbes = 0;
    public float totalTime = 0.0f;

    MetricsManager metricsManager = null;
    SolversManager solversManager = null;


    #region Constructor Functions
    public Evaluator() {
        LumiLogger.Logger.Log("Evaluator Constructor");
        metricsManager = new MetricsManager();
        solversManager = new SolversManager();
        tetrahedronGraph = new TetrahedronGraph();
        directionSamplingGenerator = new DirectionSamplingGenerator();
        solversManager.MetricsManager = metricsManager;
        Reset(4);
    }
    #endregion

    public void ResetTime() {
        totalTime = 0.0f;
    }

    public void Reset(int probesCount) {
        directionSamplingGenerator.Reset();
        isTerminationCurrentLightProbes = true;
        isTerminationEvaluationError = false;
        terminationEvaluationError = 0.1f;
        is_stochastic = false;
        num_stochastic_samples = 20;
        averageDirections = true;
        ResetLightProbeData(probesCount);
        solversManager.Reset();
        metricsManager.Reset();
    }

    public void SetProbeData(SphericalHarmonicsL2[] probeData) {
        tetrahedronGraph.LightProbesBakedProbes = new List<SphericalHarmonicsL2>(probeData);
    }

    public void SetLightProbeUserSelection(int userSelectedLightProbes, int userSelectedStochasticSamples) {
        terminationCurrentLightProbes = Mathf.Clamp(userSelectedLightProbes, terminationMinLightProbes, terminationMaxLightProbes);
        num_stochastic_samples = Mathf.Clamp(userSelectedStochasticSamples, 1, terminationMaxLightProbes);
    }

    public void ResetLightProbeData(int maxProbes) {
        terminationMinLightProbes = 4;
        terminationMaxLightProbes = Mathf.Max(4, maxProbes);
        startingLightProbes = terminationMaxLightProbes;
        terminationCurrentLightProbes = Mathf.Clamp((int)(terminationMaxLightProbes / 2), terminationMinLightProbes, terminationMaxLightProbes);
        num_stochastic_samples = Mathf.Clamp(terminationCurrentLightProbes/2, 1, terminationMaxLightProbes);

        tetrahedronGraph.ResetTetrahedronData();
        ResetEvaluationData();
    }

    public void ResetEvaluationData() {
        referenceEvaluationResults = new List<Color>();
        tetrahedronGraph.ResetEvaluationData();

        directionSamplingGenerator.ResetEvaluationData();

        mappingLPtoEP = new List<List<int>>();
        mappingLPtoEPDecimated = new List<List<int>>();

        finalLightProbes = 0;
        evaluationError = 0.0f;
    }
    

    public void populateGUI_DecimateSettings() {
        directionSamplingGenerator.populateGUI_EvaluateDirections();
        averageDirections = EditorGUILayout.Toggle(
            new GUIContent("Average Directions", "Evaluate each EP against the average result of generated directions, instead of each one separately"), averageDirections, CustomStyles.defaultGUILayoutOption);

        GUILayout.BeginHorizontal();
        is_stochastic = EditorGUILayout.Toggle(
               new GUIContent("Stochastic Decimation", "Decimate against a random subset in each iteration instead all LPs"), is_stochastic);
        EditorGUI.BeginDisabledGroup(!is_stochastic);
        num_stochastic_samples = EditorGUILayout.IntSlider(new GUIContent("", ""), num_stochastic_samples, 1, terminationMaxLightProbes, CustomStyles.defaultGUILayoutOption);
        num_stochastic_samples = Mathf.Clamp(num_stochastic_samples, 1, terminationMaxLightProbes);
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
        solversManager.populateGUI();
        metricsManager.populateGUI();

        GUILayout.BeginHorizontal();
        isTerminationCurrentLightProbes = EditorGUILayout.Toggle(
              new GUIContent("Minimum LP set:", "The minimum desired number of light probes"), isTerminationCurrentLightProbes);
        EditorGUI.BeginDisabledGroup(!isTerminationCurrentLightProbes);
        terminationCurrentLightProbes = EditorGUILayout.IntSlider(new GUIContent("", ""), terminationCurrentLightProbes, terminationMinLightProbes, terminationMaxLightProbes, CustomStyles.defaultGUILayoutOption);
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        isTerminationEvaluationError = EditorGUILayout.Toggle(
            new GUIContent("Maximum Error:", "The percentage error after which the decimation stops"), isTerminationEvaluationError);
        EditorGUI.BeginDisabledGroup(!isTerminationEvaluationError);
        terminationEvaluationError = EditorGUILayout.Slider(new GUIContent("",""), terminationEvaluationError, 0.1f, 100.0f, CustomStyles.defaultGUILayoutOption);
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();
        if (!isTerminationCurrentLightProbes && !isTerminationEvaluationError) {
            EditorGUILayout.LabelField(new GUIContent("Warning: You need to use at least one terminating condition!", 
                "At least one of Minimum LP Set and Minimum Error must be selected."), CustomStyles.EditorErrorRed, CustomStyles.defaultGUILayoutOption);
        }
    }

    public bool populateGUI_Decimate(LumibricksScript script, GeneratorInterface currentEvaluationPointsGenerator) {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        bool clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Run Optimizer", "Optimizes light probes"), CustomStyles.defaultGUILayoutOption);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Error: ", "The resulting error compared to the original estimation"), new GUIContent(evaluationError.ToString("0.00") + "%"), CustomStyles.defaultGUILayoutOption);
        EditorGUILayout.LabelField(new GUIContent("LPs (Orig/Final):", "The total number of light probes (Placed/Optimal LPs after decimation)"),
        new GUIContent(
        startingLightProbes.ToString() + "/"
         +finalLightProbes.ToString()), CustomStyles.defaultGUILayoutOption);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Time: ", "The total time taken for Decimation"), new GUIContent(totalTime.ToString("0.00s")), CustomStyles.defaultGUILayoutOption);
        EditorGUILayout.LabelField(new GUIContent("EPs:", "The total number of evaluation points"), new GUIContent(currentEvaluationPointsGenerator.TotalNumProbes.ToString()), CustomStyles.defaultGUILayoutOption);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        return clickedDecimateLightProbes;
    }

    public void GenerateReferenceEvaluationPoints(List<Vector3> evalPositions) {
        directionSamplingGenerator.GenerateDirections();
        referenceEvaluationResults = EvaluatePoints(evalPositions, null);
    }

    void GetInterpolatedLightProbe(int evalPositionIndex, ref SphericalHarmonicsL2 sh2) {
        // Get Tetrahedron Weights
        Vector4 weights = tetrahedronGraph.GetEvaluationTetrahedronWeights(evalPositionIndex);

        // GetTetrahedronSHs
        int tetrahedronIndex = tetrahedronGraph.GetEvaluationTetrahedronIndex(evalPositionIndex);
        Vector4 tetrahedronIndices = tetrahedronGraph.GetTetrahedronIndices(tetrahedronIndex);
        SphericalHarmonicsL2[] tetrahedronSH2 = tetrahedronGraph.GetTetrahedronSH(tetrahedronIndices);

        // Interpolate
        sh2 = weights.x * tetrahedronSH2[0] + weights.y * tetrahedronSH2[1] + weights.z * tetrahedronSH2[2] + weights.w * tetrahedronSH2[3];
    }
    public void evaluateTetrahedron(ref List<Color> currentEvaluationResults, int evalPositionIndex, Vector3[] directions, int resultsPerDirection) {
        SphericalHarmonicsL2 sh2 = new SphericalHarmonicsL2();
        GetInterpolatedLightProbe(evalPositionIndex, ref sh2);
        Color[] evaluationResultsPerDir = new Color[directions.Length];
        sh2.Evaluate(directions, evaluationResultsPerDir);
        if (resultsPerDirection > 1) {
            Color uniformSampledEvaluation = new Color(0, 0, 0);
            for (int i = 0; i < directions.Length; i++) {
                uniformSampledEvaluation.r += evaluationResultsPerDir[i].r;
                uniformSampledEvaluation.g += evaluationResultsPerDir[i].g;
                uniformSampledEvaluation.b += evaluationResultsPerDir[i].b;
            }
            uniformSampledEvaluation /= directions.Length;
            currentEvaluationResults.Add(uniformSampledEvaluation);
        } else {
            currentEvaluationResults.AddRange(evaluationResultsPerDir);
        }
    }

    public List<Color> EvaluatePoints(List<Vector3> evalPositions, List<Color> oldEvaluationResults) {
        Vector3[] directions = directionSamplingGenerator.GetEvaluationDirections();
        int resultsPerDirection = averageDirections ? 1 : directions.Length;

        List<Color> currentEvaluationResults = new List<Color>(evalPositions.Count * resultsPerDirection);

        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; ++evaluationPositionIndex) {
            // invalid tetrahedron
            if (tetrahedronGraph.UnMappedEvaluationState(evaluationPositionIndex)) {
                currentEvaluationResults.AddRange(new Color[resultsPerDirection]);
            }
            // updated tetrahedron
            else if (tetrahedronGraph.ChangedEvaluationState(evaluationPositionIndex)) {
                evaluateTetrahedron(ref currentEvaluationResults, evaluationPositionIndex, directions, resultsPerDirection);
            }
            // set previous results to tetrahedron
            else {
                currentEvaluationResults.AddRange(oldEvaluationResults.GetRange(evaluationPositionIndex * resultsPerDirection, resultsPerDirection));
            }
        }
        return currentEvaluationResults;
    }
    public List<Vector3> DecimateBakedLightProbes(LumibricksScript script, ref bool isCancelled, List<Vector3> evaluationPoints, List<Vector3> lightProbePositions) {
        // TODO: add iterate [DONE]
        // TODO: optimize [NOT], e.g. stochastic [DONE]
        // TODO: modify cost function [DONE]
        // TODO: verify result [DONE]
        // TODO: potentially add multiple cost functions and error metrics [DONE]
        // TODO: finalize plugin/UI software engineering [ALMOST DONE]

        solversManager.SetCurrentSolver();
        metricsManager.SetCurrentMetric();

        // store the final result here
        List<Vector3> finalPositionsDecimated = new List<Vector3>(lightProbePositions);
        List<SphericalHarmonicsL2> finalLightProbesDecimated = new List<SphericalHarmonicsL2>(tetrahedronGraph.LightProbesBakedProbes);
        double currentEvaluationError = 0.0;
        int iteration = 0;
        float termination_error = isTerminationEvaluationError ? terminationEvaluationError : float.MaxValue;
        int termination_probes = isTerminationCurrentLightProbes ? terminationCurrentLightProbes : 0;

        LumiLogger.Logger.Log("Starting Decimation. Settings: " +
            "LPs: " + script.currentLightProbesGenerator.TotalNumProbes +
            ", EPs: " + script.currentEvaluationPointsGenerator.TotalNumProbes +
            ", LP Evaluation method: " + directionSamplingGenerator.EvaluationType.ToString() + "(" + directionSamplingGenerator.GetDirectionCount() + ")" +
            ", Averaging EP directions: " + averageDirections.ToString() +
            ", Stochastic: " + (is_stochastic ? num_stochastic_samples.ToString() : "Disabled") +
            ", Solver: " + solversManager.CurrentSolverType.ToString() +
            ", Metric: " + metricsManager.CurrentMetricType.ToString() +
            ", Minimum Error: " + (isTerminationEvaluationError ? termination_error.ToString() : "Disabled") +
            ", Minimum LP Set: " + (isTerminationCurrentLightProbes ? termination_probes.ToString() : "Disabled"));

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
        List<Color> decimatedEvaluationResults = referenceEvaluationResults.ConvertAll(res => new Color(res.r, res.g, res.b));
#endif
       
        float progress_range = terminationMaxLightProbes - terminationCurrentLightProbes;
        float progress_step = 1 / (progress_range);
        System.DateTime startTime = System.DateTime.Now;

        while (currentEvaluationError < termination_error && termination_probes < finalPositionsDecimated.Count && finalPositionsDecimated.Count > 4 && !isCancelled) {
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

            float progress = 0.0f; 
            if (isTerminationCurrentLightProbes) {
                progress = (lightProbePositions.Count - finalPositionsDecimated.Count) / progress_range;
            }
            for (int i = 0; i < random_samples_each_iteration && !isCancelled; i++) {
                System.TimeSpan interval = (System.DateTime.Now - startTime);
                string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", interval.Hours, interval.Minutes, interval.Seconds);
                //LumiLogger.Logger.Log((progress).ToString("0.0%"));
                if (isTerminationCurrentLightProbes && isTerminationEvaluationError) {
                    float internal_progress = progress + (progress_step * i / (float)(random_samples_each_iteration));
                    isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Progress: " + (internal_progress).ToString("0.0%") + ", Error: " + currentEvaluationError.ToString("0.00") + "%", timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), internal_progress);
                } else if (isTerminationEvaluationError) {
                    isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Error: " + currentEvaluationError.ToString("0.00") + "%", timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), progress);
                } else {
                    float internal_progress = progress + (progress_step * i / (float)(random_samples_each_iteration));
                    isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Progress: " + (internal_progress).ToString("0.0%"), timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), internal_progress);
                  }
                // 1. Remove Light Probe from Set
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int random_index = (is_stochastic) ? Random.Range(0, finalPositionsDecimated.Count) : i;
                Vector3 last_position_removed = finalPositionsDecimated[random_index];
                finalPositionsDecimated.RemoveAt(random_index);
                SphericalHarmonicsL2 last_SH_removed = finalLightProbesDecimated[random_index];
                finalLightProbesDecimated.RemoveAt(random_index);
                // assign the new probe data to the graph
                tetrahedronGraph.LightProbesBakedProbes = finalLightProbesDecimated;
                stopwatch.Stop();
                step1 += stopwatch.ElapsedMilliseconds;

                // 2. Map Evaluation Points to New Light Probe Set 
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                Tetrahedralize(finalPositionsDecimated);
                MapEvaluationPointsToLightProbes(finalPositionsDecimated, evaluationPoints, ref mappingLPtoEPDecimated);
                stopwatch.Stop();
                step2 += stopwatch.ElapsedMilliseconds;

                // 3. Evaluate only the points that have changed
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                // Flag the points that have been changed
                foreach (int j in mappingLPtoEP[random_index]) {
                    tetrahedronGraph.FlagΑsChanged(j, true);
                }
                List<Color> currentEvaluationResults = EvaluatePoints(evaluationPoints, prevEvaluationResults);
                stopwatch.Stop();
                step3 += stopwatch.ElapsedMilliseconds;

                // 4. Compute Cost of current configuration
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                double decimatedCost = solversManager.computeLoss(currentEvaluationResults, referenceEvaluationResults);
                stopwatch.Stop();
                step4 += stopwatch.ElapsedMilliseconds;

                // 5. Find light probe with the minimum error
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (decimatedCost < decimatedCostMin) {
                    decimatedIndex = i;
                    decimatedCostMin = decimatedCost;

#if FAST_IMPL
                    decimatedEvaluationResults = prevEvaluationResults.ConvertAll(res => new Color(res.r, res.g, res.b));
                    foreach (int j in mappingLPtoEP[random_index])
                        decimatedEvaluationResults[j] = new Color(currentEvaluationResults[j].r, currentEvaluationResults[j].g, currentEvaluationResults[j].b);

                    mappingEPtoLPDecimatedMin = mappingLPtoEPDecimated.ConvertAll(res => new List<int>(res.ToArray()));
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

            // if we have terminated on error, skip this iteration
            if (isTerminationEvaluationError && decimatedCostMin > termination_error) {
                LumiLogger.Logger.Log("Iteration: " + iteration.ToString() + ". Error: " + decimatedCostMin.ToString() + ". Stopping using max error criteria");
                break;
            }
            // 6. Remove light probe with the minimum error
            finalPositionsDecimated.RemoveAt(decimatedIndex);
            finalLightProbesDecimated.RemoveAt(decimatedIndex);
            currentEvaluationError = decimatedCostMin;

#if FAST_IMPL
            mappingLPtoEP = mappingEPtoLPDecimatedMin.ConvertAll(res => new List<int>(res.ToArray()));
#endif
            LumiLogger.Logger.Log("Iteration: " + iteration.ToString() + ". Error: " + decimatedCostMin.ToString() + ". Removed probe: " + decimatedIndex.ToString());
            ++iteration;
        }

        {
            float progress = (lightProbePositions.Count - finalPositionsDecimated.Count) / progress_range;
            System.TimeSpan interval = (System.DateTime.Now - startTime);
            string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", interval.Hours, interval.Minutes, interval.Seconds);
            LumiLogger.Logger.Log((progress).ToString("0.0%"));
            if (isTerminationCurrentLightProbes && isTerminationEvaluationError) {
                isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Progress: " + (progress).ToString("0.0%") + ", Error: " + currentEvaluationError.ToString("0.00") + "%", timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), progress);
              } else if (isTerminationEvaluationError) {
                isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Error: " + currentEvaluationError.ToString("0.00") + "%", timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), progress);
            } else {
                isCancelled = EditorUtility.DisplayCancelableProgressBar("Decimation: " + "Progress: " + (progress).ToString("0.0%"), timeText + " Running: Remaining probes: " + finalPositionsDecimated.Count.ToString(), progress);
            }
        }

        if (!isCancelled) {
            evaluationError = currentEvaluationError;
        }

        LumiLogger.Logger.Log("Finished after " + iteration.ToString() + " iterations. Final error: " + evaluationError.ToString("0.00") + "%");
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

    long tetr = 0;
    long mapping = 0;
    public void Tetrahedralize(List<Vector3> probePositions) {
        System.Diagnostics.Stopwatch stopwatch;
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        tetrahedronGraph.Tetrahedralize(probePositions);
        stopwatch.Stop();
        tetr += stopwatch.ElapsedMilliseconds;
    }

    public (int, int) MapEvaluationPointsToLightProbes(List<Vector3> probePositions, List<Vector3> evalPositions) {
        var res = MapEvaluationPointsToLightProbes(probePositions, evalPositions, ref mappingLPtoEP);
        // also flag everything as changed
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++) {
            tetrahedronGraph.FlagΑsChanged(evaluationPositionIndex, true);
        }
        return res;
    }
    private (int, int) MapEvaluationPointsToLightProbes(List<Vector3> probePositions, List<Vector3> evalPositions, ref List<List<int>> mappingList) {
        System.Diagnostics.Stopwatch stopwatch;
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int mapped = 0;
        int mapped_outside = 0;

        List<Vector3[]> tetrahedronPositionsList = tetrahedronGraph.GetPositionList();

        // Compute LP center -> TODO Faster, keep previous center 
        Vector3 lightProbesCentroid = MathUtilities.GetCentroid(probePositions);

        mappingList = new List<List<int>>(probePositions.Count);
        for (int j = 0; j < probePositions.Count; j++) {
            mappingList.Add(new List<int>());
        }

        tetrahedronGraph.Init(evalPositions.Count);
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++) {
            tetrahedronGraph.ResetEvaluationTetrahedron(evaluationPositionIndex);
            tetrahedronGraph.FlagΑsChanged(evaluationPositionIndex, false);

            // 1. Relate Evaluation Point with one Tetrahedron
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronGraph.getNumTetrahedrons(); tetrahedronIndex++) {
                Vector4 tetrahedronWeights = Vector4.zero;
                if (MathUtilities.IsInsideTetrahedronWeights(tetrahedronPositionsList[tetrahedronIndex], evalPositions[evaluationPositionIndex], out tetrahedronWeights)) {
                    tetrahedronGraph.SetEvaluationTetrahedronIndex(evaluationPositionIndex, tetrahedronIndex);
                    tetrahedronGraph.SetEvaluationTetrahedronWeights(evaluationPositionIndex, tetrahedronWeights);

                    Vector4 indices = tetrahedronGraph.GetTetrahedronIndices(tetrahedronIndex);
                    mappingList[(int)indices[0]].Add(evaluationPositionIndex);
                    mappingList[(int)indices[1]].Add(evaluationPositionIndex);
                    mappingList[(int)indices[2]].Add(evaluationPositionIndex);
                    mappingList[(int)indices[3]].Add(evaluationPositionIndex);
                    ++mapped;
                    break;
                }
            }

            // 2. Map invalid tetrahedron to triangles
            if (tetrahedronGraph.UnMappedEvaluationState(evaluationPositionIndex))
            {   
                int   min_index = -1;
                float min_t     = float.MaxValue;

                Vector3 ray_origin    = evalPositions[evaluationPositionIndex];
                Vector3 ray_direction = lightProbesCentroid - ray_origin;
                Vector4 weights       = new Vector4();

                for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronGraph.getNumTetrahedrons(); tetrahedronIndex++) { 
                    for (int j = 0; j < 4; j++) {
                        Vector3 v0 = tetrahedronPositionsList[tetrahedronIndex][(j + 0)%4];
                        Vector3 v1 = tetrahedronPositionsList[tetrahedronIndex][(j + 1)%4];
                        Vector3 v2 = tetrahedronPositionsList[tetrahedronIndex][(j + 2)%4];

                        float t=0, u=0, v=0;
                        if (MathUtilities.IntersectRay_Triangle(ray_origin, ray_direction, v0, v1, v2, ref t, ref u, ref v))
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
                tetrahedronGraph.SetEvaluationTetrahedronIndex(evaluationPositionIndex, min_index);
                tetrahedronGraph.SetEvaluationTetrahedronWeights(evaluationPositionIndex, weights);
                Vector4 indices = tetrahedronGraph.GetTetrahedronIndices(min_index);
                mappingList[(int)indices[0]].Add(evaluationPositionIndex);
                mappingList[(int)indices[1]].Add(evaluationPositionIndex);
                mappingList[(int)indices[2]].Add(evaluationPositionIndex);
                mappingList[(int)indices[3]].Add(evaluationPositionIndex);
                ++mapped_outside;
            }
        }

        stopwatch.Stop();
        mapping += stopwatch.ElapsedMilliseconds;
        return (mapped, mapped_outside);
    }
}
