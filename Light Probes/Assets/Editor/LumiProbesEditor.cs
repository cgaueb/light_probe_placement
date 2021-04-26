using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(LumiProbesScript))]
public class LightProbesEditor : Editor
{
    #region Private Variables
    private AnimBool ShowPlacedObjects;
    private AnimBool ShowConfiguration;
    int selectionGridIndex = 0;
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
        public bool clickedShowRemovedLightProbes = false;

        public void Reset() {
            clickedSuccess = false;
            clickedResetLightProbes = false;
            clickedPlaceLightProbes = false;
            clickedResetEvaluationPoints = false;
            clickedPlaceEvaluationPoints = false;
            clickedBakeLightProbes = false;
            clickedDecimateLightProbes = false;
            clickedShowRemovedLightProbes = false;
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

        LumiProbesScript script = (LumiProbesScript)target;
        CustomStyles.setStyles();

        // Set LightProbeGroup
        script.SetLightProbeGroup();
        if (script.LightProbeGroup == null) {
            return;
        }

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

        // Start Process - Decimate Light Probes
        if (populateGUIResponse.clickedShowRemovedLightProbes) {
            RunFunc(script.ShowRemovedLightProbes, "Show Removed Light Probes", "Show Removed");
        }
    }
    #endregion

    #region Private Functions
    private void FinishProcess() {
        EditorUtility.ClearProgressBar();
        GUIUtility.ExitGUI();
    }
    private void Awake() {
        //LumiLogger.Logger.Log("Awake");
    }
    private void OnDestroy() // [TODO] not activated - we have to destroy EP game objs !
    {
        //LumiLogger.Logger.Log("OnDestroy");
        //LumiProbesScript script = (LumiProbesScript)target;
        //script.Destroy();
    }
    //Tool lastTool = Tool.None;
    private void OnEnable() {
        //LumiLogger.Logger.Log("OnEnable");
        //lastTool = Tools.current;
        //Tools.current = Tool.None;
        ShowPlacedObjects = new AnimBool(selectionGridIndex == 0 ? true : false);
        ShowPlacedObjects.valueChanged.AddListener(Repaint);
        ShowConfiguration = new AnimBool(selectionGridIndex == 1 ? true : false);
        ShowConfiguration.valueChanged.AddListener(Repaint);
    }
    private void OnDisable() {
        //LumiLogger.Logger.Log("OnDisable");
        //Tools.current = lastTool;
        LumiProbesScript script = (LumiProbesScript)target;
    }

    public Vector2 scrollPosition = Vector2.zero;
    private bool populateInspectorGUI() {

        populateGUIResponse.Reset();
        LumiProbesScript script = (LumiProbesScript)target;
        if (!script.Init()) {
            return false;
        }

        if (!script.CanUseScript()) {
            EditorGUILayout.LabelField("Warning! Baked Global Illumination must be enabled to use this script!", CustomStyles.EditorErrorRed);
            EditorGUILayout.Space();
        }

        EditorGUI.BeginDisabledGroup(!script.CanUseScript());
        {
            EditorGUILayout.LabelField("Illumination-driver Light Probe Placement", CustomStyles.EditorStylesHeader);
            EditorGUILayout.Space();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!script.CanUseScript());
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Debug Settings", CustomStyles.EditorStylesMainAction);
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    bool clickedVolumesDebug = GUILayout.Button(new GUIContent("Auto Set Volumes", "Auto adds a TestVolumeLP and TestVolumeEP gameobject to LP and EPs if they exist (debug feature)"), CustomStyles.defaultGUILayoutOption);
                    if (clickedVolumesDebug) {
                        script.populateGUI_LoadDebugVolumes();
                    }
                    populateGUIResponse.clickedBakeLightProbes = GUILayout.Button(
                        !script.isBaking() ? new GUIContent("Bake Light Probes (Async)", "Bake light probes") : new GUIContent("Cancel Bake", "Cancel Baking of light probes")
                        , CustomStyles.defaultGUILayoutOption);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(style);
            {
                EditorGUILayout.LabelField("Configuration", CustomStyles.EditorStylesMainAction);
                EditorGUILayout.Space();
                GUIContent[] selStrings = { new GUIContent("Placement", ""), new GUIContent("Settings", "") };
                selectionGridIndex = GUILayout.SelectionGrid(selectionGridIndex, selStrings, 2, CustomStyles.defaultGUILayoutOption);
                EditorGUILayout.Space();
                if (selectionGridIndex == 0) {
                    ShowPlacedObjects.target = true;
                    ShowConfiguration.target = false;
                } else {
                    ShowPlacedObjects.target = false;
                    ShowConfiguration.target = true;
                }
                //if (EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowPlacement.target, new GUIContent("1. Placement", "Placement Fields"), EditorStylesFoldoutMainAction)) { 
                if (EditorGUILayout.BeginFadeGroup(ShowPlacedObjects.faded)) {
                    EditorGUI.BeginDisabledGroup(!script.CanPlaceProbes());
                    EditorGUILayout.LabelField("Light Probes (LP)", CustomStyles.EditorStylesSubAction);
                    script.populateGUI_LightProbes();
                    GUILayout.BeginHorizontal();
                    populateGUIResponse.clickedResetLightProbes = GUILayout.Button(new GUIContent("Reset", "Reset the Light Probes for this configuration"), CustomStyles.defaultGUILayoutOption);
                    populateGUIResponse.clickedPlaceLightProbes = GUILayout.Button(new GUIContent("Place", "Place the Light Probes for this configuration"), CustomStyles.defaultGUILayoutOption);
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Evaluation Points (EP)", CustomStyles.EditorStylesSubAction);
                    script.populateGUI_EvaluationPoints();

                    GUILayout.BeginHorizontal();
                    populateGUIResponse.clickedResetEvaluationPoints = GUILayout.Button(new GUIContent("Reset", "Reset the evaluation points for this configuration"), CustomStyles.defaultGUILayoutOption);
                    populateGUIResponse.clickedPlaceEvaluationPoints = GUILayout.Button(new GUIContent("Place", "Place the evaluation points for this configuration"), CustomStyles.defaultGUILayoutOption);
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFadeGroup();
                if (EditorGUILayout.BeginFadeGroup(ShowConfiguration.faded)) {
                    EditorGUILayout.LabelField("Decimation Settings", CustomStyles.EditorStylesSubAction);
                    script.populateGUI_DecimateSettings();
                }
                EditorGUILayout.EndFadeGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!script.CanDecimate());
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Decimate Probes", CustomStyles.EditorStylesMainAction);
                    EditorGUILayout.Space();
                    populateGUIResponse.clickedDecimateLightProbes = script.populateGUI_Decimate();
                    populateGUIResponse.clickedShowRemovedLightProbes = script.populateGUI_ShowRemovedLP();
                }
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUI.EndDisabledGroup();

        return true;
    }
    
    private void RunFunc(System.Action<bool> func, string title, string msg, bool show_progress_bar = true) {
        // Start Process - Generate
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (show_progress_bar) {
            EditorUtility.DisplayProgressBar(title, msg, 0f);
        }
        func(false);
        if (show_progress_bar) {
            EditorUtility.DisplayProgressBar(title, msg, 1f);
        }
        stopwatch.Stop();
        LumiLogger.Logger.Log("Done (" + func.Method.Name.ToString() + ": " + stopwatch.ElapsedMilliseconds / 1000.0 + "s)");
        FinishProcess();
    }
    #endregion
}
