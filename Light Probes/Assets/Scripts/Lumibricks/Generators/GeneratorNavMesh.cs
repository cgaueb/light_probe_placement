﻿using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

public class GeneratorNavMesh : GeneratorInterface
{
    #region Private Variables
    private NavMeshAgent navMeshAgent = null;
    #endregion

    #region Constructor Functions
    public GeneratorNavMesh(NavMeshAgent navMeshAgent) : base(LumibricksScript.PlacementType.NavMesh.ToString()) { this.navMeshAgent = navMeshAgent; Reset(); }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        this.navMeshAgent = EditorGUILayout.ObjectField("Navigation Mesh Agent:", navMeshAgent, typeof(UnityEngine.AI.NavMeshAgent), true) as NavMeshAgent;
        EditorGUILayout.LabelField(new GUIContent("Number:", "The total number of points"), new GUIContent(m_positions.Count.ToString()));
    }

    public override void Reset() {
        m_positions.Clear();
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>();

        NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();

        if (navMesh.vertices.Length == 0) {
            Debug.LogWarning("You have to declare a NavMesh!");
        }

        foreach (Vector3 pos in navMesh.vertices) {
            positions.Add(pos);
        }

        m_positions = positions;
        m_positions_before = m_positions.Count;
        m_positions_after = m_positions.Count;
        return positions;
    }
    #endregion

}
