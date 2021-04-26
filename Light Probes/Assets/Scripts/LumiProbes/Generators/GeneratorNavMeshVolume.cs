using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

public class GeneratorNavMeshVolume : GeneratorInterface
{
    #region Private Variables
    private NavMeshAgent navMeshAgent = null;
    private LumiProbesScript script = null;
    #endregion

    #region Constructor Functions
    public GeneratorNavMeshVolume(LumiProbesScript script, NavMeshAgent navMeshAgent) : base(LumiProbesScript.PlacementType.NavMeshVolume.ToString()) {
        this.script = script;
        this.navMeshAgent = navMeshAgent;
        Reset();
    }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        this.navMeshAgent = EditorGUILayout.ObjectField("Navigation Mesh Agent:", navMeshAgent, typeof(UnityEngine.AI.NavMeshAgent), true) as NavMeshAgent;
        EditorGUILayout.LabelField(new GUIContent("Placed:", "The total number of placed points"), new GUIContent(m_positions.Count.ToString()), CustomStyles.defaultGUILayoutOption);
    }

    public override void Reset() {
        m_positions.Clear();
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>();

        float height = navMeshAgent.height;
        NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();
        if (navMesh.vertices.Length == 0) {
            LumiLogger.Logger.LogWarning("You have to declare a NavMesh!");
        }

        foreach (Vector3 pos in navMesh.vertices) {
            positions.Add(pos);
            positions.Add(new Vector3(pos.x, pos.y + height, pos.z));
        }

        m_positions = positions;
        m_placed_positions = m_positions.Count;
        return positions;
    }
    #endregion

}
