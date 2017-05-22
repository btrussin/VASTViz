using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace AnnotationPanel
{
    public class AnnotationPanel : MonoBehaviour
    {
        public string annotationPanelDataPath = "E:\\groundTruth.json";

        DateTime _timeRangeStart;
        DateTime _timeRangeEnd;

        DataModel _dataModel = new DataModel();
        bool _waitingForData = true;
        bool _timeRangeDirty = true;    // Flag indicating to the update method that the timerange has changed

        public GroundTruthModel groundTruthModel
        {
            get {
                if (_dataModel.dataLoadComplete)
                {
                    return _dataModel.groundTruthModel;
                }
                return null;
            }
        }

        public void setTimeRange(DateTime timeStart, DateTime timeEnd)
        {
            if (DateTime.Equals(timeStart, this._timeRangeStart) && DateTime.Equals(timeEnd, this._timeRangeEnd))
            {
                // No actual change
                return;
            }

            this._timeRangeStart = timeStart;
            this._timeRangeEnd = timeEnd;
            this._timeRangeDirty = true;
        }

        // Use this for initialization
        void Start()
        {
            _dataModel.Initialize(annotationPanelDataPath);
        }

        // Update is called once per frame
        void Update()
        {
            if (_waitingForData)
            {
                if (_dataModel.dataLoadComplete)
                {
                    // At this point, we now have the data loaded, so we can do whatever
                    // initialization we need to do based on the data.
                    this.InitializeWithLoadedData();
                    _waitingForData = false;
                } else
                {
                    DelegateUpdateWaitingForData();
                    return;
                }
            }


        }

        /// <summary>
        /// Delegate Update method for when the data has yet to load. For this case, this is not 
        /// expected to be invoked much since the data is not particular large.
        /// </summary>
        void DelegateUpdateWaitingForData()
        {
        }

        /// <summary>
        /// Called from the Update loop the first time that data is in the loaded state.
        /// This is where you should instantiate/initialize things that depend on the 
        /// actual data.
        /// </summary>
        void InitializeWithLoadedData()
        {
            TextMesh t = FindTextMesh("StatusText");
            if (t)
            {
                t.text = "Loaded Data: " + _dataModel.groundTruthModel.annotations.Count;

                //foreach (AnnotationRecord rec in _dataModel.groundTruthModel.annotations)
                //{
                //    string msg = "JLH Record: " + rec.when + "\r\n";
                //    foreach (string str in rec.sourceIPs)
                //    {
                //        msg += str + " ------ ";
                //    }
                //    Debug.Log(msg);
                //    
                //}
                
            }
        }

        TextMesh FindTextMesh(string name)
        {
            TextMesh[] textMeshes = GetComponentsInChildren<TextMesh>();
            foreach (TextMesh text in textMeshes)
            {
                if (text.name.Equals(name))
                {
                    return text;
                }
            }
            return null;
        }
    }


    /// <summary>
    /// Contains all of relevant data for the panel.
    /// </summary>
    class DataModel
    {
        public bool dataLoadComplete = false;
        public GroundTruthModel groundTruthModel;

        private Thread thread;
        private string dataPath;

        public void Initialize(string dataPath)
        {
            if (thread == null)
            {
                this.dataPath = dataPath;
                thread = new Thread(new ThreadStart(this.LoadDataModel));
                thread.Start();
            }
        }

        void LoadDataModel()
        {
            this.groundTruthModel = new AnnotationModelManager().LoadGroundTruthModel(dataPath);
            dataLoadComplete = true;
            thread = null;
        }
    }
}
