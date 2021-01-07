using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StochasticOptimizer
{
    #region Private Variables
    string name;
    #endregion

    #region Public Variables
    #endregion

    #region Constructor Functions
    public StochasticOptimizer(string name) {
        //this.name = name;
    }
    #endregion

    #region Functions
    double acceptanceProbability(float currentEnergy, float newEnergy, float convergence) {
        // If the new solution is better, accept it
        if (newEnergy < currentEnergy) {
            return 1.0;
        }
        // If the new solution is worse, calculate an acceptance probability
        return Mathf.Exp((currentEnergy - newEnergy) / convergence);
    }

    void start() {
        // Set initial temp
        float convergence = 10000;
        // Cooling rate
        float reductionRate = 0.003f;

        int iterations = 50;
        // generate initial solution
        float current_solution = Random.Range(0, 1);
        float best_solution = current_solution;

        // Loop until convergence
        while (convergence > 1) {
            for (int i = 0; i < iterations; ++i) {
                float new_solution = Random.Range(0, 1);
                // generate random solution
                double acceptance = acceptanceProbability(current_solution, new_solution, convergence);
                if (acceptance > Random.Range(0, 1)) {
                    current_solution = new_solution;
                    if (best_solution > current_solution) {
                        best_solution = current_solution;
                    }
                }
            }
            convergence *= reductionRate;
        }
    }
    #endregion
}
