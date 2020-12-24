using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorRandom : GeneratorInterface
{
#region Constructor Functions
    public GeneratorRandom() : base("Random") { Reset(); }
#endregion
     
#region Public Override Functions
    public override void populateGUI_Initialization()
    {
        probeCount = EditorGUILayout.IntField(new GUIContent("Number:", "The total number of dense light probes"), probeCount);
        probeCount = Mathf.Max(1, probeCount);
    }

    public override void populateGUI_Simplification()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Before:", "The total number of light points before Simplification"), new GUIContent(probeCount.ToString()));
        EditorGUILayout.LabelField(new GUIContent("After :", "The total number of light probes after  Simplification"), new GUIContent(probeCountSimplified.ToString()));
        EditorGUILayout.EndHorizontal();
    }

    public override void Reset()
    {
        probeCount = 1024;
        probeCountSimplified = 0;
    }

    public override List<Vector3> GeneratePositions(Bounds bounds)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < probeCount; i++) {
            Vector3 probePos = bounds.center + new Vector3(Random.Range(-0.5f, 0.5f) * bounds.size.x, Random.Range(-0.5f, 0.5f) * bounds.size.y, Random.Range(-0.5f, 0.5f) * bounds.size.z);
            positions.Add(probePos);
        }

        return positions;
    }
#endregion

}
