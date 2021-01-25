using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

class MetricsManager
{
    public enum MetricType
    {
        RGB,
        Chrominance,
        Luminance
    }

    class Metric
    {
        public ColorTypeManager.ColorType colorType;
        public Vector3 weight;

        public Vector3 WeightedMetric(Color value, Color reference) {
            Color converted_value = colorType.convertColor(value);
            Color converted_reference = colorType.convertColor(reference);
            return new Vector3(
                (converted_value.r - converted_reference.r) * weight.x,
                (converted_value.g - converted_reference.g) * weight.y,
                (converted_value.b - converted_reference.b) * weight.z);
        }
        public Vector3 WeightedEvaluation(Color value) {
            Color converted_value = colorType.convertColor(value);
            return new Vector3(
                (converted_value.r) * weight.x,
                (converted_value.g) * weight.y,
                (converted_value.b) * weight.z);
        }
    }

    Dictionary<MetricType, Metric> LightEvaluationMetricsList = new Dictionary<MetricType, Metric> {
        { MetricType.RGB, new Metric() },
        { MetricType.Chrominance, new Metric() },
        { MetricType.Luminance, new Metric() }
    };

    public MetricType CurrentMetricType { get; private set; } = MetricType.RGB;

    private Metric currentMetric;

    public MetricsManager() {
        // init metrics
        LightEvaluationMetricsList[MetricType.RGB].weight = new Vector3(0.33f, 0.33f, 0.33f);
        LightEvaluationMetricsList[MetricType.Chrominance].weight = new Vector3(0.1f, 0.45f, 0.45f);
        LightEvaluationMetricsList[MetricType.Luminance].weight = new Vector3(0.5f, 0.25f, 0.25f);
        LightEvaluationMetricsList[MetricType.RGB].colorType = new ColorTypeManager.RGBType();
        LightEvaluationMetricsList[MetricType.Chrominance].colorType = new ColorTypeManager.YCoCgType();
        LightEvaluationMetricsList[MetricType.Luminance].colorType = new ColorTypeManager.YCoCgType();
    }

    public void populateGUI() {
        CurrentMetricType = (MetricType)EditorGUILayout.EnumPopup(new GUIContent("Metric:", "The metric used to evaluate the values"), CurrentMetricType, LumibricksScript.defaultOption);
    }
    public void SetCurrentMetric() {
        currentMetric = LightEvaluationMetricsList[CurrentMetricType];
    }

    public Vector3 computeSampleLoss(Color value, Color reference) {
        return currentMetric.WeightedMetric(value, reference);
    }

    public Vector3 evaluateSample(Color value) {
        return currentMetric.WeightedEvaluation(value);
    }
}
