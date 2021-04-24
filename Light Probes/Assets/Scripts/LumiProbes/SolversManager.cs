using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class SolversManager
{
    public enum SolverType
    {
        MaxPercentageError,
        AveragePercentageError
    }

    private MetricsManager metricsManager = null;

    #region Solvers
    abstract class Solver
    {
        public MetricsManager metricsManager = null;
        public virtual double computeLoss(List<Color> estimates, List<Color> reference) {
            double cost = 0.0;
            for (int j = 0; j < estimates.Count; j++) {
                double sample_cost = computeSampleLoss(estimates[j], reference[j]);
                cost += sample_cost;
            }
            cost /= estimates.Count;
            cost *= 100; 
            return cost;
        } 
        public abstract double computeSampleLoss(Color estimate, Color reference);
    }
    

    abstract class PercentageErrorSolver : Solver
    {
        public override double computeSampleLoss(Color estimate, Color reference) {
            Vector3[] sample_evaluation = metricsManager.evaluateSample(estimate, reference);
            // return the average absolute relative difference, but also account for zero values
            // abs(x-y) / (abs(x) + abs(y))
            // could also use sqrt here to give higher feedback to larger differences
            double cost = 0.0;
            double num_valid_weights = 0.0;
            Vector3 sample_cost_triplet = Vector3.zero;
            sample_cost_triplet.x = System.Math.Abs(sample_evaluation[0].x - sample_evaluation[1].x) / (System.Math.Abs(sample_evaluation[0].x) + System.Math.Abs(sample_evaluation[1].x));
            if (!double.IsNaN(sample_cost_triplet.x)) {
                cost = sample_cost_triplet.x;
                num_valid_weights++;
            }
            if (metricsManager.CurrentMetricType != MetricsManager.MetricType.Luminance) {
                sample_cost_triplet.y = System.Math.Abs(sample_evaluation[0].y - sample_evaluation[1].y) / (System.Math.Abs(sample_evaluation[0].y) + System.Math.Abs(sample_evaluation[1].y));
                if (!double.IsNaN(sample_cost_triplet.y)) {
                    cost += sample_cost_triplet.y;
                    num_valid_weights++;
                }
                sample_cost_triplet.z = System.Math.Abs(sample_evaluation[0].z - sample_evaluation[1].z) / (System.Math.Abs(sample_evaluation[0].z) + System.Math.Abs(sample_evaluation[1].z));
                if (!double.IsNaN(sample_cost_triplet.z)) {
                    cost += sample_cost_triplet.z;
                    num_valid_weights++;
                }
            }
            if (num_valid_weights > 0) {
                cost /= num_valid_weights;
            }
            return cost;
        }
    }

    class MaxPercentageErrorSolver : PercentageErrorSolver
    {
        public override double computeLoss(List<Color> estimates, List<Color> reference) {
            double cost = 0.0;
            for (int j = 0; j < estimates.Count; j++) {
                double sample_cost = computeSampleLoss(estimates[j], reference[j]);
                cost = System.Math.Max(cost, sample_cost);
            }
            cost *= 100;
            return cost;
        }
    }

    class AveragePercentageErrorSolver : PercentageErrorSolver
    {
        public override double computeLoss(List<Color> estimates, List<Color> reference) {
            double cost = 0.0;
            for (int j = 0; j < estimates.Count; j++) {
                double sample_cost = computeSampleLoss(estimates[j], reference[j]);
                cost += sample_cost;
            }
            cost /= estimates.Count;
            if (metricsManager.CurrentMetricType != MetricsManager.MetricType.Luminance) {
                cost /= 3.0;
            }
            cost *= 100; 
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
        { SolverType.MaxPercentageError, new MaxPercentageErrorSolver() },
        { SolverType.AveragePercentageError, new AveragePercentageErrorSolver() }
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
        CurrentSolverType = SolverType.AveragePercentageError;
    }
    public void populateGUI() {
        //CurrentSolverType = (SolverType)EditorGUILayout.EnumPopup(new GUIContent("Solver:", "The solver method"), CurrentSolverType, CustomStyles.defaultGUILayoutOption);
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
