using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorStratified : GeneratorInterface
{
#region Public Variables
    public Vector3Int probesDims;
#endregion

#region Constructor Functions
    public GeneratorStratified() : base("Stratified") { Reset(); }
#endregion

#region Public Override Functions
    public override void populateGUI_Initialization()
    {
        probesDims = EditorGUILayout.Vector3IntField(new GUIContent("Grid Size", "The size of the 3D grid"), probesDims);
        probesDims = Vector3Int.Max(new Vector3Int(1, 1, 1), probesDims);
        probeCount = GetTotalNumProbes;
        EditorGUILayout.LabelField(new GUIContent("Number:", "The total number of dense light probes"), new GUIContent(probeCount.ToString()));
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
        probesDims = new Vector3Int(4, 4, 4);
        probesDims = Vector3Int.Max(new Vector3Int(1, 1, 1), probesDims);
        probeCount = GetTotalNumProbes;
        probeCountSimplified = 0;
    }

    public override int GetTotalNumProbes
    {
        get { return probesDims.x * probesDims.y * probesDims.z; }
    }

    public override List<Vector3> GeneratePositions(Bounds bounds)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 subdivisions = probesDims;
        Vector3 step    = new Vector3(bounds.size.x / (subdivisions.x), bounds.size.y / (subdivisions.y), bounds.size.z / (subdivisions.z));
        Vector3 offset  = new Vector3(0.0f, 0.0f, 0.0f);

        for (int x = 0; x < subdivisions.x; x++)
        {
            for (int y = 0; y < subdivisions.y; y++)
            {
                for (int z = 0; z < subdivisions.z; z++)
                {
                    Vector3 rand = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
                    Vector3 probePos = bounds.min + new Vector3(
                        step.x * (x + offset.x) + rand.x * step.x,
                        step.y * (y + offset.y) + rand.y * step.y,
                        step.z * (z + offset.z) + rand.z * step.z);
                    positions.Add(probePos);
                }
            }
        }

        return positions;
    }
#endregion
}
