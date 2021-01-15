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
    GUIStyle EditorStylesFoldoutMainAction;
    GUIStyle EditorStylesSubAction;
    bool clickedSimpleGUI = true;
    #endregion

    class PopulateGUIResponse
    {
        public bool clickedSuccess = false;
        public bool clickedResetLightProbes = false;
        public bool clickedPlaceLightProbes = false;
        public bool clickedResetEvaluationPoints = false;
        public bool clickedPlaceEvaluationPoints = false;
        public bool clickedMapEvaluationPointsToLightProbes = false;
        public bool clickedBakeLightProbes = false;
        public bool clickedRemoveInvalidLightProbes = false;
        public bool clickedRemoveInvalidEvaluationPoints = false;
        public bool clickedGenerateReferenceEvaluationPoints = false;
        public bool clickedDecimateLightProbes = false;

        public void Reset() {
            clickedSuccess = false;
            clickedResetLightProbes = false;
            clickedPlaceLightProbes = false;
            clickedResetEvaluationPoints = false;
            clickedPlaceEvaluationPoints = false;
            clickedMapEvaluationPointsToLightProbes = false;
            clickedBakeLightProbes = false;
            clickedRemoveInvalidLightProbes = false;
            clickedRemoveInvalidEvaluationPoints = false;
            clickedGenerateReferenceEvaluationPoints = false;
            clickedDecimateLightProbes = false;
        }
        public bool anyClicked() {
            return !clickedResetLightProbes && !clickedPlaceLightProbes &&
            !clickedResetEvaluationPoints && !clickedPlaceEvaluationPoints && !clickedMapEvaluationPointsToLightProbes &&
            !clickedBakeLightProbes && !clickedRemoveInvalidLightProbes &&
            !clickedRemoveInvalidEvaluationPoints && !clickedGenerateReferenceEvaluationPoints && !clickedDecimateLightProbes;
        }
    }

    private PopulateGUIResponse populateGUIResponse = new PopulateGUIResponse();

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
        EditorStylesFoldoutMainAction = new GUIStyle(EditorStyles.foldoutHeader);
        EditorStylesFoldoutMainAction.normal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.active.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.hover.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.focused.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onNormal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onActive.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onHover.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesFoldoutMainAction.onFocused.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);

        // Set LightProbeGroup
        var component = script.GetComponent<LightProbeGroup>();
        if (component == null) {
            LumiLogger.Logger.LogWarning("Not a LightProbeGroup component");
            return;
        }
        script.LightProbeGroup = component;

        // generate GUI Elements
        bool clickedSuccess = populateInspectorGUI();

        if (!clickedSuccess && !populateGUIResponse.anyClicked()) {
            return;
        }

        // Start Process - Clear Light Probes
        if (populateGUIResponse.clickedResetLightProbes) {
            float placems = ResetLightProbes();
            LumiLogger.Logger.Log("Done (ResetLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Light Probes
        if (populateGUIResponse.clickedPlaceLightProbes) {
            float placems = PlaceLightProbes();
            LumiLogger.Logger.Log("Done (PlaceLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Clear Evaluation Points
        if (populateGUIResponse.clickedResetEvaluationPoints) {
            float placems = ResetEvaluationPoints();
            LumiLogger.Logger.Log("Done (ResetEvaluationsPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Evaluation Points
        if (populateGUIResponse.clickedPlaceEvaluationPoints) {
            float placems = PlaceEvaluationPoints();
            LumiLogger.Logger.Log("Done (PlaceEvaluationPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        if (populateGUIResponse.clickedMapEvaluationPointsToLightProbes) {
            float mappings = MapEvaluationPointsToLightProbes();
            LumiLogger.Logger.Log("Done (MapEvaluationPointsToLightProbes: " + mappings / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Bake 
        if (populateGUIResponse.clickedBakeLightProbes) {
            if (!Lightmapping.bakedGI) {
                LumiLogger.Logger.LogWarning("Baked GI option must be enabled to perform optimization");
                FinishProcess();
                return;
            }

            float bakeLPms = BakeLightProbes();
            LumiLogger.Logger.Log("Done (Bake: " + bakeLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Light Probes
        if (populateGUIResponse.clickedRemoveInvalidLightProbes) {
            float removeInvalidLPms = RemoveInvalidLightProbes();
            LumiLogger.Logger.Log("Done (Remove Invalid LP: " + removeInvalidLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Evaluation Points
        if (populateGUIResponse.clickedRemoveInvalidEvaluationPoints) {
            float removeInvalidEPms = RemoveInvalidEvaluationPoints();
            LumiLogger.Logger.Log("Done (Remove Invalid EP: " + removeInvalidEPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Evaluate
        if (populateGUIResponse.clickedGenerateReferenceEvaluationPoints) {
            float evaluatems = GenerateReferenceEvaluationPoints();
            LumiLogger.Logger.Log("Done (Generate Reference EP: " + evaluatems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Decimate Light Probes
        if (populateGUIResponse.clickedDecimateLightProbes) {
            float decimatems = 0.0f;
            if (clickedSimpleGUI) {
                decimatems = DecimateLightProbes(true);
            } else {
                decimatems = DecimateLightProbes(false);
            }
            LumiLogger.Logger.Log("Done (Decimate LP: " + decimatems / 1000.0 + "s)");
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
            EditorUtility.DisplayProgressBar("Place Light Probes", "Place LP", 0f);
            script.PlaceLightProbes();
            EditorUtility.DisplayProgressBar("Place Light Probes", "Place LP", 1f);
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
            EditorUtility.DisplayProgressBar("Reset Light Probes", "Reset", 0f);
            script.ResetLightProbes();
            EditorUtility.DisplayProgressBar("Reset Light Probes", "Reset", 1f);
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
            EditorUtility.DisplayProgressBar("Place Evaluation Points", "Place", 0f);
            script.PlaceEvaluationPoints();
            EditorUtility.DisplayProgressBar("Place Evaluation Points", "Place", 1f);
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
            EditorUtility.DisplayProgressBar("Reset Evaluation Points", "Reset", 0f);
            script.ResetEvaluationPoints();
            EditorUtility.DisplayProgressBar("Reset Evaluation Points", "Reset", 1f);
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
            EditorUtility.DisplayProgressBar("Map Evaluation Points to Light Probes", "Map", 0f);
            script.MapEvaluationPointsToLightProbes();
            EditorUtility.DisplayProgressBar("Map Evaluation Points to Light Probes", "Map", 1f);
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
            EditorUtility.DisplayProgressBar("Remove Invalid Light Probes", "Remove", 0f);
            script.RemoveInvalidLightProbes(false);
            EditorUtility.DisplayProgressBar("Remove Invalid Light Probes", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float RemoveInvalidEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Remove Invalid Evaluation Points", "Remove", 0f);
            script.RemoveInvalidEvaluationPoints();
            EditorUtility.DisplayProgressBar("Remove Invalid Evaluation Points", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float GenerateReferenceEvaluationPoints() {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Generate Reference Evaluation Points", "Evaluate", 0f);
            script.GenerateReferenceEvaluationPoints();
            EditorUtility.DisplayProgressBar("Generate Reference Evaluation Points", "Evaluate", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }
    float DecimateLightProbes(bool executeAll) {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Decimate Light Probes", "Decimate", 0f);
            script.DecimateLightProbes(executeAll);
            EditorUtility.DisplayProgressBar("Decimate Light Probes", "Decimate", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    bool populateInspectorGUI() {

        populateGUIResponse.Reset();
        LumibricksScript script = (LumibricksScript)target;
        if (!script.Init()) {
            return false;
        }
        GUILayoutOption[] defaultOption = new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MinWidth(50), GUILayout.MaxWidth(1500) };

        EditorGUILayout.LabelField("Light Probes Cut Algorithm", EditorStylesHeader);
        EditorGUILayout.Space();

        clickedSimpleGUI = EditorGUILayout.ToggleLeft(new GUIContent("Simplified GUI", "Minimized GUI version"), clickedSimpleGUI, EditorStylesSubAction);
        EditorGUILayout.Space();

        bool clickedVolumesDebug = GUILayout.Button(new GUIContent("Auto set LP/EP Volumes (Debug)", ""), defaultOption);
        if (clickedVolumesDebug) {
            script.sceneVolumeLP = GameObject.Find("ATestVolumeLP");
            script.sceneVolumeEP = GameObject.Find("BTestVolumeEP");
        }

        //m_ShowPlacement.target = EditorGUILayout.ToggleLeft(new GUIContent("1. Placement", "Placement Fields"), m_ShowPlacement.target, EditorStylesMainAction);
        //if (EditorGUILayout.BeginFadeGroup(m_ShowPlacement.faded)) {
            
            m_ShowPlacement.target = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowPlacement.target, new GUIContent("1. Placement", "Placement Fields"), EditorStylesFoldoutMainAction);
            if (m_ShowPlacement.target) {
                EditorGUILayout.LabelField("1.1. Light Probes (LP)", EditorStylesSubAction);
                script.populateGUI_LightProbes();
                GUILayout.BeginHorizontal();
                populateGUIResponse.clickedResetLightProbes = GUILayout.Button(new GUIContent("Reset", "Reset the Light Probes for this configuration"), defaultOption);
                populateGUIResponse.clickedPlaceLightProbes = GUILayout.Button(new GUIContent("Place", "Place the Light Probes for this configuration"), defaultOption);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("1.2. Evaluation Points (EP)", EditorStylesSubAction);
                script.populateGUI_EvaluationPoints();

                // Created internally
                // evaluationPointGeometry = EditorGUILayout.ObjectField("Geometry Representation", evaluationPointGeometry, typeof(GameObject), true) as GameObject;

                GUILayout.BeginHorizontal();
                populateGUIResponse.clickedResetEvaluationPoints = GUILayout.Button(new GUIContent("Reset", "Reset the evaluation points for this configuration"), defaultOption);
                populateGUIResponse.clickedPlaceEvaluationPoints = GUILayout.Button(new GUIContent("Place", "Place the evaluation points for this configuration"), defaultOption);
                GUILayout.EndHorizontal();
            //}
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space();

        //m_ShowOptimization.target = EditorGUILayout.ToggleLeft(new GUIContent("2. Simplification", "Simplification Fields"), m_ShowOptimization.target, EditorStylesMainAction);
        //if (EditorGUILayout.BeginFadeGroup(m_ShowOptimization.faded)) {
        m_ShowOptimization.target = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowOptimization.target, new GUIContent("2. Decimation", "Decimation Fields"), EditorStylesFoldoutMainAction);
        if (m_ShowOptimization.target) {
            if (!clickedSimpleGUI) {
                GUILayout.BeginHorizontal();
                populateGUIResponse.clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes (Async)", "Bake light probes"), defaultOption);
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.1. Map Valid LP to EP", EditorStylesSubAction);
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                populateGUIResponse.clickedRemoveInvalidLightProbes = GUILayout.Button(new GUIContent("Remove Invalid Light Probes", "Remove Invalid Light Probes"), defaultOption);
                GUILayout.EndHorizontal();
                script.populateGUI_LightProbesRemoveInvalid();

                EditorGUILayout.Space();
                populateGUIResponse.clickedMapEvaluationPointsToLightProbes = GUILayout.Button(new GUIContent("Map EP to LP", "Map Evaluation Points to Light Probes Tetrahedrons"), defaultOption);

                //GUILayout.BeginHorizontal();
                //populateGUIResponse.clickedRemoveInvalidEvaluationPoints = GUILayout.Button(new GUIContent("Remove Invalid Evaluation Points", "Remove invalid evaluation points"), defaultOption);
                //GUILayout.EndHorizontal();
                //script.populateGUI_EvaluationPointsRemoveInvalid();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2. Graph Reduction", EditorStylesSubAction);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2.1. Generate Reference", EditorStyles.boldLabel);
                populateGUIResponse.clickedGenerateReferenceEvaluationPoints = script.populateGUI_GenerateReferenceEvaluationPoints();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("2.2.2. Decimation", EditorStyles.boldLabel);
                populateGUIResponse.clickedDecimateLightProbes = script.populateGUI_Decimate(false);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("3. Optimization", EditorStylesMainAction);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("3.1. Refinement", EditorStylesSubAction);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            GUILayout.BeginHorizontal();
            populateGUIResponse.clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes (Async)", "Bake light probes"), defaultOption);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            populateGUIResponse.clickedDecimateLightProbes = script.populateGUI_Decimate(clickedSimpleGUI);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //EditorGUILayout.EndFadeGroup();

        return true;        
    }
    #endregion
}
