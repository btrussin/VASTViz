using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnnotationPanel;

namespace RealityCheck
{
    /// <summary>
    /// Acts as a hub for session state information.
    /// </summary>
    public class RealitySessionState : MonoBehaviour, ITimeRangeListener, ITimeRangeProvider
    {

        public List<MonoBehaviour> timeRangeListeners = new List<MonoBehaviour>();
        
        private TimeSpan? _timeRangeStart;
        private TimeSpan? _timeRangeEnd;

        // Use this for initialization
        void Start()
        {
            // NOOP
        }

        // Update is called once per frame
        void Update()
        {
            // NOOP
        }


        public void SetActiveTimeRange(TimeSpan? timeStart, TimeSpan? timeEnd)
        {
            this._timeRangeStart = timeStart;
            this._timeRangeEnd = timeEnd;

            NotifyTimeRangeUpdate();
        }
        public void SetActiveTime(TimeSpan? time)
        {
            SetActiveTimeRange(time, time);
        }

        private void NotifyTimeRangeUpdate()
        {
            foreach (MonoBehaviour item in timeRangeListeners)
            {
                ITimeRangeListener listener = item as ITimeRangeListener;
                if (listener != null)
                {
                    listener.SetActiveTimeRange(_timeRangeStart, _timeRangeEnd);
                }
            }
        }

        public TimeSpan? getActiveTimeRangeStart()
        {
            return _timeRangeStart;
        }

        public TimeSpan? getActiveTimeRangeEnd()
        {
            return _timeRangeEnd;
        }
    }

}
