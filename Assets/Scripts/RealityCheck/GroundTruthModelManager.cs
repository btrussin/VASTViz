using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Threading;

namespace RealityCheckData
{
    public class GroundTruthModelManager : MonoBehaviour
    {
        public string groundTruthDataPath = "E:\\groundTruth.json";

        private bool _dataLoadComplete = false;
        private GroundTruthModel _groundTruthModel;

        private Thread thread;
        private string dataPath;
        private string backupDataPath;


        public GroundTruthModel groundTruthModel
        {
            get
            {
                if (_dataLoadComplete)
                {
                    return _groundTruthModel;
                }
                return null;
            }
        }
        public bool dataLoadComplete
        {
            get
            {
                return _dataLoadComplete;
            }
        }

        public GroundTruthModel LoadGroundTruthModel(string dataPath)
        {
            if (File.Exists(dataPath))
            {
                string jsonText = File.ReadAllText(dataPath);
                return JsonUtility.FromJson<GroundTruthModel>(jsonText);
            }
            return null;
        }

        // Use this for initialization
        void Start()
        {
            string backupDataPath = Directory.GetParent(Application.dataPath).FullName + "/groundTruth.json";
            Initialize(groundTruthDataPath, backupDataPath);
        }


        private void Initialize(string dataPath, string backupDataPath)
        {
            if (thread == null)
            {
                this.dataPath = dataPath;
                this.backupDataPath = backupDataPath;
                thread = new Thread(new ThreadStart(this.LoadDataModel));
                thread.Start();
            }
        }

        private void LoadDataModel()
        {
            _groundTruthModel = LoadGroundTruthModel(dataPath);
            if (groundTruthModel == null)
            {
                _groundTruthModel = LoadGroundTruthModel(backupDataPath);
            }
            _dataLoadComplete = true;
            thread = null;
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
        public long timeStart;
        public long timeEnd;
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