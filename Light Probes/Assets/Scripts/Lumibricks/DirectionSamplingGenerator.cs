using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

class DirectionSamplingGenerator
{
    public enum LightProbesEvaluationType
    {
        FixedLow,
        FixedMedium,
        FixedHigh,
        Random
    }

    List<Vector3> evaluationFixedDirections = new List<Vector3> { 
        // LOW  (6)
        new Vector3( 1.0f, 0.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3( 0.0f, 1.0f, 0.0f),
        new Vector3( 0.0f,-1.0f, 0.0f),
        new Vector3( 0.0f, 0.0f, 1.0f),
        new Vector3( 0.0f, 0.0f,-1.0f),

        // MEDIUM (8)
        new Vector3( 1.0f, 1.0f, 1.0f),
        new Vector3( 1.0f, 1.0f,-1.0f),
        new Vector3(-1.0f, 1.0f, 1.0f),
        new Vector3(-1.0f, 1.0f,-1.0f),
        new Vector3( 1.0f,-1.0f, 1.0f),
        new Vector3( 1.0f,-1.0f,-1.0f),
        new Vector3(-1.0f,-1.0f, 1.0f),
        new Vector3(-1.0f,-1.0f,-1.0f),

        // HIGH (12)
        new Vector3( 0.0f, 1.0f, 1.0f),
        new Vector3( 0.0f, 1.0f,-1.0f),
        new Vector3( 0.0f,-1.0f, 1.0f),
        new Vector3( 0.0f,-1.0f,-1.0f),
        new Vector3( 1.0f, 0.0f, 1.0f),
        new Vector3( 1.0f, 0.0f,-1.0f),
        new Vector3( 1.0f, 1.0f, 0.0f),
        new Vector3( 1.0f,-1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 1.0f),
        new Vector3(-1.0f, 0.0f,-1.0f),
        new Vector3(-1.0f, 1.0f, 0.0f),
        new Vector3(-1.0f,-1.0f, 0.0f)
    };

    // sampling
    public LightProbesEvaluationType EvaluationType { get; set; }
    public readonly int[] evaluationFixedCount = new int[] { 6, 14, 26 };
    public int evaluationRandomSamplingCount;
    public List<Vector3> evaluationRandomDirections = new List<Vector3>();

    public DirectionSamplingGenerator() {
        evaluationRandomSamplingCount = 50;
        EvaluationType = LightProbesEvaluationType.FixedHigh;
        EvaluationType = LightProbesEvaluationType.FixedHigh;
        for (int i = 0; i < evaluationFixedDirections.Count; ++i) {
            Vector3 v1 = evaluationFixedDirections[i];
            v1.Normalize();
            evaluationFixedDirections[i] = v1;
        }

    }

    public void Reset() {

        EvaluationType = LightProbesEvaluationType.FixedHigh;
        evaluationRandomSamplingCount = 32;
    }

    public void ResetEvaluationData() {
        evaluationRandomDirections = new List<Vector3>();
    }
    public void populateGUI_EvaluateDirections() {
        EvaluationType = (LightProbesEvaluationType)EditorGUILayout.EnumPopup(new GUIContent("Type:", "The probe evaluation method"), EvaluationType, CustomStyles.defaultGUILayoutOption);
        if (EvaluationType == LightProbesEvaluationType.Random) {
            evaluationRandomSamplingCount = EditorGUILayout.IntField(new GUIContent("Number of Directions:", "The total number of uniform random sampled directions"), evaluationRandomSamplingCount, CustomStyles.defaultGUILayoutOption);
            evaluationRandomSamplingCount = Mathf.Clamp(evaluationRandomSamplingCount, 1, 1000000);
        } else {
            EditorGUILayout.LabelField(new GUIContent("Number of Directions:", "The total number of evaluation directions"), new GUIContent(evaluationFixedCount[(int)EvaluationType].ToString()), CustomStyles.defaultGUILayoutOption);
        }
    }
    public void GenerateDirections() {
        if (EvaluationType == LightProbesEvaluationType.Random) {
            if (evaluationRandomDirections.Count != evaluationRandomSamplingCount) {
                MathUtilities.GenerateUniformSphereSampling(out evaluationRandomDirections, evaluationRandomSamplingCount);
                LumiLogger.Logger.Log("Generated " + evaluationRandomSamplingCount.ToString() + " random evaluation directions");
            }
        }
    }

    public Vector3[] GetEvaluationDirections() {
        Vector3[] directions;
        if (EvaluationType == LightProbesEvaluationType.Random) {
            directions = evaluationRandomDirections.ToArray();
        } else {
            int numDirections = evaluationFixedCount[(int)EvaluationType];
            directions = evaluationFixedDirections.GetRange(0, numDirections).ToArray();
        }
        return directions;
    }

    public int GetDirectionCount() {
        return EvaluationType == LightProbesEvaluationType.Random ? evaluationRandomSamplingCount : evaluationFixedCount[(int)EvaluationType];
    }
}