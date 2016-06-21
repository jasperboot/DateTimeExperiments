using DstExperiment.Extensions;
using System;

namespace DstExperiment.TimezoneHandlers
{
    public class TimeZoneInfoHandler : ITimezoneHandler
    {
        public TimeZoneInfoHandler(TimeZoneInfo timezone)
        {
            this.Timezone = timezone;
        }
        public TimeZoneInfo Timezone { get; private set; }

        public string Description => "BCL TimeZoneInfo";
        public TimeZoneHandlerType HandlerType => TimeZoneHandlerType.LocalZoneHandler;

        public DateTime ConvertFromUtcToLocal(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, Timezone);
        }

        public DateTime ConvertFromLocalToUtc(DateTime localDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, Timezone);
        }

        public DateTime ConvertFromLocalToMostProbableUtc(DateTime newLocalDateTime, DateTime oldUtcDateTime, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None)
        {
            return newLocalDateTime.ToMostProbableUniversal(oldUtcDateTime, timeModificationAssumption, Timezone);
        }

        public bool IsDaylightSavingTime(DateTime localDateTime)
        {
            return Timezone.IsDaylightSavingTime(localDateTime);
        }

        public bool IsAmbiguous(DateTime localDateTime)
        {
            return Timezone.IsAmbiguousTime(localDateTime);
        }
    }
}
