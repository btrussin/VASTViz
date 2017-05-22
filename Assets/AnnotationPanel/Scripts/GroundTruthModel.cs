using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace AnnotationPanel
{
    class AnnotationModelManager
    {
        public GroundTruthModel LoadGroundTruthModel(string dataPath)
        {
            if (File.Exists(dataPath))
            {
                string jsonText = File.ReadAllText(dataPath);
                return JsonUtility.FromJson<GroundTruthModel>(jsonText);
            }
            return null;
        }
    }

    [Serializable]
    public class GroundTruthModel {
        public string title;
        public string description;
        public List<AnnotationRecord> annotations = new List<AnnotationRecord>();
    }

    [Serializable]
    public class AnnotationRecord
    {
        public int excelSheetRow;
        public string when;
        public string aggregateEvent;
        public string degreeOfSubtlety;
        public string dataSourceAndIndicator;
        public string eventDetail;
        public List<string> sourceIPs = new List<string>();
        public List<string> targetHostnames = new List<string>();
        public List<string> targetIPExternals = new List<string>();
        public List<string> targetIPInternals = new List<string>();
        public List<string> ports = new List<string>();
    }

 
}