using MichaelBrumm.Globalization;
using System;
using static DstExperiment.TimezoneHandlers.Extensions.DateTimeExtensions;

namespace DstExperiment.TimezoneHandlers.Extensions
{
    public static class SimpleTimeZoneExtensions
    {
        public static DateTime ToMostProbableUniversalTime(this SimpleTimeZone timezone, DateTime newValueLocal, DateTime oldValueUTC, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None)
        {
            // Check for invalid NULL parameters
            if (timezone == null)
                throw new ArgumentNullException(nameof(timezone));

            // Check whether the supplied DateTime objects are not of the wrong kind
            // Preferably one wouldn't use DateTimeKind.Unspecified either, but we mimic the BCL here
            if (newValueLocal.Kind == DateTimeKind.Utc)
                throw new ArgumentException("Argument can not be of kind DateTimeKind.Utc", nameof(newValueLocal));
            if (oldValueUTC.Kind == DateTimeKind.Local)
                throw new ArgumentException("Argument can not be of kind DateTimeKind.Local", nameof(oldValueUTC));

            // If the local DateTime is not ambiguous, return the corresponding UTC value
            if (!timezone.IsAmbiguous(newValueLocal))
                return timezone.ToUniversalTime(newValueLocal);

            // We have an ambiguous local DateTime...
            {
                // Find the DST rule that is/was active around the time of the newly supplied local DateTime
                var activeDstRule = timezone.GetDaylightChanges(newValueLocal.Year);

                // Although activeDstRule should not be null, as we have determined already that we have an ambiguous local DateTime, just make sure...
                if (activeDstRule == null)
                    return timezone.ToUniversalTime(newValueLocal); // Or should we throw an Exception instead?

                //  Convert the local DateTime to UTC according to what MichealBrumm.Globalization.SimpleTimeZone's naive implementation seems to do:
                //  ( => Always assume the datetime was meant to fall in the first hour, i.e. Daylight Savings Time in this case)
                var naiveNewUTC = timezone.ToUniversalTime(newValueLocal);

                // ASSUMPTION 1: If the new naive UTC value matches the old UTC value, we assume this is correct and the datetime (falling in the 1st hour) wasn't changed
                if (naiveNewUTC == oldValueUTC)
                    return naiveNewUTC;

                // Get the DST offset from the current DST rule
                var dstOffset = activeDstRule.Delta;
                // Get the UTC datetime for the new local DateTime, but assuming the datetime was meant to fall in Standard Time, i.e. the second hour
                var adjustedNewUTC = naiveNewUTC + dstOffset;

                // ASSUMPTION 2: If this adjusted UTC value matches the old UTC value, we assume this is corrent and the datime (falling in the 2nd hour) wasn't changed
                if (adjustedNewUTC == oldValueUTC)
                    return adjustedNewUTC;

                // We've now matched any unchanged values. From here on we're certain that the new value is changed from the old value.
                // In that case all we can do is add in our own heuristics, but to really know which correct UTC value was meant, the UI should have asked the user
                // When filling in the local datetime manually in the first place (e.g. "Was this 2:14:35 before or after the summer time (DST) to normal time changeover?")

                // Depending on what the datetime value is used for, sometimes they are only allowed to change into one direction or almost always stay close to the original value
                // We accomodate this with the TimeModificationAssumption parameter
                var firstHour = naiveNewUTC;
                var secondHour = adjustedNewUTC;
                var heuristicsUTC = ReturnMostLikelyDateTime(firstHour, secondHour, oldValueUTC, timeModificationAssumption);

                // As default behaviour we fall back to SimpleTimeZone's standard behaviour and assume the naive UTC value to be correct
                // ASSUMPTION 5: In any other case, assume the datetime was meant to be falling in the first hour, i.e. Daylight Savings Time in this case
                return heuristicsUTC ?? naiveNewUTC;
            }
        }
    }
}
