using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(LumibricksScript))]
public class LightProbesEditor : Editor
{
    #region Private Variables
    private AnimBool m_ShowPlacement;
    private AnimBool m_ShowOptimization;
    Stopwatch stopwatch = null;
    GUIStyle EditorStylesHeader;
    GUIStyle EditorStylesMainAction;
    GUIStyle EditorStylesSubAction;
    bool simpleGUI = true;
    #endregion

    #region Public Override Functions
    public override void OnInspectorGUI() {
        LumibricksScript script = (LumibricksScript)target;

        float scale = 0.9f;
        Color colorHeader = new Color(1.0f, 0.65f, 0.0f);
        Color colorMainAction = new Color(1.0f, 0.65f, 0.0f);
        Color colorSubAction = new Color(0.95f, 0.95f, 0.0f);

        EditorStylesHeader = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesHeader.normal.textColor = new Color(colorHeader.r * scale, colorHeader.g * scale, colorHeader.b * scale);
        EditorStylesMainAction = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesMainAction.normal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesSubAction = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesSubAction.normal.textColor = new Color(colorSubAction.r * scale, colorSubAction.g * scale, colorSubAction.b * scale);
        
        // Set LightProbeGroup
        var component = script.GetComponent<LightProbeGroup>();
        if (component == null) {
            LumiLogger.Logger.LogWarning("Not a LightProbeGroup component");
            return;
        }
        script.LightProbeGroup = component;

        // generate GUI Elements
        var clickedElements = populateInspectorGUI();
        bool clickedSuccess = clickedElements.Item1;
        bool clickedResetLightProbes = clickedElements.Item2;
        bool clickedPlaceLightProbes = clickedElements.Item3;
        bool clickedResetEvaluationPoints = clickedElements.Item4;
        bool clickedPlaceEvaluationPoints = clickedElements.Item5;
        bool clickedMapEvaluationPointsToLightProbes = clickedElements.Item6;
        bool clickedBakeLightProbes = clickedElements.Item7;
        bool clickedSimplifyLightProbes = clickedElements.Item8;
        bool clickedSimplifyEvaluationPoints = clickedElements.Item9;
        bool clickedEvaluateEvaluationPoints = clickedElements.Item10;
        bool clickedDecimateLightProbes = clickedElements.Item11;

        if (!clickedSuccess &&
            !clickedResetLightProbes && !clickedPlaceLightProbes &&
            !clickedResetEvaluationPoints && !clickedPlaceEvaluationPoints && !clickedMapEvaluationPointsToLightProbes &&
            !clickedBakeLightProbes && !clickedSimplifyLightProbes &&
            !clickedSimplifyEvaluationPoints && !clickedEvaluateEvaluationPoints && !clickedDecimateLightProbes) {
            return;
        }

        // Start Process - Clear Light Probes
        if (clickedResetLightProbes) {
            float placems = ResetLightProbes();
            LumiLogger.Logger.Log("Done (ResetLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Light Probes
        if (clickedPlaceLightProbes) {
            float placems = PlaceLightProbes();
            LumiLogger.Logger.Log("Done (PlaceLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Clear Evaluation Points
        if (clickedResetEvaluationPoints) {
            float placems = ResetEvaluationPoints();
            LumiLogger.Logger.Log("Done (ResetEvaluationsPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Evaluation Points
        if (clickedPlaceEvaluationPoints) {
            float placems = PlaceEvaluationPoints();
            LumiLogger.Logger.Log("Done (PlaceEvaluationPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        if (clickedMapEvaluationPointsToLightProbes) {
            float mappings = MapEvaluationPointsToLightProbes();
            LumiLogger.Logger.Log("Done (MapEvaluationPointsToLightProbes: " + mappings / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Bake 
        if (clickedBakeLightProbes) {
            if (!Lightmapping.bakedGI) {
                LumiLogger.Logger.LogWarning("Baked GI option must be enabled to perform optimization");
                FinishProcess();
                return;
            }

            float bakeLPms = BakeLightProbes();
            LumiLogger.Logger.Log("Done  (Bake: " + bakeLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Light Probes
        if (clickedSimplifyLightProbes) {
            float removeInvalidLPms = RemoveInvalidLightProbes();
            LumiLogger.Logger.Log("Done  (Remove Invalid LP: " + removeInvalidLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Evaluation Points
        if (clickedSimplifyEvaluationPoints) {
            float removeInvalidEPms = RemoveInvalidEvaluationPoints();
            LumiLogger.Logger.Log("Done  (Remove Invalid EP: " + removeInvalidEPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Evaluate
        if (clickedEvaluateEvaluationPoints) {
            float evaluatems = EvaluateEvaluationPoints();
            LumiLogger.Logger.Log("Done  (Evaluate EP: " + evaluatems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Decimate Light Probes
        if (clickedDecimateLightProbes) {
            float decimatems = 0.0f;
            if (simpleGUI) {
                decimatems = DecimateLightProbes(true);
            } else {
                decimatems = DecimateLightProbes(false);
            }
            LumiLogger.Logger.Log("Done  (Decimate LP: " + decimatems / 1000.0 + "s)");
            FinishProcess();
        }
    }
    #endregion

    #region Private Functions
    void OnInspectorUpdate() {
        // Call Repaint on OnInspectorUpdate as it repaints the windows less times as if it was OnGUI/Update
        Repaint();
    }
    public void Awake() {
        LumiLogger.Logger.Log("Awake");
    }

    public void OnEnable() {
        LumiLogger.Logger.Log("OnEnable");
        m_ShowPlacement = new AnimBool(true);
        m_ShowPlacement.valueChanged.AddListener(Repaint);
        m_ShowOptimization = new AnimBool(true);
        m_ShowOptimization.valueChanged.AddListener(Repaint);
    }

    public void OnDisable() {
        LumiLogger.Logger.Log("OnDisable");
    }

    void OnDestroy() // [TODO] not activated - we have to destroy EP game objs !
    {
        LumiLogger.Logger.Log("OnDestroy entered");
        LumibricksScript script = (LumibricksScript)target;
        //script.Destroy();
    }

    void FinishProcess() {
        EditorUtility.ClearProgressBar();
        GUIUtility.ExitGUI();
    }

    float PlaceLightProbes() {
        LumibricksScript script = (LumibricksScript)target;

        // Start Process - Generate
        stopwatch = Stopwatch.StartNew();
        {
            // Find new positions
            EditorUtility.DisplayProgressBar("Generating light probes", "Generate", 0f);
            script.PlaceLightProbes();
            EditorUtility.DisplayProgressBar("Generating light probes", "Generate", 1f);
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    float ResetLightProbes() {
        LumibricksScript script = (LumibricksScript)target;

        // Start Process - Generate
        stopwatch = Stopwatch.StartNew();
        {
            // Find new positions
            EditorUtility.DisplayProgressBar("Reset light probes", "Reset", 0f);
            script.ResetLightProbes();
            EditorUtility.DisplayProgressBar("Reset light probes", "Reset", 1f);
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    float PlaceEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        // Start Process - Generate
        stopwatch = Stopwatch.StartNew();
        {
            // Find new positions
            EditorUtility.DisplayProgressBar("Generating Evaluation Points", "Generate", 0f);
            script.PlaceEvaluationPoints();
            EditorUtility.DisplayProgressBar("Generating Evaluation Points", "Generate", 1f);
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    float ResetEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        // Start Process - Generate
        stopwatch = Stopwatch.StartNew();
        {
            // Find new positions
            EditorUtility.DisplayProgressBar("Reset evaluation points", "Reset", 0f);
            script.ResetEvaluationPoints();
            EditorUtility.DisplayProgressBar("Reset evaluation points", "Reset", 1f);
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    float MapEvaluationPointsToLightProbes() {
        LumibricksScript script = (LumibricksScript)target;

        // Start Process - Generate
        stopwatch = Stopwatch.StartNew();
        {
            // Find new positions
            EditorUtility.DisplayProgressBar("Map Evaluation Points to Light Probes", "Generate", 0f);
            script.MapEvaluationPointsToLightProbes();
            EditorUtility.DisplayProgressBar("Map Evaluation Points to Light Probes", "Generate", 1f);
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    float BakeLightProbes() {

        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            //EditorUtility.DisplayProgressBar("Clear Caches", "Clean", 0f);
            //Lightmapping.ClearDiskCache();
            //Lightmapping.ClearLightingDataAsset();
            //EditorUtility.DisplayProgressBar("Clear Caches", "Clean", 1f);

            EditorUtility.DisplayProgressBar("Bake", "Bake", 0f);
            script.Bake();
            EditorUtility.DisplayProgressBar("Bake", "Bake", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float RemoveInvalidLightProbes() {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Remove invalid Light Probes", "Remove", 0f);
            script.RemoveInvalidLightProbes();
            EditorUtility.DisplayProgressBar("Remove invalid Light Probes", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float RemoveInvalidEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Remove invalid Evaluation Points", "Remove", 0f);
            script.RemoveInvalidEvaluationPoints();
            EditorUtility.DisplayProgressBar("Remove invalid Evaluation Points", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float EvaluateEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Evaluate Evaluation Points", "Evaluate", 0f);
            script.EvaluateEvaluationPoints();
            EditorUtility.DisplayProgressBar("Evaluate Evaluation Points", "Evaluate", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }
    float DecimateLightProbes(bool executeAll) {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Decimate light probes", "Decimate", 0f);
            script.DecimateLightProbes(executeAll);
            EditorUtility.DisplayProgressBar("Decimate light probes", "Decimate", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    (bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool) populateInspectorGUI() {
        bool clickedSuccess = false;
        bool clickedResetLightProbes = false;
        bool clickedPlaceLightProbes = false;
        bool clickedResetEvaluationPoints = false;
        bool clickedPlaceEvaluationPoints = false;
        bool clickedMapEvaluationPointsToProbes = false;
        bool clickedBakeLightProbes = false;
        bool clickedRemoveInvalidLightProbes = false;
        bool clickedRemoveInvalidEvaluationPoints = false;
        bool clickedEvaluateEvaluationPoints = false;
        bool clickedDecimateLightProbes = false;

        LumibricksScript script = (LumibricksScript)target;
        if (!script.Init()) {
            return (clickedSuccess, clickedResetLightProbes, clickedPlaceLightProbes,
                clickedResetEvaluationPoints, clickedPlaceEvaluationPoints, clickedMapEvaluationPointsToProbes,
                clickedBakeLightProbes,
                clickedRemoveInvalidLightProbes, clickedRemoveInvalidEvaluationPoints,
                clickedEvaluateEvaluationPoints, clickedDecimateLightProbes);
        }
        GUILayoutOption[] defaultOption = new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MinWidth(50), GUILayout.MaxWidth(1500) };

        EditorGUILayout.LabelField("Light Probes Cut Algorithm", EditorStylesHeader);
        EditorGUILayout.Space();

        simpleGUI = EditorGUILayout.ToggleLeft(new GUIContent("Simplified GUI", "Minimized GUI version"), simpleGUI, EditorStylesSubAction);
        EditorGUILayout.Space();

        bool clicked = GUILayout.Button(new GUIContent("Auto set LP/EP Volumes (Debug)", ""), defaultOption);
        if (clicked) {
            script.sceneVolumeLP = GameObject.Find("ATestVolumeLP");
            script.sceneVolumeEP = GameObject.Find("BTestVolumeEP");
        }

        m_ShowPlacement.target = EditorGUILayout.ToggleLeft(new GUIContent("1. Placement", "Placement Fields"), m_ShowPlacement.target, EditorStylesMainAction);
        if (EditorGUILayout.BeginFadeGroup(m_ShowPlacement.faded)) {
            EditorGUILayout.LabelField("1.1. Light Probes (LP)", EditorStylesSubAction);
            script.populateGUI_LightProbes();

            GUILayout.BeginHorizontal();
            clickedResetLightProbes = GUILayout.Button(new GUIContent("Reset", "Reset the Light Probes for this configuration"), defaultOption);
            clickedPlaceLightProbes = GUILayout.Button(new GUIContent("Place", "Place the Light Probes for this configuration"), defaultOption);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("1.2. Evaluation Points (EP)", EditorStylesSubAction);
            script.populateGUI_EvaluationPoints();

            // Created internally
            // evaluationPointGeometry = EditorGUILayout.ObjectField("Geometry Representation", evaluationPointGeometry, typeof(GameObject), true) as GameObject;

            GUILayout.BeginHorizontal();
            clickedResetEvaluationPoints = GUILayout.Button(new GUIContent("Reset", "Reset the evaluation points for this configuration"), defaultOption);
            clickedPlaceEvaluationPoints = GUILayout.Button(new GUIContent("Place", "Place the evaluation points for this configuration"), defaultOption);
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space();

        m_ShowOptimization.target = EditorGUILayout.ToggleLeft(new GUIContent("2. Simplification", "Simplification Fields"), m_ShowOptimization.target, EditorStylesMainAction);
        if (EditorGUILayout.BeginFadeGroup(m_ShowOptimization.faded)) {
            if (simpleGUI) {

                GUILayout.BeginHorizontal();
                clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes (Async)", "Bake light probes"), defaultOption);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                clickedDecimateLightProbes = script.populateGUI_LightProbesDecimated(true);
            } else {

                GUILayout.BeginHorizontal();
                clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes (Async)", "Bake light probes"), defaultOption);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.1. Remove Invalid Objects", EditorStylesSubAction);
                EditorGUILayout.Space();

                //EditorGUILayout.LabelField("2.1.1. Light Probes", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                clickedRemoveInvalidLightProbes = GUILayout.Button(new GUIContent("Remove Invalid Light Probes", "Remove invalid light probes"), defaultOption);
                GUILayout.EndHorizontal();
                script.populateGUI_LightProbesSimplified();

                EditorGUILayout.Space();
                //EditorGUILayout.LabelField("2.1.2. Evaluation Points", EditorStyles.boldLabel);

                EditorGUILayout.Space();
                clickedMapEvaluationPointsToProbes = GUILayout.Button(new GUIContent("Map EP to LP", "Map Evaluation Points to Light Probes Tetrahedrons"), defaultOption);

                GUILayout.BeginHorizontal();
                clickedRemoveInvalidEvaluationPoints = GUILayout.Button(new GUIContent("Remove Invalid Evaluation Points", "Remove invalid evaluation points"), defaultOption);
                GUILayout.EndHorizontal();
                script.populateGUI_EvaluationPointsSimplified();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2. Graph Reduction", EditorStylesSubAction);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2.1. Evaluation", EditorStyles.boldLabel);
                //clickedEvaluateEvaluationPoints = script.populateGUI_LightProbesEvaluated();
                clickedEvaluateEvaluationPoints = script.populateGUI_LightProbesEvaluated();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2.2. Decimation", EditorStyles.boldLabel);
                clickedDecimateLightProbes = script.populateGUI_LightProbesDecimated(false);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("3. Optimization", EditorStylesMainAction);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("3.1. Refinement", EditorStylesSubAction);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }
        EditorGUILayout.EndFadeGroup();

        clickedSuccess = true;
        return (clickedSuccess, clickedResetLightProbes, clickedPlaceLightProbes,
                clickedResetEvaluationPoints, clickedPlaceEvaluationPoints, clickedMapEvaluationPointsToProbes,
                clickedBakeLightProbes,
                clickedRemoveInvalidLightProbes, clickedRemoveInvalidEvaluationPoints,
                clickedEvaluateEvaluationPoints, clickedDecimateLightProbes);
    }
    #endregion
}
