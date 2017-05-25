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
        public bool showFullDetails = false;

        TimeSpan? _timeRangeStart;
        TimeSpan? _timeRangeEnd;

        DataModel _dataModel = new DataModel();
        bool _waitingForData = true;
        bool _contentDirty = true;      // General flag indicating that the text content should be regenerated

        List<AnnotationRecord> _activeRecords = new List<AnnotationRecord>();

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
                long msStart = (long)(_timeRangeStart.GetValueOrDefault().TotalMilliseconds);
                long msEnd = (long)(_timeRangeEnd.GetValueOrDefault().TotalMilliseconds);

                DateTime dt1 = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dt1 = dt1.AddMilliseconds(msStart).ToLocalTime();

                DateTime dt2 = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dt2 = dt2.AddMilliseconds(msEnd).ToLocalTime();

                List<AnnotationRecord> recentRecs;
                List<AnnotationRecord> upcomingRecs;
                List<AnnotationRecord> activeRecs = FindRelevantRecords(msStart, msEnd, out recentRecs, out upcomingRecs);

                result = "<b>Active time range</b>: ";
                result += dt1 + "  --  ";
                result += dt2 + "\r\n\r\n";
                if (recentRecs.Count + upcomingRecs.Count + activeRecs.Count == 0)
                {
                    result += "<color=#CCCCCC><i>No comments for this time range</i></color>";
                } else
                {
                    result += "<color=#FFCCCC>";
                    result += GetRecordsText(activeRecs, null);
                    result += "</color><color=#FFFFCC>";
                    result += GetRecordsText(upcomingRecs, "Upcoming");
                    result += "</color><color=#DDDDDD>";
                    result += GetRecordsText(recentRecs, "Recent");
                    result += "</color>";
                }

                //------------------------------------------------------------------
                // If something new has entered the active list, notify with a sound
                bool alreadyNoted = true;
                foreach (AnnotationRecord rec in activeRecs)
                {
                    if (!_activeRecords.Contains(rec))
                    {
                        alreadyNoted = false;
                        break;
                    }
                }
                if (!alreadyNoted)
                {
                    AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
                    foreach (AudioSource audio in audioSources)
                    {
                        if (audio.name.Equals("AlertNotification"))
                        {
                            audio.Play();
                            break;
                        }
                    }
                    
                }
                _activeRecords = activeRecs;
                //------------------------------------------------------------------

            }
            return result;
        }

        string GetRecordsText(List<AnnotationRecord> recs, string title)
        {
            if (recs.Count == 0)
            {
                return "";
            }
            string result = "";
            if (title != null)
            {
                result += "<b>" + title + "</b>\r\n";
            }
            foreach (AnnotationRecord rec in recs)
            {
                result += GetRecordText(rec);
                result += "\r\n";
            }
            return result;
        }
        string GetRecordText(AnnotationRecord rec)
        {
            string result = "";
            result += "<b>" + rec.aggregateEvent + "</b>";
            result += " (" + rec.when + ")\r\n";
            if (rec.eventDetail != null)
            {
                result += rec.eventDetail + "\r\n";
            }
            if (rec.dataSourceAndIndicator != null)
            {
                result += "<i><color=#DDDDDD>" + rec.dataSourceAndIndicator + "</color></i>\r\n";
            }

            if (showFullDetails)
            {
                result += ListToString(rec.sourceIPs, "Source IPs");
                result += ListToString(rec.targetHostnames, "Target Hostnames");
                result += ListToString(rec.targetIPInternals, "Target IPs (Internal)");
                result += ListToString(rec.targetIPExternals, "Target IPs (External)");
                result += ListToString(rec.ports, "Ports");
            }

            return result;
        }

        string ListToString(List<string> list, string label)
        {
            if (list == null || list.Count == 0)
            {
                return "";
            }

            string result = label + ": ";
            bool first = true;
            foreach (string str in list)
            {
                if (!first)
                {
                    result += ", ";
                } else
                {
                    first = false;
                }
                result += str;
            }

            result += "\r\n";
            return result;
        }

        List<AnnotationRecord> FindRelevantRecords(long msStart, long msEnd, out List<AnnotationRecord> recent, out List<AnnotationRecord> upcoming) {
            List<AnnotationRecord> result = new List<AnnotationRecord>();
            recent = new List<AnnotationRecord>();
            upcoming = new List<AnnotationRecord>();

            long msNearStart = msStart - (6 * 60 * 60 * 1000);  // 6 hours before window
            long msNearEnd = msEnd + (6 * 60 * 60 * 1000);  // 6 hours before window

            foreach (AnnotationRecord rec in _dataModel.groundTruthModel.annotations)
            {
                if (TimeRangeContains(msStart, msEnd, rec.timeStart, rec.timeEnd))
                {
                    result.Add(rec);
                }
                else if (TimeRangeContains(msEnd, msNearEnd, rec.timeStart, rec.timeEnd))
                {
                    upcoming.Add(rec);
                }
                else if (TimeRangeContains(msNearStart, msStart, rec.timeStart, rec.timeEnd))
                {
                    recent.Add(rec);
                }
            }

            return result;
        }

        static bool TimeRangeContains(long windowStart, long windowEnd, long eventStart, long eventEnd)
        {
            // there are really only two conditions that we need to check - the inverse scenario
            // where the start and end of the event occur either left or right of the window
            if (eventStart < windowStart && eventEnd < windowStart)
            {
                return false;
            }
            if (eventStart > windowEnd && eventEnd > windowEnd)
            {
                return false;
            }

            return true;
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
