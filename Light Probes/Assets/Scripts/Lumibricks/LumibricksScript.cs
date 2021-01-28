using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class LumibricksScript : MonoBehaviour
{
    #region Public Enum Types
    public enum PlacementType {
        Grid,
        Random,
        Stratified,
        Poisson,
        NavMesh,
        NavMeshVolume
    }
    #endregion

    #region Private Variables
    GameObject sceneVolumeLPprev;
    public GameObject sceneVolumeLP;
    Bounds sceneVolumeLPBounds;
    GameObject sceneVolumeEPprev;
    public GameObject sceneVolumeEP;
    Bounds sceneVolumeEPBounds;
    Material EPMaterial = null;

    Evaluator m_evaluator = null;

    List<SphericalHarmonicsL2> LightProbesBakedProbes = null;

    public GeneratorInterface currentLightProbesGenerator = null;
    public GeneratorInterface currentEvaluationPointsGenerator = null;
    Dictionary<PlacementType, GeneratorInterface> generatorListLightProbes;
    Dictionary<PlacementType, GeneratorInterface> generatorListEvaluationPoints;
    #endregion

    #region Public Variables

    public LightProbeGroup LightProbeGroup { get; set; } = null;
    public PlacementType LightProbesPlaceType { get; set; } = PlacementType.Grid;
    public PlacementType EvaluationPositionsPlaceType { get; set; } = PlacementType.Poisson;
    #endregion

    #region Constructor Functions
    public LumibricksScript() {
        LumiLogger.Logger.Log("LumiScript Constructor");
        m_evaluator = new Evaluator();
    }
    #endregion

    #region Public Functions
    public bool Init() {
        var component = GetComponent<LightProbeGroup>();
        var nv = component.GetComponentInChildren<NavMeshAgent>();

        if (generatorListLightProbes != null || generatorListEvaluationPoints != null) {
            if (nv != null && nv.isOnNavMesh && !generatorListLightProbes.ContainsKey(PlacementType.NavMesh)) {
                // dynamically load NavMesh components
                generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

                generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);
                LumiLogger.Logger.Log("Nav mesh volume detected. Loading NavMesh elements");
            } else if (nv == null && generatorListLightProbes.ContainsKey(PlacementType.NavMesh)) {
                generatorListLightProbes.Remove(PlacementType.NavMesh);
                generatorListLightProbes.Remove(PlacementType.NavMeshVolume);
                generatorListEvaluationPoints.Remove(PlacementType.NavMesh);
                generatorListEvaluationPoints.Remove(PlacementType.NavMeshVolume);
                LumiLogger.Logger.LogWarning("No nav mesh volume defined. NavMesh elements will not be loaded");
            }
            return true;
        }
        LumiLogger.Logger.Log("Init");
        EPMaterial = new Material(Shader.Find("Unlit/Color"));
        EPMaterial.color = new Color(0.87f, 0.55f, 0.15f);

        generatorListLightProbes = new Dictionary<PlacementType, GeneratorInterface>();
        generatorListLightProbes[PlacementType.Grid] = new GeneratorGrid();
        generatorListLightProbes[PlacementType.Random] = new GeneratorRandom();
        generatorListLightProbes[PlacementType.Stratified] = new GeneratorStratified();
        generatorListLightProbes[PlacementType.Poisson] = new GeneratorPoisson();

        generatorListEvaluationPoints = new Dictionary<PlacementType, GeneratorInterface>();
        generatorListEvaluationPoints[PlacementType.Grid] = new GeneratorGrid();
        generatorListEvaluationPoints[PlacementType.Random] = new GeneratorRandom();
        generatorListEvaluationPoints[PlacementType.Stratified] = new GeneratorStratified();
        generatorListEvaluationPoints[PlacementType.Poisson] = new GeneratorPoisson();

        if (nv == null) {
            LumiLogger.Logger.LogWarning("No nav mesh agent component. NavMesh placement types will not be loaded");
            return false;
        }
        if (!nv.isOnNavMesh) {
            LumiLogger.Logger.LogWarning("Nav mesh agent is not bound to any nav mesh. NavMesh placement types will not be loaded");
            return false;
        }

        generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        return true;
    }

    public void populateGUI_LightProbes() {
        sceneVolumeLP = EditorGUILayout.ObjectField("LP Volume:", sceneVolumeLP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The LP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof(string));
        PlacementType newLightProbesPlaceType = (PlacementType)EditorGUILayout.Popup((int)LightProbesPlaceType, options, CustomStyles.defaultGUILayoutOption);
        EditorGUILayout.EndHorizontal();

        //LightProbesPlaceType = (LumibricksScript.PlacementType)EditorGUILayout.EnumPopup(new GUIContent("Placement Type:", "The LP placement method"), LightProbesPlaceType);
        currentLightProbesGenerator = generatorListLightProbes[newLightProbesPlaceType];
        currentLightProbesGenerator.populateGUI_Initialization();

        if (LightProbesPlaceType != newLightProbesPlaceType) {
            LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
            LightProbesPlaceType = newLightProbesPlaceType;
        }
    }

    public void populateGUI_EvaluationPoints() {
        sceneVolumeEP = EditorGUILayout.ObjectField("EP Volume:", sceneVolumeEP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The EP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof(string));
        PlacementType newEvaluationPositionsPlaceType = (PlacementType)EditorGUILayout.Popup((int)EvaluationPositionsPlaceType, options, CustomStyles.defaultGUILayoutOption);
        EditorGUILayout.EndHorizontal();

        //EvaluationPositionsPlaceType = (LumibricksScript.PlacementType)EditorGUILayout.EnumPopup(new GUIContent("Placement Type:", "The EP placement method"), EvaluationPositionsPlaceType);
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.populateGUI_Initialization();

        if (EvaluationPositionsPlaceType != newEvaluationPositionsPlaceType) {
            EvaluationPositionsPlaceType = newEvaluationPositionsPlaceType;
            foreach (PlacementType type in Enum.GetValues(typeof(PlacementType))) {
                GameObject evaluationObjectParent = GameObject.Find("EvaluationGroup_" + type.ToString());
                if (evaluationObjectParent == null) {
                    continue;
                }
                if (type == newEvaluationPositionsPlaceType) {
                    evaluationObjectParent.transform.localScale = new Vector3(1, 1, 1);
                } else {
                    evaluationObjectParent.transform.localScale = new Vector3(0, 0, 0);
                }
            }
        }
    }

    public void populateGUI_DecimateSettings() {
        m_evaluator.populateGUI_DecimateSettings();
    }
    public bool populateGUI_Decimate() {
        return m_evaluator.populateGUI_Decimate(this, currentEvaluationPointsGenerator);
    }

    public bool isBaking() {
        return Lightmapping.bakedGI && Lightmapping.isRunning;
    }
    public bool CanPlaceProbes() {
        return Lightmapping.bakedGI && !Lightmapping.isRunning;
    }
    public bool CanUseScript() {
        return Lightmapping.bakedGI;
    }
    public bool CanDecimate() {
        return
            Lightmapping.bakedGI &&
            !Lightmapping.isRunning &&
            currentLightProbesGenerator.TotalNumProbes > 0 &&
            currentEvaluationPointsGenerator.TotalNumProbes > 0;
    }

    public void PlaceLightProbes(bool reset) {
        if (!UpdateSceneVolume(ref sceneVolumeLP, ref sceneVolumeLPprev, ref sceneVolumeLPBounds)) {
            return;
        }
        GenerateLightProbes(reset);

        m_evaluator.ResetLightProbeData(currentLightProbesGenerator.TotalNumProbes);
        m_evaluator.ResetTime();
    }

    public void ResetLightProbes(bool reset) {
        if (generatorListLightProbes == null) {
            return;
        }

        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.Reset();
        // Place the Light Probes
        PlaceLightProbes(reset);
    }
    public void PlaceEvaluationPoints(bool reset) {
        if (!UpdateSceneVolume(ref sceneVolumeEP, ref sceneVolumeEPprev, ref sceneVolumeEPBounds)) {
            return;
        }
        GenerateEvaluationPoints(reset);
        m_evaluator.ResetEvaluationData();
        m_evaluator.ResetTime();
    }

    public void ResetEvaluationPoints(bool reset) {
        if (generatorListEvaluationPoints == null) {
            return;
        }

        // Clear Positions
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.Reset();
        // Place the Evaluation Points
        PlaceEvaluationPoints(reset);
    }
    public void Bake(bool reset) {
        if (isBaking()) {
            Lightmapping.Cancel();
            LumiLogger.Logger.Log("Bake cancelled");
            return;
        }
        Lightmapping.BakeAsync();
        LumiLogger.Logger.Log("Bake started");
    }

    #endregion

    #region Private Functions
    private void Reset() {
        LumiLogger.Logger.Log("Reset entered");

        m_evaluator.Reset(currentLightProbesGenerator.TotalNumProbes);

        LightProbesPlaceType = PlacementType.Grid;
        EvaluationPositionsPlaceType = PlacementType.Poisson;

        DestroyImmediate(EPMaterial);
        ResetLightProbes(true);
        ResetEvaluationPoints(true);
    }
    private ArrayList populatePlacementPopup() {
        ArrayList options = new ArrayList();
        options.AddRange(Enum.GetNames(typeof(PlacementType)));
        if (!generatorListLightProbes.ContainsKey(PlacementType.NavMesh)) {
            options.Remove(PlacementType.NavMesh.ToString());
            options.Remove(PlacementType.NavMeshVolume.ToString());
            if (LightProbesPlaceType == PlacementType.NavMesh || LightProbesPlaceType == PlacementType.NavMeshVolume) {
                LightProbesPlaceType = PlacementType.Poisson;
            }
            if (EvaluationPositionsPlaceType == PlacementType.NavMesh || EvaluationPositionsPlaceType == PlacementType.NavMeshVolume) {
                EvaluationPositionsPlaceType = PlacementType.Random;
            }
        }
        return options;
    }
    private bool UpdateSceneVolume(ref GameObject current, ref GameObject prev, ref Bounds bounds) {
        bool no_error = true;
        //if (current != prev)
        {
            no_error = ComputeSceneVolume(ref current, ref bounds);
            prev = current;
        }
        return no_error;
    }

    private bool ComputeSceneVolume(ref GameObject gameObject, ref Bounds bounds) {
        List<Renderer> renderers = new List<Renderer>();

        if (gameObject != null) {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null) {
                LumiLogger.Logger.LogError("No renderer found for GameObject: " + gameObject.name);
                return false;
            }
            renderers.Add(renderer);
        } else {
            // Compute Scene's Bounding Box
            Renderer[] rnds = FindObjectsOfType<Renderer>();
            if (rnds.Length == 0) {
                return false;
            }
            renderers.AddRange(rnds);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++) {
            b.Encapsulate(renderers[i].bounds);
        }

        bounds = b;
        return true;
    }

    private void GenerateLightProbes(bool reset) {
        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];

        // Compute Positions
        if (!reset) {
            currentLightProbesGenerator.GeneratePositions(sceneVolumeLPBounds);
        }

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
    }

    private void GenerateEvaluationPoints(bool reset) {
        // Get Generator
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];

        // Compute Positions
        if (reset) {
            // Destroy the Evaluation Points
            foreach (var generator in generatorListEvaluationPoints.Keys) {
                DestroyEvaluationPoints(generatorListEvaluationPoints[generator]);
            }
            return;
        }

        List<Vector3> evaluationPositions = currentEvaluationPointsGenerator.GeneratePositions(sceneVolumeEPBounds);

        // Destroy the Evaluation Points
        DestroyEvaluationPoints(currentEvaluationPointsGenerator);

        // Generate new ones
        GameObject evaluationObjectParent = new GameObject("EvaluationGroup_" + currentEvaluationPointsGenerator.GeneratorName);
        GameObject defaultobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        defaultobj.transform.localScale = new Vector3(0.0625f, 0.0625f, 0.0625f);
        MeshRenderer renderer = defaultobj.GetComponent<MeshRenderer>();
        if (renderer) {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.receiveGI = ReceiveGI.LightProbes;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.sharedMaterial = EPMaterial;
        }
        MeshCollider collider = defaultobj.GetComponent<MeshCollider>();
        if (collider) {
            collider.enabled = false;
        }
        SetEvaluationPointProperties(defaultobj, evaluationPositions[0], 0, evaluationObjectParent);
        for (int i = 1; i < evaluationPositions.Count; i++) {
            GameObject obj = Instantiate(defaultobj, evaluationPositions[i], Quaternion.identity);
            SetEvaluationPointProperties(obj, evaluationPositions[i], i, evaluationObjectParent);
        }
    }
    private void DestroyEvaluationPoints(GeneratorInterface generator) {
        GameObject evaluationObjectParent = GameObject.Find("EvaluationGroup_" + generator.GeneratorName);
        if (evaluationObjectParent == null) {
            //LumiLogger.Logger.LogWarning("Could not find object: " + "EvaluationGroup_" + generator.GeneratorName);
            return;
        }
        Transform[] evaluationObjectsTransforms = evaluationObjectParent.GetComponentsInChildren<Transform>();
        if (evaluationObjectsTransforms.Length == 0) {
            return;
        }
        foreach (Transform child in transform) {
            DestroyImmediate(child.gameObject);
        }
        DestroyImmediate(evaluationObjectParent);
    }

    private void SetEvaluationPointProperties(GameObject obj, Vector3 position, int index, GameObject parenttobject) {
        obj.transform.position = position;
        obj.transform.parent = parenttobject.transform;
        obj.SetActive(true);
        obj.name = "Evaluation Point " + index.ToString();
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) {
            collider.enabled = false;
        }
    }

    public void DecimateLightProbes(bool reset) {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // STEP 1. Bake
        Lightmapping.Bake();        
        LightProbesBakedProbes = new List<SphericalHarmonicsL2>(LightmapSettings.lightProbes.bakedProbes);

        // STEP 2. Reset light probe and evaluation data
        // set light probes
        currentLightProbesGenerator.TotalNumProbes = currentLightProbesGenerator.Positions.Count;
        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
        int userSelectedLightProbes = m_evaluator.terminationCurrentLightProbes;
        // reset to default state
        m_evaluator.ResetLightProbeData(currentLightProbesGenerator.TotalNumProbes);
        // set terminating LP condition according to user selection
        m_evaluator.SetLightProbeUserSelection(userSelectedLightProbes);

        // STEP 3. Map EP to LP
        m_evaluator.Tetrahedralize(currentLightProbesGenerator.Positions);
        int mapped = m_evaluator.MapEvaluationPointsToLightProbes(currentLightProbesGenerator.Positions, currentEvaluationPointsGenerator.Positions);
        LumiLogger.Logger.Log("Mapped " + (mapped / (float)(currentEvaluationPointsGenerator.Positions.Count)).ToString("0.00%") + " of EPs: " +
            mapped.ToString() + " out of " + currentEvaluationPointsGenerator.Positions.Count + " (" + (currentEvaluationPointsGenerator.Positions.Count - mapped).ToString() + " unmapped)");

        // STEP 4. Generate reference
        m_evaluator.GenerateReferenceEvaluationPoints(LightProbesBakedProbes, currentEvaluationPointsGenerator.Positions);
        LumiLogger.Logger.Log("Generate reference EP");

        // STEP 5. Run decimation
        int num_probes_before_decimation = currentLightProbesGenerator.Positions.Count;
        var result = m_evaluator.DecimateBakedLightProbes(this, currentEvaluationPointsGenerator.Positions, currentLightProbesGenerator.Positions, LightProbesBakedProbes);

        // STEP 6. Finalize
        currentLightProbesGenerator.Positions = result;
        m_evaluator.finalLightProbes = currentLightProbesGenerator.Positions.Count;
        m_evaluator.decimatedLightProbes = num_probes_before_decimation - currentLightProbesGenerator.Positions.Count;

        LumiLogger.Logger.Log("Decimated " + m_evaluator.decimatedLightProbes.ToString() + " light probes, " + m_evaluator.finalLightProbes + " left");
        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
 
        stopwatch.Stop();
        m_evaluator.totalTime = (float)(stopwatch.ElapsedMilliseconds / 1000.0);
    }

    #endregion
}
