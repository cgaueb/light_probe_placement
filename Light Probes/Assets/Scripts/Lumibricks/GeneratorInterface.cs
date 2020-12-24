using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GeneratorInterface
{
#region Private Variables
    string generatorName;
#endregion

#region Public Variables
    public int probeCount = 0;
    public int probeCountSimplified = 0;
#endregion
    
#region Constructor Functions
    public GeneratorInterface(string name)
    {
        generatorName = name;
    }
#endregion

#region Abstract Functions
    abstract public void populateGUI_Initialization();
    abstract public void populateGUI_Simplification();
    abstract public List<Vector3> GeneratePositions(Bounds bounds);
    #endregion

    #region Core Functions
    virtual public void Reset()
    {
        probeCount = 0;
        probeCountSimplified = 0;
    }
#endregion

#region Virtual Functions
    virtual public int GetTotalNumProbes
    {
        get { return probeCount; }
    }
#endregion
}
