using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// based on Bart Wronski's Python implementation: https://github.com/bartwronski/PoissonSamplingGenerator
public class GeneratorPoisson : GeneratorInterface
{
    #region Private Variables
    private Bounds sceneBounds;
    private int num_iterations = 4;
    private int num_new_candidate_points = 32;
    private int defaultProbeCount = 128;
    private int probeCount = 128;
    #endregion

    #region Constructor Functions
    public GeneratorPoisson() : base(LumiProbesScript.PlacementType.Poisson.ToString()) { Reset(); }
    #endregion

    #region Public Override Functions
    public override void populateGUI_Initialization() {
        probeCount = EditorGUILayout.IntField(new GUIContent("Number:", "The total number of points"), probeCount, CustomStyles.defaultGUILayoutOption);
        probeCount = Mathf.Max(1, probeCount);
        EditorGUILayout.LabelField(new GUIContent("Placed:", "The total number of placed points"), new GUIContent(m_positions.Count.ToString()), CustomStyles.defaultGUILayoutOption);
    }

    public override void Reset() {
        m_positions.Clear();
        probeCount = defaultProbeCount;
    }

    public override List<Vector3> GeneratePositions(Bounds bounds) {
        List<Vector3> positions = new List<Vector3>();
        if (probeCount == 0) {
            m_positions = positions;
            return positions;
        }
        sceneBounds = bounds;
        positions = find_point_set(bounds, probeCount, num_iterations, num_new_candidate_points);
        m_positions = positions;
        m_placed_positions = m_positions.Count;
        return positions;
    }
    #endregion

    #region Private Functions
    float minDistSquared(List<Vector3> points, Vector3 new_point) {
        float min_dist = float.MaxValue;
        foreach (Vector3 point in points) {
            min_dist = Mathf.Min(min_dist, Vector3.Dot(point - new_point, point - new_point));
        }
        return min_dist;
    }

    Vector3 getNewPoint() {
        return new Vector3(
            Random.Range(sceneBounds.center.x - sceneBounds.extents.x, sceneBounds.center.x + sceneBounds.extents.x),
            Random.Range(sceneBounds.center.y - sceneBounds.extents.y, sceneBounds.center.y + sceneBounds.extents.y),
            Random.Range(sceneBounds.center.z - sceneBounds.extents.z, sceneBounds.center.z + sceneBounds.extents.z)
            );
    }

    Vector3 find_next_point(List<Vector3> current_points, int iterations_per_point) {
        // keep the point that maximizes the minimum distance between the current point set and the new random point
        float best_dist = 0;
        Vector3 best_point = new Vector3(0, 0, 0);
        for (int i = 0; i < iterations_per_point; i++) {
            Vector3 new_point = getNewPoint();
            // get the min distance between the point set and the new point
            float dist = minDistSquared(current_points, new_point);
            // keep the point that maximizes this
            if (dist > best_dist) {
                best_dist = dist;
                best_point = new_point;
            }
        }
        return best_point;
    }

    List<Vector3> find_point_set(Bounds bounds, int num_points, int num_iter, int iterations_per_point) {
        List<Vector3> best_point_set = new List<Vector3>();
        float best_dist_avg = 0.0f;

        for (int i = 0; i < num_iter; ++i) {
            List<Vector3> points = new List<Vector3>();
            points.Add(getNewPoint());

            for (int j = 0; j < num_points - 1; ++j) {
                // get the next point
                Vector3 next_point = find_next_point(points, iterations_per_point);
                points.Add(next_point);
            }

            // keep the set with the largest pairwise distance
            float current_set_dist = float.MaxValue;
            //List<float> pairwise_distances = new List<float>();
            for (int first_iter = 0; first_iter < points.Count - 1; ++first_iter) {
                for (int second_iter = first_iter + 1; second_iter < points.Count; ++second_iter) {
                    float dist = Vector3.Distance(points[first_iter], points[second_iter]);
                    //pairwise_distances.Add(dist);
                    current_set_dist = Mathf.Max(current_set_dist, dist);
                }
            }

            // keep the set with the largest pairwise distance
            if (current_set_dist > best_dist_avg) {
                best_dist_avg = current_set_dist;
                best_point_set = points;
            }
        }
        return best_point_set;
    }
    #endregion
}