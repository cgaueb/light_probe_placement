using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class SolversManager
{
    public enum SolverType
    {
        MaxPercentileError,
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
        public abstract double computeSampleLoss(Color estimate, Color reference);
    }
    

    class MaxPercentileErrorSolver : Solver
    {
        public override double computeLoss(List<Color> estimates, List<Color> reference) {
            double cost = 0.0;
            //List<Color> diffs = new List<Color>(estimates.Count);
            for (int j = 0; j < estimates.Count; j++) {
               // diffs.Add(estimates[j] - reference[j]);
                double sample_cost = computeSampleLoss(estimates[j], reference[j]);
                cost = System.Math.Max(cost, sample_cost);
            }
            cost *= 2 * 100;
            cost /= estimates.Count;
            return cost;
        }

        public override double computeSampleLoss(Color estimate, Color reference) {
            Vector3 sample_cost = metricsManager.computeSampleLoss(estimate, reference);
            Vector3[] sample_evaluation = metricsManager.evaluateSample(estimate, reference);
            // return the max absolute relative difference, but also account for zero values
            // abs(x-y) / 0.5 (x+y)
            // could also use sqrt here to give higher feedback to larger differences
            Vector3 sample_cost_triplet = new Vector3(
                System.Math.Abs(sample_cost.x) / System.Math.Abs(sample_evaluation[0].x + sample_evaluation[1].x),
                System.Math.Abs(sample_cost.y) / System.Math.Abs(sample_evaluation[0].y + sample_evaluation[1].y),
                System.Math.Abs(sample_cost.z) / System.Math.Abs(sample_evaluation[0].z + sample_evaluation[1].z));
            double cost = System.Math.Max(System.Math.Max(sample_cost_triplet.x, sample_cost_triplet.y), sample_cost_triplet.z);
            if (double.IsNaN(cost)) {
                cost = 0.0;
            }
            return cost;
        }
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
        { SolverType.MaxPercentileError, new MaxPercentileErrorSolver() },
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
        CurrentSolverType = SolverType.MaxPercentileError;
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
}
