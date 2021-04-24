using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public abstract class GeneratorInterface
{
    #region Private Variables
    private string generatorName;
    #endregion

    #region Protected Variables
    protected List<Vector3> m_positions = null;
    protected int m_placed_positions = 0;
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
    virtual public string GeneratorName {
        get { return generatorName; }
    }
    virtual public int TotalNumProbes {
        get { return m_placed_positions; }
        set { m_placed_positions = value; }
    }
    virtual public List<Vector3> Positions {
        get { return m_positions; }
        set { m_positions = value; }
    }
    #endregion
}
