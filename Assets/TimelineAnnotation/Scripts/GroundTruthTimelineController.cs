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
                Transform parentTransform = timeline.baseLine.transform;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(parentTransform.position.x, parentTransform.position.y - 0.01f, parentTransform.position.z + 0.025f);
                go.transform.localScale = new Vector3(parentTransform.localScale.x, 0.01f, 0.05f);
                go.transform.parent = parentTransform;
            }
            //---------------------------------


            // Create a series of blocks along the base of the timeline representing the events
            foreach (AnnotationRecord rec in groundTruthModelManager.groundTruthModel.annotations)
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

            // TODO: calculate position and scale
            /*
            
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(parentTransform.position.x + positionX, parentTransform.position.y - 0.02f, parentTransform.position.z + 0.025f);
            go.transform.localScale = new Vector3(parentTransform.localScale.x * (float)scaleX, 0.01f, 0.05f);
            go.transform.parent = parentTransform;

            */
        }
    }

}