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

    Evaluator m_evaluator = new Evaluator();

    List<SphericalHarmonicsL2> LightProbesBakedProbes = null;

    GeneratorInterface currentLightProbesGenerator = null;
    GeneratorInterface currentEvaluationPointsGenerator = null;
    Dictionary<PlacementType, GeneratorInterface> generatorListLightProbes;
    Dictionary<PlacementType, GeneratorInterface> generatorListEvaluationPoints;
    #endregion

    #region Public Variables

    public LightProbeGroup LightProbeGroup { get; set; } = null;
    public PlacementType LightProbesPlaceType { get; set; } = PlacementType.Grid;
    public PlacementType EvaluationPositionsPlaceType { get; set; } = PlacementType.Grid;
    #endregion

    #region Constructor Functions
    public LumibricksScript() {
        Debug.Log("Lumi Script Constructor");
    }
    #endregion

    #region Public Functions
    public bool Init() {
        var component = GetComponent<LightProbeGroup>();
        var nv = component.GetComponentInChildren<NavMeshAgent>();

        if (generatorListLightProbes != null || generatorListEvaluationPoints != null) {
            if (nv != null && !generatorListLightProbes.ContainsKey(PlacementType.NavMesh)) {
                // dynamically load NavMesh components
                generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

                generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);
                UnityEngine.Debug.Log("Nav mesh volume detected. Loading NavMesh elements");
            } else if (nv == null && generatorListLightProbes.ContainsKey(PlacementType.NavMesh)) {
                generatorListLightProbes.Remove(PlacementType.NavMesh);
                generatorListLightProbes.Remove(PlacementType.NavMeshVolume);
                generatorListEvaluationPoints.Remove(PlacementType.NavMesh);
                generatorListEvaluationPoints.Remove(PlacementType.NavMeshVolume);
                UnityEngine.Debug.LogWarning("No nav mesh volume defined. NavMesh elements will not be loaded");
            }
            return true;
        }
        Debug.Log("Init");
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
            UnityEngine.Debug.LogWarning("No nav mesh volume defined. NavMesh elements will not be loaded");
            return false;
        }

        generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        return true;
    }

    public void Reset() {
        Debug.Log("Reset entered");

        m_evaluator.Reset(currentLightProbesGenerator.TotalNumProbes);

        LightProbesPlaceType = PlacementType.Poisson;
        EvaluationPositionsPlaceType = PlacementType.Random;

        Destroy();
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

    public void populateGUI_LightProbes() {
        sceneVolumeLP = EditorGUILayout.ObjectField("LP Volume:", sceneVolumeLP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The LP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof(string));
        PlacementType newLightProbesPlaceType = (PlacementType)EditorGUILayout.Popup((int)LightProbesPlaceType, options);
        EditorGUILayout.EndHorizontal();

        //LightProbesPlaceType = (LumibricksScript.PlacementType)EditorGUILayout.EnumPopup(new GUIContent("Placement Type:", "The LP placement method"), LightProbesPlaceType);
        currentLightProbesGenerator = generatorListLightProbes[newLightProbesPlaceType];
        currentLightProbesGenerator.populateGUI_Initialization();

        if (LightProbesPlaceType != newLightProbesPlaceType) {
            LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
            LightProbesPlaceType = newLightProbesPlaceType;
        }
    }

    public void populateGUI_LightProbesSimplified() {
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.populateGUI_Simplification();
    }

    public void populateGUI_EvaluationPoints() {
        sceneVolumeEP = EditorGUILayout.ObjectField("EP Volume:", sceneVolumeEP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The EP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof(string));
        PlacementType newEvaluationPositionsPlaceType = (PlacementType)EditorGUILayout.Popup((int)EvaluationPositionsPlaceType, options);
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

    public void populateGUI_EvaluationPointsSimplified() {
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.populateGUI_Simplification();
    }

    public bool populateGUI_LightProbesEvaluated() {
        return m_evaluator.populateGUI_LightProbesEvaluated();
    }
    public bool populateGUI_LightProbesDecimated() {
        return m_evaluator.populateGUI_LightProbesDecimated();
    }

    public void Destroy() {
        Debug.Log("Destroy entered");

        DestroyImmediate(EPMaterial);
        ResetLightProbes();
        ResetEvaluationPoints();
    }

    void Update() {
        if (transform.hasChanged) {
            print("The transform has changed!");
            transform.hasChanged = false;
        }
    }

    public void PlaceLightProbes() {
        if (!UpdateSceneVolume(ref this.sceneVolumeLP, ref this.sceneVolumeLPprev, ref this.sceneVolumeLPBounds)) {
            return;
        }
        GenerateLightProbes();
        m_evaluator.ResetLightProbeData(currentLightProbesGenerator.TotalNumProbes);
    }

    public void ResetLightProbes() {
        if (generatorListLightProbes == null) {
            return;
        }

        if (!UpdateSceneVolume(ref this.sceneVolumeLP, ref this.sceneVolumeLPprev, ref this.sceneVolumeLPBounds)) {
            return;
        }

        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.Reset();
        GenerateLightProbes();
        m_evaluator.ResetLightProbeData(currentLightProbesGenerator.TotalNumProbes);
    }
    public void PlaceEvaluationPoints() {
        if (!UpdateSceneVolume(ref this.sceneVolumeEP, ref this.sceneVolumeEPprev, ref this.sceneVolumeEPBounds)) {
            return;
        }
        GenerateEvaluationPoints();
        m_evaluator.ResetEvaluationData();
    }

    public void ResetEvaluationPoints() {
        if (generatorListEvaluationPoints == null) {
            return;
        }

        if (!UpdateSceneVolume(ref this.sceneVolumeEP, ref this.sceneVolumeEPprev, ref this.sceneVolumeEPBounds)) {
            return;
        }

        // Clear Positions
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.Reset();
        GenerateEvaluationPoints();
        m_evaluator.ResetEvaluationData();
    }

    public void MapEvaluationPointsToLightProbes() {
        int mapped = m_evaluator.MapEvaluationPointsToLightProbes(currentLightProbesGenerator.Positions.ToArray(), currentEvaluationPointsGenerator.Positions.ToArray());
        Debug.Log("Mapped " + (mapped / (float)(currentEvaluationPointsGenerator.Positions.Count)).ToString("0.00%") + "of EPs: " + 
            mapped.ToString() + " out of " + currentEvaluationPointsGenerator.Positions.Count + " (" + (currentEvaluationPointsGenerator.Positions.Count - mapped).ToString() + " unmapped)");
    }

    public void Bake() {
        Lightmapping.Bake();
        LightProbesBakedProbes = new List<SphericalHarmonicsL2>(LightmapSettings.lightProbes.bakedProbes);
    }

    public void RemoveUnlitLightProbes() {

        // Remove Dark Probes to discard invisible or ones that are hidden inside geometry
        bool[] unlitPoints;
        currentLightProbesGenerator.TotalNumProbes = currentLightProbesGenerator.Positions.Count;
        m_evaluator.EvaluateBakedLightProbes(LightProbesBakedProbes.ToArray(), out unlitPoints);
        
        int count = 0;
        for (int i = unlitPoints.Length -1; i >= 0; --i) {
            if (unlitPoints[i]) {
                ++count;
                currentLightProbesGenerator.Positions.RemoveAt(i);
                LightProbesBakedProbes.RemoveAt(i);
            }
        }
        Debug.Log("Removed " + count.ToString() + " light probes");

        currentLightProbesGenerator.TotalNumProbesSimplified = currentLightProbesGenerator.Positions.Count;

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();

        m_evaluator.ResetLightProbeData(currentLightProbesGenerator.TotalNumProbesSimplified);
    }

    public void RemoveUnlitEvaluationPoints() {
        currentEvaluationPointsGenerator.TotalNumProbes = currentEvaluationPointsGenerator.Positions.Count;
        GetLitEvaluationPoints(currentEvaluationPointsGenerator.Positions.ToArray(), currentEvaluationPointsGenerator.Positions);
        currentEvaluationPointsGenerator.TotalNumProbesSimplified = currentEvaluationPointsGenerator.Positions.Count;
    }
    public void EvaluateEvaluationPoints() {
        m_evaluator.EvaluateReferencePoints(LightProbesBakedProbes.ToArray(), currentEvaluationPointsGenerator.Positions.ToArray());
    }
    public void DecimateLightProbes() {
        
        currentLightProbesGenerator.TotalNumProbes           = currentLightProbesGenerator.Positions.Count;
        currentLightProbesGenerator.Positions                = m_evaluator.DecimateBakedLightProbes(currentEvaluationPointsGenerator.Positions.ToArray(), currentLightProbesGenerator.Positions, LightProbesBakedProbes);
        currentLightProbesGenerator.TotalNumProbesSimplified = currentLightProbesGenerator.Positions.Count;
        Debug.Log("Decimated " + (currentLightProbesGenerator.TotalNumProbes-currentLightProbesGenerator.TotalNumProbesSimplified).ToString() + " light probes");

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
    }
    #endregion

    #region Private Functions

    bool UpdateSceneVolume(ref GameObject current, ref GameObject prev, ref Bounds bounds) {
        bool success = true;
        //if (current != prev)
        {
            success = ComputeSceneVolume(ref current, ref bounds);
            prev = current;
        }
        return success;
    }

    bool ComputeSceneVolume(ref GameObject gameObject, ref Bounds bounds) {
        List<Renderer> renderers = new List<Renderer>();

        if (gameObject != null) {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null) {
                Debug.LogError("No renderer found for GameObject: " + gameObject.name);
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

    void GenerateLightProbes() {
        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];

        // Compute Positions
        currentLightProbesGenerator.GeneratePositions(sceneVolumeLPBounds);

        // Set Max termination value 
        //terminationMinLightProbes = currentLightProbesGenerator.TotalNumProbes;
        //terminationMaxLightProbes = currentLightProbesGenerator.TotalNumProbes;

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = currentLightProbesGenerator.Positions.ToArray();
    }

    void GenerateEvaluationPoints() {
        // Get Generator
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];

        // Compute Positions
        List<Vector3> evaluationPositions = currentEvaluationPointsGenerator.GeneratePositions(sceneVolumeEPBounds);

        // Destroy the Evaluation Points
        DestroyEvaluationPoints();

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
    void DestroyEvaluationPoints() {
        GameObject evaluationObjectParent = GameObject.Find("EvaluationGroup_" + currentEvaluationPointsGenerator.GeneratorName);
        if (evaluationObjectParent == null) {
            Debug.LogWarning("Could not find object: " + "EvaluationGroup_" + currentEvaluationPointsGenerator.GeneratorName);
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

    void SetEvaluationPointProperties(GameObject obj, Vector3 position, int index, GameObject parenttobject) {
        obj.transform.position = position;
        obj.transform.parent = parenttobject.transform;
        obj.SetActive(true);
        obj.name = "Evaluation Point " + index.ToString();
    }

    void GetLitEvaluationPoints(Vector3[] posIn, List<Vector3> posOut) {
        posOut.Clear();

        // Evaluation
        bool[] unlitPoints;
        m_evaluator.EvaluateVisibilityPoints(posIn, out unlitPoints);

        GameObject evaluationObjectParent = GameObject.Find("EvaluationGroup_" + currentEvaluationPointsGenerator.GeneratorName);

        int count = 0;
        // Delete Nodes & Objects
        for (int i = 0; i < unlitPoints.Length; ++i) {
            if (unlitPoints[i]) {
                ++count;
                Transform epTransform = evaluationObjectParent.transform.Find("Evaluation Point " + i.ToString());
                DestroyImmediate(epTransform.gameObject);
                continue;
            }
            posOut.Add(posIn[i]);
        }
        Debug.Log("Removed " + count.ToString() + " evaluation points");
    }
    
    #endregion
}
