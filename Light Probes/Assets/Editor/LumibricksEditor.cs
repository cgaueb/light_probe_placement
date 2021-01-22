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
    GUIStyle EditorStylesHeader;
    GUIStyle EditorStylesMainAction;
    GUIStyle EditorStylesFoldoutMainAction;
    GUIStyle EditorStylesSubAction;
    #endregion

    class PopulateGUIResponse
    {
        public bool clickedSuccess = false;
        public bool clickedResetLightProbes = false;
        public bool clickedPlaceLightProbes = false;
        public bool clickedResetEvaluationPoints = false;
        public bool clickedPlaceEvaluationPoints = false;
        public bool clickedBakeLightProbes = false;
        public bool clickedDecimateLightProbes = false;

        public void Reset() {
            clickedSuccess = false;
            clickedResetLightProbes = false;
            clickedPlaceLightProbes = false;
            clickedResetEvaluationPoints = false;
            clickedPlaceEvaluationPoints = false;
            clickedBakeLightProbes = false;
            clickedDecimateLightProbes = false;
        }
        public bool anyClicked() {
            return !clickedResetLightProbes && !clickedPlaceLightProbes &&
            !clickedResetEvaluationPoints && !clickedPlaceEvaluationPoints &&
            !clickedBakeLightProbes &&
            !clickedDecimateLightProbes;
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
            RunFunc(script.ResetLightProbes, "Reset Light Probes", "Reset");
        }

        // Start Process - Place Light Probes
        if (populateGUIResponse.clickedPlaceLightProbes) {
            RunFunc(script.PlaceLightProbes, "Place Light Probes", "Place");
        }

        // Start Process - Clear Evaluation Points
        if (populateGUIResponse.clickedResetEvaluationPoints) {
            RunFunc(script.ResetEvaluationPoints, "Reset Evaluation Points", "Reset");
        }

        // Start Process - Place Evaluation Points
        if (populateGUIResponse.clickedPlaceEvaluationPoints) {
            RunFunc(script.PlaceEvaluationPoints, "Place Evaluation Points", "Place");
        }

        // Start Process - Bake 
        if (populateGUIResponse.clickedBakeLightProbes) {
            if (!Lightmapping.bakedGI) {
                LumiLogger.Logger.LogWarning("Baked GI option must be enabled to perform optimization");
                FinishProcess();
                return;
            }
            RunFunc(script.Bake, "Bake", "Bake");
        }

        // Start Process - Decimate Light Probes
        if (populateGUIResponse.clickedDecimateLightProbes) {
            RunFunc(script.DecimateLightProbes, "Decimate Light Probes", "Decimate", false);
        }
    }
    private void RunFunc(System.Action func, string title, string msg, bool show_progress_bar = true) {
        // Start Process - Generate
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (show_progress_bar) {
            EditorUtility.DisplayProgressBar(title, msg, 0f);
        }
        func();
        if (show_progress_bar) {
            EditorUtility.DisplayProgressBar(title, msg, 1f);
        }
        stopwatch.Stop();
        LumiLogger.Logger.Log("Done (" + func.Method.Name.ToString() + ": " + stopwatch.ElapsedMilliseconds / 1000.0 + "s)");
        FinishProcess();
    }

    #endregion

    #region Private Functions
    public void OnInspectorUpdate() {
        LumiLogger.Logger.Log("OnInspectorUpdate");
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

    bool populateInspectorGUI() {

        populateGUIResponse.Reset();
        LumibricksScript script = (LumibricksScript)target;
        if (!script.Init()) {
            return false;
        }
        GUILayoutOption[] defaultOption = new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MinWidth(50), GUILayout.MaxWidth(1500) };

        EditorGUILayout.LabelField("Light Probes Cut Algorithm", EditorStylesHeader);
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
            GUILayout.BeginHorizontal();
            populateGUIResponse.clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes (Async)", "Bake light probes"), defaultOption);
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            populateGUIResponse.clickedDecimateLightProbes = script.populateGUI_Decimate();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        //EditorGUILayout.EndFadeGroup();

        return true;        
    }
    #endregion
}
