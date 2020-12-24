using UnityEngine;
using UnityEditor;
using System.Diagnostics;

[CustomEditor(typeof(LumibricksScript))]
public class LightProbesEditor : Editor
{
#region Private Variables
    Stopwatch stopwatch = null;
    GUIStyle  EditorStylesHeader;
    GUIStyle  EditorStylesMainAction;
    GUIStyle  EditorStylesSubAction;
#endregion

#region Public Override Functions
    public override void OnInspectorGUI()
    {
        LumibricksScript script = (LumibricksScript)target;

        float scale = 0.9f;
        Color colorHeader     = new Color(1.0f, 0.65f, 0.0f);
        Color colorMainAction = new Color(1.0f, 0.65f, 0.0f);
        Color colorSubAction  = new Color(0.95f, 0.95f, 0.0f);

        EditorStylesHeader                      = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesHeader.normal.textColor     = new Color(colorHeader.r * scale, colorHeader.g * scale, colorHeader.b * scale);
        EditorStylesMainAction                  = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesMainAction.normal.textColor = new Color(colorMainAction.r * scale, colorMainAction.g * scale, colorMainAction.b * scale);
        EditorStylesSubAction                   = new GUIStyle(EditorStyles.boldLabel);
        EditorStylesSubAction.normal.textColor  = new Color(colorSubAction.r * scale, colorSubAction.g * scale, colorSubAction.b * scale);

        // Set LightProbeGroup
        var component = script.GetComponent<LightProbeGroup>();
        if (component == null)
        {
            UnityEngine.Debug.LogWarning("Not a LightProbeGroup component");
            return;
        }
        script.LightProbeGroup = component;

        // generate GUI Elements
        var  clickedElements                 = populateInspectorGUI();
        bool clickedSuccess                  = clickedElements.Item1;
        bool clickedResetLightProbes         = clickedElements.Item2;
        bool clickedPlaceLightProbes         = clickedElements.Item3;
        bool clickedResetEvaluationPoints    = clickedElements.Item4;
        bool clickedPlaceEvaluationPoints    = clickedElements.Item5;
        bool clickedMapEvaluationPointsToLightProbes  = clickedElements.Item6;
        bool clickedBakeLightProbes          = clickedElements.Item7;
        bool clickedSimplifyLightProbes      = clickedElements.Item8;
        bool clickedSimplifyEvaluationPoints = clickedElements.Item9;
        bool clickedEvaluateEvalautionPoints = clickedElements.Item10;
        bool clickedDecimateLightProbes      = clickedElements.Item11;

        if (!clickedSuccess                  &&
            !clickedResetLightProbes         && !clickedPlaceLightProbes      &&
            !clickedResetEvaluationPoints    && !clickedPlaceEvaluationPoints && !clickedMapEvaluationPointsToLightProbes &&
            !clickedBakeLightProbes          && !clickedSimplifyLightProbes   && 
            !clickedSimplifyEvaluationPoints && !clickedEvaluateEvalautionPoints && !clickedDecimateLightProbes)
        {
            return;
        }

        // Start Process - Clear Light Probes
        if (clickedResetLightProbes) {
            float placems = ResetLightProbes();
            UnityEngine.Debug.Log("Done (ResetLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Light Probes
        if (clickedPlaceLightProbes)
        {
            float placems = PlaceLightProbes();
            UnityEngine.Debug.Log("Done (PlaceLightProbes: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Clear Evaluation Points
        if (clickedResetEvaluationPoints) {
            float placems = ResetEvaluationPoints();
            UnityEngine.Debug.Log("Done (ResetEvaluationsPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Place Evaluation Points
        if (clickedPlaceEvaluationPoints) {
            float placems = PlaceEvaluationPoints();
            UnityEngine.Debug.Log("Done (PlaceEvaluationPoints: " + placems / 1000.0 + "s)");
            FinishProcess();
        }

        if (clickedMapEvaluationPointsToLightProbes) {
            float mappings = MapEvaluationPointsToLightProbes();
            UnityEngine.Debug.Log("Done (MapEvaluationPointsToLightProbes: " + mappings / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Bake 
        if (clickedBakeLightProbes)
        {
            if (!Lightmapping.bakedGI)
            {
                UnityEngine.Debug.LogWarning("Baked GI option must be enabled to perform optimization");
                FinishProcess();
                return;
            }

            float bakeLPms        = BakeLightProbes();
            UnityEngine.Debug.Log("Done  (Bake: " + bakeLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Light Probes
        if (clickedSimplifyLightProbes)
        {
            float removeUnlitLPms = RemoveUnlitLightProbes();
            UnityEngine.Debug.Log("Done  (Remove Unlit LP: " + removeUnlitLPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Simplify Evaluation Points
        if (clickedSimplifyEvaluationPoints)
        {
            float removeUnlitEPms = RemoveUnlitEvaluationPoints();
            UnityEngine.Debug.Log("Done  (Remove Unlit EP: " + removeUnlitEPms / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Evaluate
        if (clickedEvaluateEvalautionPoints)
        {
            float evaluatems = EvaluateEvaluationPoints();
            UnityEngine.Debug.Log("Done  (Evaluate EP: " + evaluatems / 1000.0 + "s)");
            FinishProcess();
        }

        // Start Process - Decimate Light Probes
        if (clickedDecimateLightProbes)
        {
            float decimatems = DecimateLightProbes();
            UnityEngine.Debug.Log("Done  (Evaluate EP: " + decimatems / 1000.0 + "s)");
            FinishProcess();
        }
    }
#endregion

#region Private Functions
    void OnInspectorUpdate()
    {
        // Call Repaint on OnInspectorUpdate as it repaints the windows less times as if it was OnGUI/Update
        Repaint();
    }
    public void Awake()
    {
        UnityEngine.Debug.Log("Awake");
    }

    public void OnEnable()
    {
        UnityEngine.Debug.Log("OnEnable");
    }

    public void OnDisable()
    {
        UnityEngine.Debug.Log("OnDisable");
    }

    void OnDestroy() // [TODO] not activated - we have to destroy EP game objs !
    {
        UnityEngine.Debug.Log("OnDestroy entered");
        LumibricksScript script = (LumibricksScript)target;
        //script.Destroy();
    }

    void FinishProcess()
    {
        EditorUtility.ClearProgressBar();
        GUIUtility.ExitGUI();
    }

    float PlaceLightProbes()
    {
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

    float ResetLightProbes()
    {
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

    float PlaceEvaluationPoints()
    {
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

    float ResetEvaluationPoints()
    {
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

    float MapEvaluationPointsToLightProbes()
    {
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

    float BakeLightProbes()
    {
        stopwatch = Stopwatch.StartNew();
        EditorUtility.DisplayProgressBar("Clear Caches", "Clean", 0f);
        Lightmapping.ClearDiskCache();
        Lightmapping.ClearLightingDataAsset();
        EditorUtility.DisplayProgressBar("Clear Caches", "Clean", 1f);

        EditorUtility.DisplayProgressBar("Bake", "Bake", 0f);
        Lightmapping.BakeAsync();
        EditorUtility.DisplayProgressBar("Bake", "Bake", 1f);
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float RemoveUnlitLightProbes()
    {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Remove unlit Light Probes", "Remove", 0f);
            script.RemoveUnlitLightProbes();
            EditorUtility.DisplayProgressBar("Remove unlit Light Probes", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float RemoveUnlitEvaluationPoints()
    {
        LumibricksScript script = (LumibricksScript)target;

        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Remove unlit Evaluation Points", "Remove", 0f);
            script.RemoveUnlitEvaluationPoints();
            EditorUtility.DisplayProgressBar("Remove unlit Evaluation Points", "Remove", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    float EvaluateEvaluationPoints()
    {
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
    float DecimateLightProbes()
    {
        LumibricksScript script = (LumibricksScript)target;
        
        stopwatch = Stopwatch.StartNew();
        {
            EditorUtility.DisplayProgressBar("Decimate light probes", "Decimate", 0f);
            script.DecimateLightProbes();
            EditorUtility.DisplayProgressBar("Decimate light probes", "Decimate", 1f);
        }
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    (bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool) populateInspectorGUI()
    {
        bool clickedSuccess = false;
        bool clickedResetLightProbes = false;
        bool clickedPlaceLightProbes = false;
        bool clickedResetEvaluationPoints = false;
        bool clickedPlaceEvaluationPoints = false;
        bool clickedMapEvaluationPointsToProbes = false;
        bool clickedBakeLightProbes = false;
        bool clickedRemoveUnlitLightProbes = false;
        bool clickedRemoveUnlitEvaluationPoints = false;
        bool clickedEvaluateEvaluationPoints = false;
        bool clickedDecimateLightProbes = false;

        LumibricksScript script = (LumibricksScript)target;
        if (!script.Init())
        {
            return (clickedSuccess, clickedResetLightProbes     , clickedPlaceLightProbes,
                clickedResetEvaluationPoints, clickedPlaceEvaluationPoints, clickedMapEvaluationPointsToProbes,
                clickedBakeLightProbes      , 
                clickedRemoveUnlitLightProbes, clickedRemoveUnlitEvaluationPoints,
                clickedEvaluateEvaluationPoints, clickedDecimateLightProbes);
        }

        EditorGUILayout.LabelField("Light Probes Cut Algorithm", EditorStylesHeader);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Initialization", EditorStylesMainAction);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("1.1. Light Probes (LP)", EditorStylesSubAction);
        script.populateGUI_LightProbes();

        GUILayoutOption[] defaultOption = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500) };
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
       
        EditorGUILayout.Space();
        clickedMapEvaluationPointsToProbes = GUILayout.Button(new GUIContent("Map EP to LP" , "Map Evaluation Points to Light Probes Tetrahedrons"), defaultOption);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Simplification", EditorStylesMainAction);

        GUILayout.BeginHorizontal();
        clickedBakeLightProbes = GUILayout.Button(new GUIContent("Bake Light Probes", "Bake light probes"), defaultOption);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2.1. Remove Unlit Objects", EditorStylesSubAction);
        EditorGUILayout.Space();

        //EditorGUILayout.LabelField("2.1.1. Light Probes", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        clickedRemoveUnlitLightProbes = GUILayout.Button(new GUIContent("Remove Unlit Light Probes", "Remove unlit light probes"), defaultOption);
        GUILayout.EndHorizontal();
        script.populateGUI_LightProbesSimplified();

        EditorGUILayout.Space();
        //EditorGUILayout.LabelField("2.1.2. Evaluation Points", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        clickedRemoveUnlitEvaluationPoints = GUILayout.Button(new GUIContent("Remove Unlit Evaluation Points", "Remove unlit evaluation points"), defaultOption);
        GUILayout.EndHorizontal();
        script.populateGUI_EvaluationPointsSimplified();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2.2. Graph Reduction", EditorStylesSubAction);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2.2.1. Evaluation", EditorStyles.boldLabel);

        script.EvaluationType = (LumibricksScript.LightProbesEvaluationType)EditorGUILayout.EnumPopup(new GUIContent("Type:", "The probe evaluation method"), script.EvaluationType);
        if (script.EvaluationType == LumibricksScript.LightProbesEvaluationType.Random)
        {
            int prevCount = script.evaluationRandomSamplingCount;
            script.evaluationRandomSamplingCount = EditorGUILayout.IntSlider(new GUIContent("Number of Directions:", "The total number of uniform random sampled directions"), script.evaluationRandomSamplingCount, 25, 500);
            if (prevCount != script.evaluationRandomSamplingCount)
                script.GenerateUniformSphereSampling();
        }
        else
            EditorGUILayout.LabelField(new GUIContent("Number of Directions:", "The total number of evaluation directions"), new GUIContent(script.evaluationFixedCount[(int)script.EvaluationType].ToString()));

        GUILayout.BeginHorizontal();
        clickedEvaluateEvaluationPoints = GUILayout.Button(new GUIContent("Evaluate", "Evaluate evaluation points"), GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500));
        GUILayout.EndHorizontal();
        script.populateGUI_LightProbesEvaluated();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2.2.2. Decimation", EditorStyles.boldLabel);
        script.terminationMinLightProbes  = EditorGUILayout.IntSlider(new GUIContent("Minimum set:"  , "The minimum desired number of light probes")     , script.terminationMinLightProbes , 1, script.terminationMaxLightProbes);
        script.terminationEvaluationError = EditorGUILayout.Slider   (new GUIContent("Minimum error:", "The minimum desired evaluation percentage error"), script.terminationEvaluationError, 0.0f, 100.0f);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        clickedDecimateLightProbes = GUILayout.Button(new GUIContent("Decimate", "Decimate light probes"), GUILayout.ExpandWidth(true), GUILayout.MinWidth(150), GUILayout.MaxWidth(1500));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        script.populateGUI_LightProbesDecimatedEvaluated();
        //script.populateGUI_LightProbesDecimated();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3. Optimization", EditorStylesMainAction);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3.1. Refinement", EditorStylesSubAction);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        clickedSuccess = true;
        return (clickedSuccess, clickedResetLightProbes     , clickedPlaceLightProbes,
                clickedResetEvaluationPoints, clickedPlaceEvaluationPoints, clickedMapEvaluationPointsToProbes,
                clickedBakeLightProbes      , 
                clickedRemoveUnlitLightProbes, clickedRemoveUnlitEvaluationPoints,
                clickedEvaluateEvaluationPoints, clickedDecimateLightProbes);
    }
#endregion
}
