using DstExperiment.Extensions;
using MichaelBrumm.Globalization;
using System;

namespace DstExperiment.TimezoneHandlers
{
    public class SimpleTimeZoneHandler : ITimezoneHandler
    {
        public SimpleTimeZoneHandler(SimpleTimeZone timezone)
        {
            this.Timezone = timezone;
        }
        public SimpleTimeZone Timezone { get; private set; }

        public string Description => "Michael Brumm SimpleTimeZone";
        public TimeZoneHandlerType HandlerType => TimeZoneHandlerType.LocalZoneHandler;

        public DateTime ConvertFromUtcToLocal(DateTime utcDateTime)
        {
            return Timezone.ToLocalTime(utcDateTime);
        }

        public DateTime ConvertFromLocalToUtc(DateTime localDateTime)
        {
            return Timezone.ToUniversalTime(localDateTime);
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
            return Timezone.IsAmbiguous(localDateTime);
        }
    }
}
