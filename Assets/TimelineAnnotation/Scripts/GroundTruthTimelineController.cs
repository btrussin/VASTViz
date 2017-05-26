using RealityCheckData;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace AnnotationTimeline
{
    public class GroundTruthTimelineController : MonoBehaviour, RealityCheck.ITimeRangeListener
    {
        public GroundTruthModelManager groundTruthModelManager;
        public TimelineScript timeline;

        TimeSpan? _timeRangeStart;
        TimeSpan? _timeRangeEnd;

        bool _waitingForData = true;
        List<GameObject> markers = new List<GameObject>();

        public long minUTCTime = 1364802600; // first minute: 1364802600:  04-01-2013 07:50:00
        public long maxUTCTime = 1366045200; // last minute: 1366045200: 04-15-2013 17:00:00

        public void SetActiveTimeRange(TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            if (TimeSpan.Equals(timeStart, this._timeRangeStart) && TimeSpan.Equals(timeEnd, this._timeRangeEnd))
            {
                // No actual change
                return;
            }

            this._timeRangeStart = timeStart;
            this._timeRangeEnd = timeEnd;
            //this._contentDirty = true;
        }


        // Use this for initialization
        void Start()
        {
            if (groundTruthModelManager == null)
            {
                Debug.LogError("Need to assign the GroundTruthModelManager to the object " + this.name);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (groundTruthModelManager == null)
            {
                return;
            }

            if (_waitingForData)
            {
                if (groundTruthModelManager.dataLoadComplete)
                {
                    // At this point, we now have the data loaded, so we can do whatever
                    // initialization we need to do based on the data. (Enable buttons, etc)
                    _waitingForData = false;
                    this.InitializeWithLoadedData();
                }
            }
        }

        /// <summary>
        /// Called from the Update loop the first time that data is in the loaded state.
        /// This is where you should instantiate/initialize things that depend on the 
        /// actual data.
        /// </summary>
        void InitializeWithLoadedData()
        {
            if (timeline == null)
            {
                Debug.LogError("Need to assign the timeline to the AnnotationTimeline script for object " + this.name);
                return;
            }

            //---------------------------------
            // Partition
            {
                //Transform parentTransform = timeline.baseLine.transform;
                //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //go.transform.position = new Vector3(parentTransform.position.x, parentTransform.position.y - 0.01f, parentTransform.position.z + 0.025f);
                //go.transform.localScale = new Vector3(parentTransform.localScale.x, 0.01f, 0.05f);
                //go.transform.parent = parentTransform;
            }
            //---------------------------------

            List<AnnotationRecord> sortedList = new List<AnnotationRecord>();
            sortedList.AddRange(groundTruthModelManager.groundTruthModel.annotations);
            sortedList.Sort(delegate (AnnotationRecord rec1, AnnotationRecord rec2)
            {
                long diff1 = rec1.timeEnd - rec1.timeStart;
                long diff2 = rec2.timeEnd - rec2.timeStart;
                if (diff1 > diff2)
                {
                    return -1;
                }
                if (diff1 < diff2)
                {
                    return 1;
                }
                return (int)(rec1.timeStart - rec2.timeStart);
            });
            // Create a series of blocks along the base of the timeline representing the events
            foreach (AnnotationRecord rec in sortedList)
            {
                CreateEventMarker(rec);
            }

        }

        
        void CreateEventMarker(AnnotationRecord rec)
        {
            if (rec == null)
            {
                return;
            }
            Transform parentTransform = timeline.baseLine.transform;
            long t1 = rec.timeStart / 1000;
            long t2 = rec.timeEnd / 1000;
            long fullRangeDiff = this.maxUTCTime - this.minUTCTime;
            long eventDiff = t2 - t1;
            long offset = ((t2 - this.minUTCTime) + (t1 - this.minUTCTime)) / 2;
            float scaleX = ((float)eventDiff) / (float)fullRangeDiff;
            float offsetX = (((float)offset) / (float)fullRangeDiff) - 0.5f;

            if (scaleX <= 0)
            {
                scaleX = 0.01f;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(parentTransform.position.x + offsetX, parentTransform.position.y - 0.01f, parentTransform.position.z + 0.025f);
            go.transform.localScale = new Vector3(parentTransform.localScale.x * (float)scaleX, 0.01f, 0.05f);
            go.transform.parent = parentTransform;

            bool foundOverlap = isOverlappingExistingMarker(go);
            int loopEscape = 0;
            while (foundOverlap && loopEscape < 6)
            {
                go.transform.Translate(0, -0.011f, 0);
                foundOverlap = isOverlappingExistingMarker(go);
                loopEscape++;
            }

            markers.Add(go);
        }

        bool isOverlappingExistingMarker(GameObject go)
        {
            Bounds testBounds = go.GetComponent<Renderer>().bounds;
            foreach (GameObject marker in markers)
            {
                if (marker.GetComponent<Renderer>().bounds.Intersects(testBounds))
                {
                    return true;
                }
            }
            return false;
        }
    }

}