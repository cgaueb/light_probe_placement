using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorRandom : GeneratorInterface
{
    #region Private Variables
    private int defaultProbeCount = 32;
    private int probeCount = 32;
    #endregion

    #region Constructor Functions
    public GeneratorRandom() : base(LumiProbesScript.PlacementType.Random.ToString()) { Reset(); }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        probeCount = EditorGUILayout.IntField(new GUIContent("Number:", "The total number of points"), probeCount, CustomStyles.defaultGUILayoutOption);
        probeCount = Mathf.Max(1, probeCount);
        EditorGUILayout.LabelField(new GUIContent("Placed:", "The total number of placed points"), new GUIContent(m_positions.Count.ToString()), CustomStyles.defaultGUILayoutOption);
    }

    public override void Reset() {
        m_positions.Clear();
        probeCount = defaultProbeCount;
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>(probeCount);
        for (int i = 0; i < probeCount; i++) {
            Vector3 probePos = bounds.center + new Vector3(Random.Range(-0.5f, 0.5f) * bounds.size.x, Random.Range(-0.5f, 0.5f) * bounds.size.y, Random.Range(-0.5f, 0.5f) * bounds.size.z);
            positions.Add(probePos);
        }

        m_positions = positions;
        m_placed_positions = m_positions.Count;
        return positions;
    }
    #endregion

}
