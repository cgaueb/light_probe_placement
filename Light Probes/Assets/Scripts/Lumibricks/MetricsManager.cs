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

        public virtual Vector3 WeightedMetric(Color value, Color reference) {
            Color converted_value = colorType.convertColor(value);
            Color converted_reference = colorType.convertColor(reference);
            return new Vector3(
                (converted_value.r - converted_reference.r) * weight.x,
                (converted_value.g - converted_reference.g) * weight.y,
                (converted_value.b - converted_reference.b) * weight.z);
        }
        public virtual Vector3[] WeightedEvaluation(Color value, Color reference) {
            Color converted_value = colorType.convertColor(value);
            Color converted_reference = colorType.convertColor(reference);
            return new Vector3[] {
                new Vector3(
                    (converted_value.r) * weight.x,
                    (converted_value.g) * weight.y,
                    (converted_value.b) * weight.z),
                new Vector3(
                    (converted_reference.r) * weight.x,
                    (converted_reference.g) * weight.y,
                    (converted_reference.b) * weight.z)
            };
        }
    }

    class MetricLuminance : Metric
    {
        public override Vector3 WeightedMetric(Color value, Color reference) {
            Color converted_value = colorType.convertColor(value);
            Color converted_reference = colorType.convertColor(reference);
            Vector3 res = new Vector3((converted_value.r - converted_reference.r) * weight.x, 0.0f, 0.0f);
            res.y = res.x;
            res.z = res.x;
            return res;
        }
        public override Vector3[] WeightedEvaluation(Color value, Color reference) {
            // TODO: only compute the luminance value
            Color converted_value = colorType.convertColor(value);
            Color converted_reference = colorType.convertColor(reference);
            Vector3[] res = new Vector3[2];
            res[0] = new Vector3(converted_value.r * weight.x, 0.0f, 0.0f);
            res[0].y = res[0].x;
            res[0].z = res[0].x;
            res[1] = new Vector3(converted_reference.r * weight.x, 0.0f, 0.0f);
            res[1].y = res[1].x;
            res[1].z = res[1].x;
            return res;
        }
    }

        Dictionary<MetricType, Metric> LightEvaluationMetricsList = new Dictionary<MetricType, Metric> {
        { MetricType.RGB, new Metric() },
        { MetricType.Chrominance, new Metric() },
        { MetricType.Luminance, new MetricLuminance() }
    };

    public MetricType CurrentMetricType { get; private set; }

    private Metric currentMetric;

    public MetricsManager() {
        // init metrics
        LightEvaluationMetricsList[MetricType.RGB].weight = new Vector3(0.33f, 0.33f, 0.33f);
        LightEvaluationMetricsList[MetricType.Chrominance].weight = new Vector3(0.1f, 0.45f, 0.45f);
        LightEvaluationMetricsList[MetricType.Luminance].weight = new Vector3(1.0f, 0.0f, 0.0f);
        LightEvaluationMetricsList[MetricType.RGB].colorType = new ColorTypeManager.RGBType();
        LightEvaluationMetricsList[MetricType.Chrominance].colorType = new ColorTypeManager.YCoCgType();
        LightEvaluationMetricsList[MetricType.Luminance].colorType = new ColorTypeManager.YCoCgType();
        Reset();
    }
    
    public void Reset() {
        CurrentMetricType = MetricType.Chrominance;
    }

    public void populateGUI() {
        CurrentMetricType = (MetricType)EditorGUILayout.EnumPopup(new GUIContent("Metric:", "The metric used to evaluate the values"), CurrentMetricType, CustomStyles.defaultGUILayoutOption);
    }
    public void SetCurrentMetric() {
        currentMetric = LightEvaluationMetricsList[CurrentMetricType];
    }

    public Vector3 computeSampleLoss(Color value, Color reference) {
        return currentMetric.WeightedMetric(value, reference);
    }

    public Vector3[] evaluateSample(Color value, Color reference) {
        return currentMetric.WeightedEvaluation(value, reference);
    }
}
