using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorGrid : GeneratorInterface
{

#region Public Variables
    public Vector3Int probesDims;
#endregion

#region Constructor Functions
    public GeneratorGrid() : base("Grid") { Reset(); }
#endregion

#region Public Override Functions
    public override void populateGUI_Initialization()
    {
        probesDims = EditorGUILayout.Vector3IntField(new GUIContent("Grid Size", "The size of the 3D grid"), probesDims);
        probesDims = Vector3Int.Max(new Vector3Int(1, 1, 1), probesDims);
        probeCount = GetTotalNumProbes;
        EditorGUILayout.LabelField(new GUIContent("Number:", "The total number of dense points"), new GUIContent(probeCount.ToString()));
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
        probesDims = new Vector3Int(2, 2, 2);
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
        Vector3 step = new Vector3(bounds.size.x / (subdivisions.x - 1f), bounds.size.y / (subdivisions.y - 1f), bounds.size.z / (subdivisions.z - 1f));

        for (int i = 0; i < 3; i++)
        {
            if (subdivisions[i] <= 1)
            {
                step[i] = 0.0f;
            }
        }

        for (int x = 0; x < subdivisions.x; x++)
        {
            for (int y = 0; y < subdivisions.y; y++)
            {
                for (int z = 0; z < subdivisions.z; z++)
                {
                    Vector3 probePos = (bounds.center - bounds.extents) + new Vector3(step.x * x, step.y * y, step.z * z);
                    positions.Add(probePos);
                }
            }
        }

        return positions;
    }
#endregion
}
