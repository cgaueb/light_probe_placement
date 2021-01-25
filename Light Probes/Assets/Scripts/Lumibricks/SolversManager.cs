using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class SolversManager
{
    public enum SolverType
    {
        L1Norm,
        L2Norm,
        L2NormSquared
    }

    private MetricsManager metricsManager = null;

    #region Solvers
    abstract class Solver
    {
        public MetricsManager metricsManager = null;
        public virtual double computeLoss(List<Color> estimates, List<Color> reference) {
            double cost = 0.0;
            for (int j = 0; j < estimates.Count; j++) {
                cost += computeSampleLoss(estimates[j], reference[j]);
            }
            //cost /= (estimates.Count);
            return cost;
        }
        public virtual double evaluate(List<Color> reference) {
            double value = 0.0;
            for (int j = 0; j < reference.Count; j++) {
                value += evaluateSample(reference[j]);
            }
            return value;
        }
        public double evaluateSample(Color value) {
            throw new System.NotImplementedException();
        }
        public abstract double computeSampleLoss(Color estimate, Color reference);
    }

    class L1NormSolver : Solver
    {
        public override double computeSampleLoss(Color estimate, Color reference) {
            Vector3 sample_cost = metricsManager.computeSampleLoss(estimate, reference);
            return System.Math.Abs(sample_cost.x) + System.Math.Abs(sample_cost.y) + System.Math.Abs(sample_cost.z);
        }
    }
    class L2NormSolver : Solver
    {
        public override double computeSampleLoss(Color estimate, Color reference) {
            Vector3 sample_cost = metricsManager.computeSampleLoss(estimate, reference);
            return System.Math.Sqrt(sample_cost.x * sample_cost.x) + System.Math.Sqrt(sample_cost.y * sample_cost.y) + System.Math.Sqrt(sample_cost.z * sample_cost.z);
        }
    }
    class L2NormSquaredSolver : Solver
    {
        public override double computeSampleLoss(Color estimate, Color reference) {
            Vector3 sample_cost = metricsManager.computeSampleLoss(estimate, reference);
            return (sample_cost.x * sample_cost.x) + (sample_cost.y * sample_cost.y) + (sample_cost.z * sample_cost.z);
        }
    }
    #endregion

    private Solver currentSolver;
    private Dictionary<SolverType, Solver> SolverList = new Dictionary<SolverType, Solver> {
        { SolverType.L1Norm, new L1NormSolver() },
        { SolverType.L2Norm, new L2NormSolver() },
        { SolverType.L2NormSquared, new L2NormSquaredSolver() }
    };

    public SolverType CurrentSolverType { get; private set; }
    public MetricsManager.MetricType CurrentMetricType {
        get { return metricsManager.CurrentMetricType; }
    }
    public SolversManager() {
        Reset();
    }
    public MetricsManager MetricsManager {
        get { return metricsManager; }
        set {
            metricsManager = value;
            foreach (var key in SolverList.Keys) {
                SolverList[key].metricsManager = metricsManager;

            }
        }
    }
    public void Reset() {
        CurrentSolverType = SolverType.L1Norm;
    }
    public void populateGUI() {
        CurrentSolverType = (SolverType)EditorGUILayout.EnumPopup(new GUIContent("Solver:", "The solver method"), CurrentSolverType, LumibricksScript.defaultOption);
    }
    public void SetCurrentSolver() {
        currentSolver = SolverList[CurrentSolverType];
    }
    public void SetCurrentMetric() {
        metricsManager.SetCurrentMetric();
    }
        public double computeLoss(List<Color> estimates, List<Color> reference) {
        return currentSolver.computeLoss(estimates, reference);
    }
    public double evaluate(List<Color> reference) {
        return currentSolver.evaluate(reference);
    }
}
