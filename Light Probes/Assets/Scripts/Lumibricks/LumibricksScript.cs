using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class LumibricksScript : MonoBehaviour
{
    #region Public Enum Types
    public enum PlacementType
    {
        Grid,
        Random,
        Stratified,
        Poisson,
        NavMesh,
        NavMeshVolume
    }

    public enum LightProbesEvaluationType
    {
        FixedLow,
        FixedMedium,
        FixedHigh,
        Random
    }
    #endregion

    #region Private Variables
    GameObject sceneVolumeLPprev; 
    GameObject sceneVolumeLP;
    Bounds sceneVolumeLPBounds;
    GameObject sceneVolumeEPprev;
    GameObject sceneVolumeEP;
    Bounds sceneVolumeEPBounds;
    Material EPMaterial = null;
    List<Vector3> probePositions = new List<Vector3>();

    int[]         tetrahedralizeIndices;
    Vector3[]     tetrahedralizePositions;

    Vector3    [] evaluationResults;
    int        [] evaluationTetrahedron;
    List<Vector3> evaluationRandomDirections = new List<Vector3>();
    readonly List<Vector3> evaluationFixedDirections = new List<Vector3>
    {
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
        new Vector3(-1.0f,-1.0f, 0.0f),
    };
    
    List<Vector3> evaluationPositions = new List<Vector3>();

    GameObject evaluationObjectParent = null;
    List<GameObject> evaluationObjects = new List<GameObject>();

    float       evaluationTotal = 0.0f;
    float       evaluationTotalDecimated = 0.0f;

    GeneratorInterface currentLightProbesGenerator = null;
    GeneratorInterface currentEvaluationPointsGenerator = null;
    Dictionary<PlacementType, GeneratorInterface> generatorListLightProbes;
    Dictionary<PlacementType, GeneratorInterface> generatorListEvaluationPoints;
    #endregion

    #region Public Variables
    public readonly int[] evaluationFixedCount = new int[] { 6, 14, 26 };
    public int evaluationRandomSamplingCount = 50;

    public int terminationMinLightProbes;
    public int terminationMaxLightProbes;

    public float terminationEvaluationError = 0.0f;

    public LightProbeGroup LightProbeGroup { get; set; } = null;
    public PlacementType LightProbesPlaceType { get; set; } = PlacementType.Poisson;
    public PlacementType  EvaluationPositionsPlaceType { get; set; } = PlacementType.Random;
    public LightProbesEvaluationType EvaluationType { get; set; } = LightProbesEvaluationType.FixedHigh;
    #endregion

    #region Constructor Functions
    public LumibricksScript()
    {

    }
    #endregion

    #region Public Functions
    public bool Init()
    {
        var component = GetComponent<LightProbeGroup>();
        var nv = component.GetComponentInChildren<NavMeshAgent>();

        if (generatorListLightProbes != null || generatorListEvaluationPoints != null)
        {
            if (nv != null && !generatorListLightProbes.ContainsKey(PlacementType.NavMesh))
            {
                // dynamically load NavMesh components
                generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

                generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
                generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);
                UnityEngine.Debug.Log("Nav mesh volume detected. Loading NavMesh elements");
            } else if (nv == null && generatorListLightProbes.ContainsKey(PlacementType.NavMesh))
            {
                generatorListLightProbes.Remove(PlacementType.NavMesh);
                generatorListLightProbes.Remove(PlacementType.NavMeshVolume);
                generatorListEvaluationPoints.Remove(PlacementType.NavMesh);
                generatorListEvaluationPoints.Remove(PlacementType.NavMeshVolume);
                UnityEngine.Debug.LogWarning("No nav mesh volume defined. NavMesh elements will not be loaded");
            }
            return true;
        }
        EPMaterial = new Material(Shader.Find("Unlit/Color"));
        EPMaterial.color = new Color(0.87f, 0.15f, 0.15f);


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
       
        if (nv == null)
        {
            UnityEngine.Debug.LogWarning("No nav mesh volume defined. NavMesh elements will not be loaded");
            return false;
        }

        generatorListLightProbes[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListLightProbes[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        generatorListEvaluationPoints[PlacementType.NavMesh] = new GeneratorNavMesh(nv);
        generatorListEvaluationPoints[PlacementType.NavMeshVolume] = new GeneratorNavMeshVolume(this, nv);

        return true;
    }

    public void Reset()
    {
        Debug.Log("Reset entered");

        evaluationTotal = 0.0f;
        evaluationTotalDecimated = 0.0f;

        evaluationRandomSamplingCount = 50;
        terminationEvaluationError = 0.0f;

        LightProbesPlaceType = PlacementType.Poisson;
        EvaluationPositionsPlaceType = PlacementType.Random;
        EvaluationType = LightProbesEvaluationType.FixedHigh;

        if (evaluationResults != null)
        {
            Array.Clear(evaluationResults, 0, evaluationResults.Length);
        }
        if (evaluationTetrahedron != null)
        {
            Array.Clear(evaluationTetrahedron, 0, evaluationTetrahedron.Length);
        }

        Destroy();
    }
    private ArrayList populatePlacementPopup()
    {
        ArrayList options = new ArrayList();
        options.AddRange(Enum.GetNames(typeof(PlacementType)));
        if (!generatorListLightProbes.ContainsKey(PlacementType.NavMesh))
        {
            options.Remove(PlacementType.NavMesh.ToString());
            options.Remove(PlacementType.NavMeshVolume.ToString());
            if (LightProbesPlaceType == PlacementType.NavMesh || LightProbesPlaceType == PlacementType.NavMeshVolume)
            {
                LightProbesPlaceType = PlacementType.Poisson;
            }
            if (EvaluationPositionsPlaceType == PlacementType.NavMesh || EvaluationPositionsPlaceType == PlacementType.NavMeshVolume)
            {
                EvaluationPositionsPlaceType = PlacementType.Random;
            }
        }
        return options;
    }

    public void populateGUI_LightProbes()
    {
        sceneVolumeLP = EditorGUILayout.ObjectField("LP Volume:", sceneVolumeLP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The LP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof(string));
        LightProbesPlaceType = (PlacementType)EditorGUILayout.Popup((int)LightProbesPlaceType, options);
        EditorGUILayout.EndHorizontal();

        //LightProbesPlaceType = (LumibricksScript.PlacementType)EditorGUILayout.EnumPopup(new GUIContent("Placement Type:", "The LP placement method"), LightProbesPlaceType);
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.populateGUI_Initialization();
    }

    public void populateGUI_LightProbesSimplified()
    {
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.probeCountSimplified = probePositions.Count;
        currentLightProbesGenerator.populateGUI_Simplification();
    }

    public void populateGUI_EvaluationPoints()
    {
        sceneVolumeEP = EditorGUILayout.ObjectField("EP Volume:", sceneVolumeEP, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Placement Type:", "The EP placement method"));
        string[] options = (string[])populatePlacementPopup().ToArray(typeof( string ));
        EvaluationPositionsPlaceType = (PlacementType)EditorGUILayout.Popup((int)EvaluationPositionsPlaceType, options);
        EditorGUILayout.EndHorizontal();

        //EvaluationPositionsPlaceType = (LumibricksScript.PlacementType)EditorGUILayout.EnumPopup(new GUIContent("Placement Type:", "The EP placement method"), EvaluationPositionsPlaceType);
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.populateGUI_Initialization();
    }

    public void populateGUI_EvaluationPointsSimplified()
    {
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.probeCountSimplified = evaluationPositions.Count;
        currentEvaluationPointsGenerator.populateGUI_Simplification();
    }

    public void populateGUI_LightProbesEvaluated()
    {
        if (evaluationResults == null)
            return;

        Vector3 evaluationRGB = new Vector3();
        for (int i = 0; i < evaluationResults.Length; i++)
            evaluationRGB += evaluationResults[i];
        evaluationRGB /= evaluationResults.Length;

        evaluationTotal = evaluationRGB.magnitude;

        EditorGUILayout.LabelField(new GUIContent("Average Irradiance (Before):", "The evaluation average irradiance target"), new GUIContent(evaluationTotal.ToString()));
    }
    public void populateGUI_LightProbesDecimatedEvaluated()
    {
        EditorGUILayout.LabelField(new GUIContent("Average Irradiance (After):", "The evaluation average irradiance after decimation"), new GUIContent(evaluationTotalDecimated.ToString()));
    }

    public void Destroy()
    {
        Debug.Log("Destroy entered");

        DestroyImmediate(EPMaterial);
        ResetLightProbes();
        ResetEvaluationPoints();
    }

    public void GenerateUniformSphereSampling()
    {
        evaluationRandomDirections.Clear();
        for (int i = 0; i < evaluationRandomSamplingCount; i++)
        {
            float z = 2.0f * UnityEngine.Random.Range(0.0f, 1.0f) - 1.0f;
            float phi = 2.0f * Mathf.PI * UnityEngine.Random.Range(0.0f, 1.0f);
            float r = Mathf.Sqrt(1.0f - z * z);
            float x = r * Mathf.Cos(phi);
            float y = r * Mathf.Sin(phi);

            evaluationRandomDirections.Add(new Vector3(x, y, z));
        }
    }

    public void PlaceLightProbes()
    {
        if (!UpdateSceneVolume(ref this.sceneVolumeLP, ref this.sceneVolumeLPprev, ref this.sceneVolumeLPBounds))
        {
            return;
        }
        GenerateLightProbes();
    }

    public void ResetLightProbes()
    {
        if (generatorListLightProbes == null)
        {
            return;
        }

        if (!UpdateSceneVolume(ref this.sceneVolumeLP, ref this.sceneVolumeLPprev, ref this.sceneVolumeLPBounds))
        {
            return;
        }

        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];
        currentLightProbesGenerator.Reset();

        if (currentLightProbesGenerator.probeCount == 0)
        {
            // Clear Positions
            probePositions.Clear();
            // Set Positions to LightProbeGroup
            LightProbeGroup.probePositions = probePositions.ToArray();
            // Just update unity
            LightProbes.Tetrahedralize();

            return;
        }
        GenerateLightProbes();
    }

    public void PlaceEvaluationPoints()
    {
        if (!UpdateSceneVolume(ref this.sceneVolumeEP, ref this.sceneVolumeEPprev, ref this.sceneVolumeEPBounds))
        {
            return;
        }
        GenerateEvaluationPoints();
    }

    public void ResetEvaluationPoints()
    {
        if (generatorListEvaluationPoints == null)
        {
            return;
        }

        if (!UpdateSceneVolume(ref this.sceneVolumeEP, ref this.sceneVolumeEPprev, ref this.sceneVolumeEPBounds))
        {
            return;
        }

        // Clear Positions
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];
        currentEvaluationPointsGenerator.Reset();
        DestroyEvaluationPoints();
        evaluationPositions.Clear();
        if (currentEvaluationPointsGenerator.probeCount == 0)
        {
            return;
        }
        GenerateEvaluationPoints();
    }

    public void MapEvaluationPointsToLightProbes()
    {
        MappingPointsTetrahedrize(LightmapSettings.lightProbes.positions, ref evaluationPositions);
    }

    public void RemoveUnlitLightProbes()
    {
        // STEP 1
        // Remove Dark Probes to discard invisible or ones that are hidden inside geometry
        GetLitLightProbes(LightmapSettings.lightProbes, ref probePositions);

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = probePositions.ToArray();
        
        // ReTetrahedralize
        LightProbes.Tetrahedralize();
    }

    public void DecimateLightProbes()
    {
        // Perform Decimation
        GetDecimatedLightProbes(LightmapSettings.lightProbes, ref probePositions);

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = probePositions.ToArray();
        
        // ReTetrahedralize
        LightProbes.Tetrahedralize();
    }

    public void RemoveUnlitEvaluationPoints()
    {
        GetLitEvaluationPoints(evaluationPositions.ToArray(), ref evaluationPositions);
    }

    public void EvaluateEvaluationPoints()
    {
        EvaluatePoints(LightmapSettings.lightProbes.bakedProbes, evaluationPositions.ToArray());
    }

    public void EvaluateEvaluationPoints(SphericalHarmonicsL2[] bakedProbes)
    {
        EvaluatePoints(bakedProbes, evaluationPositions.ToArray());
    }
    #endregion

    #region Private Functions
    
    bool UpdateSceneVolume(ref GameObject current, ref GameObject prev, ref Bounds bounds)
    {
        bool success = true;
        //if (current != prev)
        {
            success = ComputeSceneVolume(ref current, ref bounds);
            prev = current;
        }
        return success;
    }

    bool ComputeSceneVolume(ref GameObject gameObject, ref Bounds bounds)
    {
        List<Renderer> renderers = new List<Renderer>();

        if (gameObject != null)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError("No renderer found for GameObject: " + gameObject.name);
                return false;
            }
            renderers.Add(renderer);
        } 
        else
        {
            // Compute Scene's Bounding Box
            Renderer[] rnds = FindObjectsOfType<Renderer>();
            if (rnds.Length == 0)
            {
                return false;
            }
            renderers.AddRange(rnds);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        bounds = b;
        return true;
    }

    void GenerateLightProbes()
    {
        // Get Generator
        currentLightProbesGenerator = generatorListLightProbes[LightProbesPlaceType];

        // Compute Positions
        probePositions = currentLightProbesGenerator.GeneratePositions(sceneVolumeLPBounds);

        // Set Max termination value 
        terminationMinLightProbes = probePositions.Count;
        terminationMaxLightProbes = probePositions.Count;

        // Set Positions to LightProbeGroup
        LightProbeGroup.probePositions = probePositions.ToArray();

        // Tetrahedralize
        LightProbes.Tetrahedralize();
    }

    void GenerateEvaluationPoints()
    {
        // Get Generator
        currentEvaluationPointsGenerator = generatorListEvaluationPoints[EvaluationPositionsPlaceType];

        // Compute Positions
        evaluationPositions.Clear();
        evaluationPositions = currentEvaluationPointsGenerator.GeneratePositions(sceneVolumeEPBounds);

        // Destroy the Evaluation Points
        DestroyEvaluationPoints();

        // Generate new ones
        evaluationObjectParent = new GameObject("Evaluation Group");
        //evaluationObjectParent.transform.parent = this.transform;
        GameObject defaultobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        defaultobj.transform.localScale = new Vector3(0.0625f, 0.0625f, 0.0625f);
        MeshRenderer renderer = defaultobj.GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.receiveGI = ReceiveGI.LightProbes;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.sharedMaterial = EPMaterial;
        }
        MeshCollider collider = defaultobj.GetComponent<MeshCollider>();
        if (collider)
        {
            collider.enabled = false;
        }
        SetEvaluationPointProperties(defaultobj, 0, evaluationObjectParent);
        for (int i = 1; i < evaluationPositions.Count; i++)
        {
            GameObject obj = Instantiate(defaultobj, evaluationPositions[i], Quaternion.identity);
            SetEvaluationPointProperties(obj, i, evaluationObjectParent);
        }
    }
    void DestroyEvaluationPoints()
    {
        for (int i = evaluationObjects.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(evaluationObjects[i]);
        }
        evaluationObjects.Clear();
        DestroyImmediate(evaluationObjectParent);
    }

    void SetEvaluationPointProperties(GameObject obj, int index, GameObject parenttobject)
    {
        obj.transform.position = evaluationPositions[index];
        evaluationObjects.Add(obj);
        obj.transform.parent = evaluationObjectParent.transform;
        obj.SetActive(true);
        obj.name = "Evaluation Point " + index.ToString();
    }

    void GetLitLightProbes(LightProbes lpIn, ref List<Vector3> posOut)
    {
        if (lpIn.bakedProbes.Length != posOut.Count)
        {
            UnityEngine.Debug.Log("Mismatch between probe sizes. Skipping");
            return;
        }
        // Evaluation
        bool[] unlitPoints;
        EvaluateBakedLightProbes(lpIn.bakedProbes, out unlitPoints);

        // Delete Nodes
        for (int i = unlitPoints.Length - 1; i >= 0; i--)
        {
            if (unlitPoints[i])
            {
                posOut.RemoveAt(i);
            }
        }
    }

    void GetLitEvaluationPoints(Vector3[] posIn, ref List<Vector3> posOut)
    {
        if (posIn.Length != posOut.Count)
        {
            UnityEngine.Debug.Log("Mismatch between probe sizes. Skipping");
            return;
        }

        // Evaluation
        bool[] unlitPoints;
        EvaluateVisibilityPoints(posIn, out unlitPoints);

        // Delete Nodes & Objects
        for (int i = unlitPoints.Length - 1; i >= 0; i--)
        {
            if (unlitPoints[i])
            {
                posOut.RemoveAt(i);

                if (evaluationObjects.Count > 0)
                {
                    DestroyImmediate(evaluationObjects[i]);
                    evaluationObjects.RemoveAt(i);
                }
            }
        }
    }

    void GetDecimatedLightProbes(LightProbes lpIn, ref List<Vector3> posOut)
    {
        // Decimation
        bool[] decimatedPoints;
        DecimateBakedLightProbes(lpIn, out decimatedPoints);

        // Delete Nodes
        for (int i = decimatedPoints.Length - 1; i >= 0; i--)
        {
            if (decimatedPoints[i])
            {
                posOut.RemoveAt(i);
            }
        }
    }

    void EvaluateBakedLightProbes(SphericalHarmonicsL2[] bakedProbes, out bool[] unlitPoints)
    {
        SphericalHarmonicsL2 shZero = new SphericalHarmonicsL2();
        shZero.Clear();

        int j = 0;
        unlitPoints = new bool[bakedProbes.Length];
        foreach(SphericalHarmonicsL2 sh2 in bakedProbes)
        {
            unlitPoints[j++] = sh2.Equals(shZero);
        }
    }

    void DecimateBakedLightProbes(LightProbes lightProbes, out bool[] decimatedPoints)
    {
        int      decimatedIndex   = 0;
        float    decimatedCostMin = float.MaxValue;
        float [] decimatedCost    = new float[lightProbes.count];

        Vector3                    evaluationRGB;
        List<Vector3>              probePositionsDecimated;
        List<SphericalHarmonicsL2> bakedLightProbesDecimated;

        decimatedPoints = new bool[lightProbes.count];
        for (int i = 0; i < lightProbes.count; i++)
        {
            // 1. Remove Light Probe from Set
            {
                probePositionsDecimated = new List<Vector3>(lightProbes.positions);
                probePositionsDecimated.RemoveAt(i);

                bakedLightProbesDecimated = new List<SphericalHarmonicsL2>(LightmapSettings.lightProbes.bakedProbes);
                bakedLightProbesDecimated.RemoveAt(i);
            }

            //Debug.Log("DECIMATE - LP" + i + "-> Before " + lightProbes.count);
            //Debug.Log("DECIMATE - LP" + i + "-> After "  + probePositionsDecimated.Count);

            // 2. Tetrahedralize New Light Probe Set
            //{
                // Set Positions to LightProbeGroup
                //LightProbeGroup.probePositions = probePositionsDecimated.ToArray();
                // Tetrahedralize - NOT WORKING - REQUIRES BAKE PROCESS
                //LightProbes.Tetrahedralize();
            //}

            // 3. Map Evaluation Points to New Light Probe Set 
            MappingEvaluationPoints(probePositionsDecimated.ToArray());

            // 4. Evaluate
            EvaluateEvaluationPoints(bakedLightProbesDecimated.ToArray());

            // 5. Compute Cost
            {
                evaluationRGB = new Vector3(0,0,0);
                for (int j = 0; j < evaluationResults.Length; j++)
                    evaluationRGB += evaluationResults[j];
                evaluationRGB /= evaluationResults.Length;

                decimatedCost[i] = Mathf.Abs(evaluationRGB.magnitude-evaluationTotal);
            }

            // 6. Find light probe with the minimum error
            if(decimatedCost[i] < decimatedCostMin)
            {

                //Debug.Log("DECIMATE - LP" + i + "-> Eval " + evaluationRGB.magnitude);
                //Debug.Log("DECIMATE - LP" + i + "-> Cost " + decimatedCost[i]);

                decimatedIndex           = i;
                decimatedCostMin         = decimatedCost[i];
                evaluationTotalDecimated = evaluationRGB.magnitude;
            }

            decimatedPoints[i] = false;
        }

        // 7. Remove light probe with the minimum error
        {
            decimatedPoints[decimatedIndex] = true;
        }
    }
    void GetTetrahedronPositions(int j, out Vector3[] tetrahedronPositions) 
    {
        tetrahedronPositions    = new Vector3[4];
        tetrahedronPositions[0] = tetrahedralizePositions[tetrahedralizeIndices[j*4 + 0]];
        tetrahedronPositions[1] = tetrahedralizePositions[tetrahedralizeIndices[j*4 + 1]];
        tetrahedronPositions[2] = tetrahedralizePositions[tetrahedralizeIndices[j*4 + 2]];
        tetrahedronPositions[3] = tetrahedralizePositions[tetrahedralizeIndices[j*4 + 3]];
    }

    bool PointPlaneSameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p)
    {
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);
        float   dotV4  = Vector3.Dot(normal, v4 - v1);
        float   dotP   = Vector3.Dot(normal, p  - v1);
        return Mathf.Sign(dotV4) == Mathf.Sign(dotP);
    }

    bool IsInsideTetrahedron(Vector3[] v, Vector3 p) 
    {
        return  PointPlaneSameSide(v[0], v[1], v[2], v[3], p) &&
                PointPlaneSameSide(v[1], v[2], v[3], v[0], p) &&
                PointPlaneSameSide(v[2], v[3], v[0], v[1], p) &&
                PointPlaneSameSide(v[3], v[0], v[1], v[2], p);
    }

    Vector4 GetTetrahedronWeights(Vector3[] v, Vector3 p)
    {
        Matrix4x4 mat = Matrix4x4.identity;
        mat.SetColumn(0, v[0] - v[3]);
        mat.SetColumn(1, v[1] - v[3]);
        mat.SetColumn(2, v[2] - v[3]);
    
        Vector4 v_new = p - v[3];
        Vector4 weights = mat.inverse * v_new;
        weights.w = 1 - weights.x - weights.y - weights.z;
    
        return weights;
    }

     bool IsInsideTetrahedronWeights(Vector3[] v, Vector3 p)
    {
        Vector4 weights = GetTetrahedronWeights(v, p);
        return weights.x > 0 && weights.y > 0 && weights.z > 0 && weights.w > 0;
    }

    void EvaluateVisibilityPoints(Vector3[] posIn, out bool[] unlitPoints)
    {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8; // Corresponds to "Environment"

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        //layerMask = ~layerMask;

        unlitPoints = new bool[posIn.Length];
        for (int i = 0; i < posIn.Length; i++)
        {
            unlitPoints[i] = false;
            
            // 1. Remove Outsize of Tetrahedralize
            if(evaluationTetrahedron[i] == -1)
            {
                unlitPoints[i] = true;
                continue;
            }

            // 2. Remove Occluded  (if cannot be seen by just one LP)
            Vector3[] tetrahedronPositions;
            GetTetrahedronPositions(evaluationTetrahedron[i], out tetrahedronPositions);

            foreach(Vector3 pos in tetrahedronPositions)
            {
                RaycastHit hit;
                Ray visRay = new Ray(posIn[i], pos - posIn[i]);
                if(Physics.Raycast(visRay, out hit, float.MaxValue, layerMask))
                {
                    // Collision Found
                    // Debug.Log("EP" + i + "-> Collision with " + hit.point);

                    unlitPoints[i] = true;
                    break;
                }
            }
        }
    }

    void GetInterpolatedLightProbe(Vector3 evalPosition, int evalTetrahedron, SphericalHarmonicsL2[] bakedprobes, ref SphericalHarmonicsL2 sh2)
    {
        // GetTetrahedronSHs
        SphericalHarmonicsL2[] tetrahedronSH2 = new SphericalHarmonicsL2[4];
        tetrahedronSH2[0] = bakedprobes[tetrahedralizeIndices[evalTetrahedron*4 + 0]];
        tetrahedronSH2[1] = bakedprobes[tetrahedralizeIndices[evalTetrahedron*4 + 1]];
        tetrahedronSH2[2] = bakedprobes[tetrahedralizeIndices[evalTetrahedron*4 + 2]];
        tetrahedronSH2[3] = bakedprobes[tetrahedralizeIndices[evalTetrahedron*4 + 3]];

        // Get Barycentric Weights
        Vector3[] tetrahedronPositions;
        GetTetrahedronPositions(evalTetrahedron, out tetrahedronPositions);
        Vector4 weights = GetTetrahedronWeights(tetrahedronPositions, evalPosition);

        // Interpolate
        sh2 = weights.x*tetrahedronSH2[0] + weights.y*tetrahedronSH2[1] + weights.z*tetrahedronSH2[2] + weights.w*tetrahedronSH2[3];
    }

    void EvaluatePoints(SphericalHarmonicsL2[] bakedprobes, Vector3[] evalPositions)
    {
        int         directionsCount;
        Vector3[]   directions;

        if(EvaluationType == LightProbesEvaluationType.Random)
        {
            directionsCount = evaluationRandomSamplingCount;
            directions      = evaluationRandomDirections.ToArray();
        }
        else
        {
            directionsCount = evaluationFixedCount[(int)EvaluationType];
            directions      = evaluationFixedDirections.GetRange(0, directionsCount).ToArray();

        }
        Color[]     evaluationResultsPerDir   = new Color[directionsCount];

        int j = 0;
        evaluationResults = new Vector3[evalPositions.Length];
        foreach(Vector3 pos in evalPositions)
        {
            SphericalHarmonicsL2 sh2 = new SphericalHarmonicsL2();
            if(evaluationTetrahedron[j] == -1)
                sh2.Clear();
            else
                GetInterpolatedLightProbe(pos, evaluationTetrahedron[j], bakedprobes, ref sh2);
            //LightProbes.GetInterpolatedProbe(pos, null, out sh2);

            sh2.Evaluate(directions, evaluationResultsPerDir);

            Vector3 uniformSampledEvaluation = new Vector3(0,0,0);
            for (int i = 0; i < directionsCount; i++)
            {
                uniformSampledEvaluation.x += evaluationResultsPerDir[i].r;
                uniformSampledEvaluation.y += evaluationResultsPerDir[i].g;
                uniformSampledEvaluation.z += evaluationResultsPerDir[i].b;
            }
            uniformSampledEvaluation /= directionsCount;
            
            evaluationResults[j++] = uniformSampledEvaluation;
        }
    }

    void MappingPointsTetrahedrize(Vector3[] probePositions, ref List<Vector3> evalPositions)
    {
        Lightmapping.Tetrahedralize(probePositions, out tetrahedralizeIndices, out tetrahedralizePositions);

        if(probePositions.Length != tetrahedralizePositions.Length)
            Debug.LogError("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");

        Vector3[] tetrahedronPositions;
        evaluationTetrahedron = new int[evalPositions.Count];
        for (int evaluationPositionIndex = 0; evaluationPositionIndex < evalPositions.Count; evaluationPositionIndex++)
        {
            Vector3 evaluationPosition = evalPositions[evaluationPositionIndex];
            // 1. Relate Evaluation Point with one Tetrahedron
            evaluationTetrahedron[evaluationPositionIndex] = -1;
            for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedralizeIndices.Length / 4; tetrahedronIndex++)
            {
                GetTetrahedronPositions(tetrahedronIndex, out tetrahedronPositions);
                if (IsInsideTetrahedron(tetrahedronPositions, evaluationPosition))
                {
                    evaluationTetrahedron[evaluationPositionIndex] = tetrahedronIndex;
                    break;
                }
            }
        }
    }
    void MappingEvaluationPoints(Vector3 [] probePositions)
    {
        MappingPointsTetrahedrize(probePositions, ref evaluationPositions);
    }

    #endregion
}
