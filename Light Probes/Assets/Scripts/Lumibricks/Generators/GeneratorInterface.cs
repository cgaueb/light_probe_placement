using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public abstract class GeneratorInterface
{
    #region Private Variables
    string generatorName;
    #endregion

    #region Protected Variables
    protected List<Vector3> m_positions = null;
    protected int m_positions_before = 0;
    protected int m_positions_after = 0;
    #endregion

    #region Constructor Functions
    public GeneratorInterface(string name) {
        m_positions = new List<Vector3>();
        generatorName = name;
    }
    #endregion

    #region Abstract Functions
    abstract public void populateGUI_Initialization();
    abstract public List<Vector3> GeneratePositions(Bounds bounds);
    abstract public void Reset();
    #endregion

    #region Virtual Functions
    public virtual void populateGUI_Simplification() {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Before:", "The total number of light points before Simplification"), new GUIContent(m_positions_before.ToString()));
        EditorGUILayout.LabelField(new GUIContent("After :", "The total number of light probes after  Simplification"), new GUIContent(m_positions_after.ToString()));
        EditorGUILayout.EndHorizontal();
    }
    virtual public string GeneratorName {
        get { return generatorName; }
    }
    virtual public int TotalNumProbes {
        get { return m_positions_before; }
        set { m_positions_before = value; }
    }
    virtual public int TotalNumProbesSimplified {
        get { return m_positions_after; }
        set { m_positions_after = value; }
    }
    virtual public List<Vector3> Positions {
        get { return m_positions; }
        set { m_positions = value; }
    }
    #endregion
}
