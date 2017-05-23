using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;

namespace AnnotationPanel
{
    public class AnnotationPanelController : MonoBehaviour, RealityCheck.ITimeRangeListener
    {
        public string annotationPanelDataPath = "E:\\groundTruth.json";

        TimeSpan? _timeRangeStart;
        TimeSpan? _timeRangeEnd;

        DataModel _dataModel = new DataModel();
        bool _waitingForData = true;
        bool _contentDirty = true;      // General flag indicating that the text content should be regenerated

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

        public void SetActiveTimeRange(TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            if (TimeSpan.Equals(timeStart, this._timeRangeStart) && TimeSpan.Equals(timeEnd, this._timeRangeEnd))
            {
                // No actual change
                return;
            }

            this._timeRangeStart = timeStart;
            this._timeRangeEnd = timeEnd;
            this._contentDirty = true;
        }

        // Use this for initialization
        void Start()
        {
            _dataModel.Initialize(annotationPanelDataPath);
        }

        // Update is called once per frame
        void Update()
        {
            if (_contentDirty)
            {
                _contentDirty = false;
                RefreshTextContent();
            }
            if (_waitingForData)
            {
                if (_dataModel.dataLoadComplete)
                {
                    // At this point, we now have the data loaded, so we can do whatever
                    // initialization we need to do based on the data. (Enable buttons, etc)
                    _waitingForData = false;
                    this.InitializeWithLoadedData();
                    _contentDirty = true;
                }
                else
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
        }

        void RefreshTextContent()
        {
            Text t = FindTextObject("Text");
            if (t)
            {
                t.text = GenerateTextContent();
            }
        }

        Text FindTextObject(string name)
        {
            Text[] texts = GetComponentsInChildren<Text>();
            foreach (Text text in texts)
            {
                if (text.name.Equals(name))
                {
                    return text;
                }
            }
            return null;
        }

        string GenerateTextContent()
        {
            string result = "";
            if (_timeRangeStart == null && _timeRangeEnd == null)
            {
                // Note that no active time range and show stats.
                result = "<i>Note: No active time range set.</i>\r\n\r\n";
                if (!_dataModel.dataLoadComplete)
                {
                    result += "Loading data...";
                } else
                {
                    result += "<b>Number of loaded annotations</b>: " + _dataModel.groundTruthModel.annotations.Count;
                }
                return result;
            } else
            {
                DateTime dt1 = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dt1 = dt1.AddMilliseconds((long)(_timeRangeStart.GetValueOrDefault().TotalMilliseconds)).ToLocalTime();

                DateTime dt2 = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dt2 = dt2.AddMilliseconds((long)(_timeRangeEnd.GetValueOrDefault().TotalMilliseconds)).ToLocalTime();

                result = "<b>Active time range</b>: \r\n";
                result += "\t" + dt1 + "\r\n";
                result += "\t" + dt2 + "\r\n";
                
                result += "\r\n\r\nAs of " + DateTime.Now;
            }
            return result;
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
