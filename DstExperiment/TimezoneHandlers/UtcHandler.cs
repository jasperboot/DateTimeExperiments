using DstExperiment.TimezoneHandlers.Extensions;
using System;

namespace DstExperiment.TimezoneHandlers
{
    public class UtcHandler : ITimezoneHandler
    {
        public string Description => "Unique UTC datetime";
        public TimeZoneHandlerType HandlerType => TimeZoneHandlerType.UtcHandler;

        public DateTime ConvertFromUtcToLocal(DateTime utcDateTime)
        {
            return utcDateTime;
        }

        public DateTime ConvertFromLocalToUtc(DateTime localDateTime)
        {
            return localDateTime;
        }

        public DateTime ConvertFromLocalToMostProbableUtc(DateTime newLocalDateTime, DateTime oldUtcDateTime, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None)
        {
            return newLocalDateTime;
        }

        public bool IsAmbiguous(DateTime localDateTime)
        {
            return false;
        }

        public bool IsDaylightSavingTime(DateTime localDateTime)
        {
            return false;
        }
    }
}
