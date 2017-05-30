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
        [Tooltip("Material that will be used for the markers along the timeline")]
        public Material eventMaterial;

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
            List<GameObject> objsToRotate = new List<GameObject>();
            foreach (AnnotationRecord rec in sortedList)
            {
                bool shouldRotate;
                GameObject obj = CreateEventMarker(rec, out shouldRotate);
                if (obj != null && shouldRotate)
                {
                    objsToRotate.Add(obj);
                }
            }
            foreach(GameObject obj in objsToRotate)
            {
                obj.transform.localScale = new Vector3(obj.transform.localScale.x * 0.8f, obj.transform.localScale.y * 0.8f, obj.transform.localScale.z);
                obj.transform.Rotate(0, 0, 45, Space.Self);
            }
        }

        
        GameObject CreateEventMarker(AnnotationRecord rec, out bool shouldRotate)
        {
            if (rec == null)
            {
                shouldRotate = false;
                return null;
            }

            Transform parentTransform = timeline.baseLine.transform;
            long t1 = rec.timeStart / 1000;
            long t2 = rec.timeEnd / 1000;

            if (t2 < minUTCTime)
            {
                shouldRotate = false;
                return null;
            }
            long fullRangeDiff = this.maxUTCTime - this.minUTCTime;
            long eventDiff = t2 - t1;
            long offset = ((t2 - this.minUTCTime) + (t1 - this.minUTCTime)) / 2;
            float scaleX = ((float)eventDiff) / (float)fullRangeDiff;
            float offsetX = (((float)offset) / (float)fullRangeDiff) - 0.5f;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (eventMaterial != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = eventMaterial;
                }
            }
            go.transform.position = new Vector3(parentTransform.position.x + offsetX, parentTransform.position.y - 0.01f, parentTransform.position.z + 0.025f);
            if (scaleX <= 0)
            {
                go.transform.localScale = new Vector3(parentTransform.localScale.x * 0.01f, 0.01f, 0.05f);
            }
            else
            {
                go.transform.localScale = new Vector3(parentTransform.localScale.x * (float)scaleX, 0.01f, 0.05f);
            }

            
            bool foundOverlap = isOverlappingExistingMarker(go);
            int loopEscape = 0;
            while (foundOverlap && loopEscape < 6)
            {
                go.transform.Translate(0, -0.011f, 0, Space.World);
                foundOverlap = isOverlappingExistingMarker(go);
                loopEscape++;
            }
            go.transform.parent = parentTransform;

            shouldRotate = (scaleX <= 0);
            
            markers.Add(go);
            return go;
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