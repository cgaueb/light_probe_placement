using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class SolverManager
{
    public enum SolverType
    {
        L1Norm,
        L2Norm,
        L2NormSquared
    }


    public MetricsManager metricsManager = new MetricsManager();

    #region Solvers
    abstract class Solver
    {
        public MetricsManager metricsManager = null;

        public abstract double computeSampleLoss(Color value, Color reference);
        public double evaluateSample(Color value) {
            throw new System.NotImplementedException();
        }
    }

    class L1NormSolver : Solver
    {
        public override double computeSampleLoss(Color value, Color reference) {
            Vector3 res = metricsManager.computeSampleLoss(value, reference);
            return System.Math.Abs(res.x) + System.Math.Abs(res.y) + System.Math.Abs(res.z);
        }
    }
    class L2NormSolver : Solver
    {
        public override double computeSampleLoss(Color value, Color reference) {
            Vector3 res = metricsManager.computeSampleLoss(value, reference);
            return System.Math.Sqrt(res.x * res.x) + System.Math.Sqrt(res.y * res.y) + System.Math.Sqrt(res.z * res.z);
        }
    }
    class L2NormSquaredSolver : Solver
    {
        public override double computeSampleLoss(Color value, Color reference) {
            Vector3 res = metricsManager.computeSampleLoss(value, reference);
            return (res.x * res.x) + (res.y * res.y) + (res.z * res.z);
        }
    }
    #endregion

    private Solver currentSolver;
    private Dictionary<SolverType, Solver> SolverList = new Dictionary<SolverType, Solver> {
        { SolverType.L1Norm, new L1NormSolver() },
        { SolverType.L2Norm, new L2NormSolver() },
        { SolverType.L2NormSquared, new L2NormSquaredSolver() }
    };

    public SolverType CurrentSolverType { get; private set; } = SolverType.L1Norm;
    public MetricsManager.MetricType CurrentMetricType {
        get { return metricsManager.CurrentMetricType; }
    }
    public SolverManager() {
        // init solvers
        foreach (var key in SolverList.Keys) {
            SolverList[key].metricsManager = metricsManager;
        }
    }
    public void populateGUI() {
        CurrentSolverType = (SolverType)EditorGUILayout.EnumPopup(new GUIContent("Solver:", "The solver method"), CurrentSolverType);
        currentSolver = SolverList[CurrentSolverType];
        metricsManager.populateGUI();
    }

    public double computeSampleLoss(Color value, Color reference) {
        return currentSolver.computeSampleLoss(value, reference);
    }

    public double evaluateSample(Color value) {
        return currentSolver.evaluateSample(value);
    }
}
