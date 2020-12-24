using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

public class GeneratorNavMesh : GeneratorInterface
{
    #region Private Variables
    private NavMeshAgent navMeshAgent = null;
    #endregion

    #region Constructor Functions
    public GeneratorNavMesh(NavMeshAgent navMeshAgent) : base("NavMesh") { this.navMeshAgent = navMeshAgent; Reset(); }
#endregion

#region Public Override Functions
    public override void populateGUI_Initialization()
    {
        this.navMeshAgent = EditorGUILayout.ObjectField("Navigation Mesh Agent:", navMeshAgent, typeof(UnityEngine.AI.NavMeshAgent), true) as NavMeshAgent;
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
        probeCount = 0; 
        probeCountSimplified = 0;
    }

    public override List<Vector3> GeneratePositions(Bounds bounds)
    {
        List<Vector3> positions = new List<Vector3>();

		NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();

        if(navMesh.vertices.Length == 0){
            Debug.LogWarning("You have to declare a NavMesh!");
        }

        foreach(Vector3 pos in navMesh.vertices) {
            positions.Add(pos);
        }

        probeCount = positions.Count;

        return positions;
    }
#endregion

}
