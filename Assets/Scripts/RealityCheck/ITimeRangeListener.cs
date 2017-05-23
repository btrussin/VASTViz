using System;

namespace RealityCheck
{
    /// <summary>
    /// Implementations expect to be notified when the active time range is updated.
    /// </summary>
    public interface ITimeRangeListener
    {
        void SetActiveTimeRange(TimeSpan? timeStart, TimeSpan? timeEnd);
    }

    /// <summary>
    /// Implementations of this interface behave as a maintainer of the current
    /// active time range. This can be used for situations where the timerange is
    /// accessed through polling.
    /// </summary>
    public interface ITimeRangeProvider
    {
        TimeSpan? getActiveTimeRangeStart();
        TimeSpan? getActiveTimeRangeEnd();
    }


}
