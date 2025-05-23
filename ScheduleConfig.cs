using System.Collections.Generic;

namespace ChildUsageEnforcer
{
    public class TimeRange
    {
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class ScheduleConfig
    {
        public List<TimeRange> AllowedTimeRanges { get; set; }
    }
}
