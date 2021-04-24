using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorGrid : GeneratorInterface
{
    #region Private Variables
    private Vector3Int defaultProbesDims = new Vector3Int(4,4,4);
    private Vector3Int probesDims;
    #endregion

    #region Constructor Functions
    public GeneratorGrid() : base(LumiProbesScript.PlacementType.Grid.ToString()) { Reset(); }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        EditorGUILayout.BeginHorizontal();
        probesDims = EditorGUILayout.Vector3IntField(new GUIContent("Grid Size:", "The size of the 3D grid"), probesDims, CustomStyles.defaultGUILayoutOption);
        probesDims = Vector3Int.Max(new Vector3Int(1, 1, 1), probesDims);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Placed:", "The total number of placed points"), new GUIContent(m_positions.Count.ToString()), CustomStyles.defaultGUILayoutOption);
    }

    public override void Reset() {
        probesDims = defaultProbesDims;
        m_positions.Clear();
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>();
        Vector3 subdivisions = probesDims;
        Vector3 step = new Vector3(bounds.size.x / (subdivisions.x - 1f), bounds.size.y / (subdivisions.y - 1f), bounds.size.z / (subdivisions.z - 1f));

        for (int i = 0; i < 3; i++) {
            if (subdivisions[i] <= 1) {
                step[i] = 0.0f;
            }
        }

        for (int x = 0; x < subdivisions.x; x++) {
            for (int y = 0; y < subdivisions.y; y++) {
                for (int z = 0; z < subdivisions.z; z++) {
                    Vector3 probePos = (bounds.center - bounds.extents) + new Vector3(step.x * x, step.y * y, step.z * z);
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
