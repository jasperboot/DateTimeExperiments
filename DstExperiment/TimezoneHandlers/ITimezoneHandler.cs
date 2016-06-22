using DstExperiment.TimezoneHandlers.Extensions;
using System;

namespace DstExperiment.TimezoneHandlers
{
    public enum TimeZoneHandlerType
    {
        UtcHandler = 0,
        LocalZoneHandler = 1
    }

   public interface ITimezoneHandler
    {
        TimeZoneHandlerType HandlerType { get; }
        string Description { get; }

        DateTime ConvertFromUtcToLocal(DateTime utcDateTime);
        DateTime ConvertFromLocalToUtc(DateTime localDateTime);
        DateTime ConvertFromLocalToMostProbableUtc(DateTime newLocalDateTime, DateTime oldUtcDateTime, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None);
        bool IsDaylightSavingTime(DateTime localDateTime);
        bool IsAmbiguous(DateTime localDateTime);
    }
}
