using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorStratified : GeneratorInterface
{
    #region Public Variables
    private Vector3Int defaultProbesDims = new Vector3Int(4, 4, 4);
    private Vector3Int probesDims;
    #endregion

    #region Constructor Functions
    public GeneratorStratified() : base(LumiProbesScript.PlacementType.Stratified.ToString()) { Reset(); }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        probesDims = EditorGUILayout.Vector3IntField(new GUIContent("Grid Size:", "The size of the 3D grid"), probesDims, CustomStyles.defaultGUILayoutOption);
        probesDims = Vector3Int.Max(new Vector3Int(1, 1, 1), probesDims);
        EditorGUILayout.LabelField(new GUIContent("Placed:", "The total number of placed points"), new GUIContent(m_positions.Count.ToString()), CustomStyles.defaultGUILayoutOption);
    }

    public override void Reset() {
        probesDims = defaultProbesDims;
        m_positions.Clear();
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>();
        Vector3 subdivisions = probesDims;
        Vector3 step = new Vector3(bounds.size.x / (subdivisions.x), bounds.size.y / (subdivisions.y), bounds.size.z / (subdivisions.z));
        Vector3 offset = new Vector3(0.0f, 0.0f, 0.0f);

        for (int x = 0; x < subdivisions.x; x++) {
            for (int y = 0; y < subdivisions.y; y++) {
                for (int z = 0; z < subdivisions.z; z++) {
                    Vector3 rand = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                    Vector3 probePos = bounds.min + new Vector3(
                        step.x * (x + offset.x) + rand.x * step.x,
                        step.y * (y + offset.y) + rand.y * step.y,
                        step.z * (z + offset.z) + rand.z * step.z);
                    positions.Add(probePos);
                }
            }
        }

        m_positions = positions;
        m_placed_positions = m_positions.Count;
        return positions;
    }
    #endregion
}
