using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor;

class TetrahedronGraph
{
    public List<SphericalHarmonicsL2> LightProbesBakedProbes;
    public int[] tetrahedralizeIndices;
    public Vector3[] tetrahedralizePositions;

    public int[] evaluationTetrahedronIndices;
    public Vector4[] evaluationTetrahedronWeights;
    public bool[] evaluationTetrahedronChanged;

    public void ResetTetrahedronData() {
        tetrahedralizeIndices = null;
        tetrahedralizePositions = null;
    }

    public void ResetEvaluationData() {
        evaluationTetrahedronIndices = null;
        evaluationTetrahedronWeights = null;
        evaluationTetrahedronChanged = null;
    }

    public void Tetrahedralize(List<Vector3> probePositions) {
        Lightmapping.Tetrahedralize(probePositions.ToArray(), out tetrahedralizeIndices, out tetrahedralizePositions);
        if (probePositions.Count != tetrahedralizePositions.Length) {
            LumiLogger.Logger.LogWarning("Unity considers Light Probes at the same position (within some tolerance) as duplicates, and does not include them in the tetrahedralization.\n Potential ERROR to the following computations");
        }
    }
    public Vector4 GetTetrahedronIndices(int index) {
        Vector4 indices = Vector4.zero;
        indices[0] = tetrahedralizeIndices[index * 4 + 0];
        indices[1] = tetrahedralizeIndices[index * 4 + 1];
        indices[2] = tetrahedralizeIndices[index * 4 + 2];
        indices[3] = tetrahedralizeIndices[index * 4 + 3];
        return indices;
    }

    public SphericalHarmonicsL2[] GetTetrahedronSH(Vector4 tetrahedronIndices) {
        SphericalHarmonicsL2[] tetrahedronSH = new SphericalHarmonicsL2[4];
        tetrahedronSH[0] = LightProbesBakedProbes[(int)tetrahedronIndices[0]];
        tetrahedronSH[1] = LightProbesBakedProbes[(int)tetrahedronIndices[1]];
        tetrahedronSH[2] = LightProbesBakedProbes[(int)tetrahedronIndices[2]];
        tetrahedronSH[3] = LightProbesBakedProbes[(int)tetrahedronIndices[3]];
        return tetrahedronSH;
    }

    public SphericalHarmonicsL2[] GetTetrahedronSH(int index) {
        Vector4 tetrahedronIndices = GetTetrahedronIndices(index);
        return GetTetrahedronSH(tetrahedronIndices);
    }

    public Vector3[] GetTetrahedronPositions(int index) {
        Vector4 tetrahedronIndices = GetTetrahedronIndices(index);
        return GetTetrahedronPositions(tetrahedronIndices);
    }
    public Vector3[] GetTetrahedronPositions(Vector4 tetrahedronIndices) {
        Vector3[] tetrahedronPositions = new Vector3[4];
        tetrahedronPositions[0] = tetrahedralizePositions[(int)tetrahedronIndices[0]];
        tetrahedronPositions[1] = tetrahedralizePositions[(int)tetrahedronIndices[1]];
        tetrahedronPositions[2] = tetrahedralizePositions[(int)tetrahedronIndices[2]];
        tetrahedronPositions[3] = tetrahedralizePositions[(int)tetrahedronIndices[3]];
        return tetrahedronPositions;
    }
    public List<Vector3[]> GetPositionList() {
        int tetrahedronCount = tetrahedralizeIndices.Length / 4;
        List<Vector3[]> tetrahedronPositionsList = new List<Vector3[]>(tetrahedronCount);
        for (int tetrahedronIndex = 0; tetrahedronIndex < tetrahedronCount; tetrahedronIndex++) {
            Vector3[] tetrahedronPositions = GetTetrahedronPositions(tetrahedronIndex);
            tetrahedronPositionsList.Add(tetrahedronPositions);
        }
        return tetrahedronPositionsList;
    }

    public void Init(int count) {
        evaluationTetrahedronIndices = new int[count];
        evaluationTetrahedronWeights = new Vector4[count];
        evaluationTetrahedronChanged = new bool[count];
    }

    public void ResetEvaluationTetrahedron(int index) {
        evaluationTetrahedronIndices[index] = -1;
        evaluationTetrahedronWeights[index] = Vector4.zero;
    }

    public int GetEvaluationTetrahedronIndex(int evaluationPositionIndex) {
        return evaluationTetrahedronIndices[evaluationPositionIndex];
    }
    public Vector4 GetEvaluationTetrahedronWeights(int evaluationPositionIndex) {
        return evaluationTetrahedronWeights[evaluationPositionIndex];
    }

    public void SetEvaluationTetrahedronIndex(int evaluationPositionIndex, int tetrahedronIndex) {
        evaluationTetrahedronIndices[evaluationPositionIndex] = tetrahedronIndex;
    }

    public void SetEvaluationTetrahedronWeights(int evaluationPositionIndex, Vector4 tetrahedronWeights) {
        evaluationTetrahedronWeights[evaluationPositionIndex] = tetrahedronWeights;
    }
    public bool UnMappedEvaluationState(int evaluationPositionIndex) {
        return evaluationTetrahedronIndices[evaluationPositionIndex] == -1;
    }
    public bool ChangedEvaluationState(int evaluationPositionIndex) {
        return evaluationTetrahedronChanged[evaluationPositionIndex] == true;
    }
    public void FlagEvaluationPointΑsChanged(int index, bool changed) {
        evaluationTetrahedronChanged[index] = changed;
    }

    public int getNumTetrahedrons() {
        return tetrahedralizeIndices.Length / 4;
    }
}