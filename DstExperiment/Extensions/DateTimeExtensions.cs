using MichaelBrumm.Globalization;
using MichaelBrumm.Win32;
using System;
using System.Linq;

namespace DstExperiment.Extensions
{
    public enum TimeModificationAssumption
    {
        None = 0,
        ToThePast = 1,
        ToTheFuture = 2,
        SmallToThePast = 3,
        SmallToTheFuture = 4,
        SmallestAdjustment = 5
    }

    public static class DateTimeExtensions
    {
        private static DateTime? ReturnMostLikelyDateTime(DateTime firstHour, DateTime secondHour, DateTime oldValueUTC, TimeModificationAssumption timeModificationAssumption)
        {
            switch (timeModificationAssumption)
            {
                case TimeModificationAssumption.ToThePast:
                case TimeModificationAssumption.SmallToThePast:
                    // ASSUMPTION 3a: If the (1st hour) is smaller than the old value, but (2nd hour) is not, assume that the (1st hour) was meant
                    if (firstHour < oldValueUTC && secondHour > oldValueUTC)
                        return firstHour;
                    break; // and check further
                case TimeModificationAssumption.ToTheFuture:
                case TimeModificationAssumption.SmallToTheFuture:
                    // ASSUMPTION 3b: If the (2nd hour) is higher than the old value, but the (1st hour) is not, assume that the (2nd hour) was meant
                    if (secondHour > oldValueUTC && firstHour < oldValueUTC)
                        return secondHour;
                    break; // and check further
                case TimeModificationAssumption.SmallestAdjustment:
                    // ASSUMPTION 3c: If the (1st hour) is closer to the old value than the (2nd hour), assume it was meant
                    if (Math.Abs((oldValueUTC - firstHour).TotalMilliseconds) < Math.Abs((oldValueUTC - secondHour).TotalMilliseconds))
                        return firstHour;
                    // ASSUMPTION 4c: If the (2nd hour) is closer to the old value than the (1st hour), assume it was meant
                    else
                        return secondHour;
                    break;
                default:
                    break; // and check further
            }
            switch (timeModificationAssumption)
            {
                case TimeModificationAssumption.SmallToThePast:
                    // ASSUMPTION 4a: If both values are in the past, we assume that the (2nd hour) was meant (i.e. keep closest to the original value)
                    return secondHour;
                case TimeModificationAssumption.SmallToTheFuture:
                    // ASSUMPTION 4a: If both values are in the future, we assume that the (1st hour) was meant (i.e. keep closest to the original value)
                    return firstHour;
                default:
                    break; // and fall back to default behaviour
            }
            return null;
        }

        public static DateTime ToMostProbableUniversal(this DateTime newValueLocal, DateTime oldValueUTC, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None, TimeZoneInfo timezone = null)
        {
            // Check for invalid NULL parameters
            if (oldValueUTC == null)
                throw new ArgumentNullException(nameof(oldValueUTC));

            // Use the system's local TimeZoneInfo by default (as BCL's ToUniversal does)
            if (timezone == null)
                timezone = TimeZoneInfo.Local;

            // Check whether the supplied DateTime objects are not of the wrong kind
            // Preferably one wouldn't use DateTimeKind.Unspecified either, but we mimic the BCL here
            if (newValueLocal.Kind == DateTimeKind.Utc)
                throw new ArgumentException("Method can not be run on a DateTime object of kind DateTimeKind.Utc");
            if (oldValueUTC.Kind == DateTimeKind.Local)
                throw new ArgumentException("Argument can not be of kind DateTimeKind.Local", nameof(oldValueUTC));

            // If the local DateTime is not ambiguous, return the corresponding UTC value
            if (!timezone.IsAmbiguousTime(newValueLocal))
                return TimeZoneInfo.ConvertTimeToUtc(newValueLocal, timezone);

            // We have an ambiguous local DateTime...
            {
                // Find the DST rule that is/was active around the time of the newly supplied local DateTime
                var activeDstRule = timezone.GetAdjustmentRules().Last(rule => rule.DateStart <= newValueLocal && rule.DateEnd >= newValueLocal);

                // Although activeDstRule should not be null, as we have determined already that we have an ambiguous local DateTime, just make sure...
                if (activeDstRule == null)
                    return TimeZoneInfo.ConvertTimeToUtc(newValueLocal, timezone); // Or should we throw an Exception instead?

                //  Convert the local DateTime to UTC according to .Net BCL's naive implementation
                //  ( => Always assume the datetime was meant to  be fall in Standard Time, i.e. the second hour)
                var naiveNewUTC = TimeZoneInfo.ConvertTimeToUtc(newValueLocal, timezone);

                // ASSUMPTION 1: If the new naive UTC value matches the old UTC value, we assume this is correct and the datetime (falling in the 2nd hour) wasn't changed
                if (naiveNewUTC == oldValueUTC)
                    return naiveNewUTC;

                // Get the possible offsets from UTC (which we can do with TimeZoneInfo contrary to SimpleTimeZone)
                var possibleOffsetsFromUtc = timezone.GetAmbiguousTimeOffsets(newValueLocal);
                var matchingUtcOffset = possibleOffsetsFromUtc.FirstOrDefault(offset => (newValueLocal - offset) == oldValueUTC);

                // ASSUMPTION 2a: If we have a UTC datetime after substracting the possible offsets, matching the old UTC, we assume this is correct and the datetime wasn't changed
                if (matchingUtcOffset.Ticks != 0)
                    return (newValueLocal - matchingUtcOffset);

                /*
                 * Part 2b is a fallback that mimics the algorithm for SimpleTimeZone, but shouldn't give any new results, ever.
                 */

                // Get the DST offset from the current DST rule
                var dstOffset = activeDstRule.DaylightDelta;
                // Get the UTC datetime for the new local DateTime, but assuming the datetime was meant to fall in Daylight Saving Time, i.e. the first hour
                var adjustedNewUTC = naiveNewUTC - dstOffset;

                // ASSUMPTION 2b: If this adjusted UTC value matches the old UTC value, we assume this is corrent and the datime (falling in the 1st hour) wasn't changed
                if (adjustedNewUTC == oldValueUTC)
                    return adjustedNewUTC;

                // We've now matched any unchanged values. From here on we're certain that the new value is changed from the old value.
                // In that case all we can do is add in our own heuristics, but to really know which correct UTC value was meant, the UI should have asked the user
                // When filling in the local datetime manually in the first place (e.g. "Was this 2:14:35 before or after the summer time (DST) to normal time changeover?")

                // Depending on what the datetime value is used for, sometimes they are only allowed to change into one direction or almost always stay close to the original value
                // We accomodate this with the TimeModificationAssumption parameter
                var firstHour = adjustedNewUTC;
                var secondHour = naiveNewUTC;
                var heuristicsUTC = ReturnMostLikelyDateTime(firstHour, secondHour, oldValueUTC, timeModificationAssumption);

                // As default behaviour we fall back to .Net's standard behaviour and assume the naive UTC value to be correct
                // ASSUMPTION 5: In any other case, assume the datetime was meant to  be fall in Standard Time, i.e. the second hour
                return heuristicsUTC ?? naiveNewUTC;
            }
        }

        public static DateTime ToMostProbableUniversal(this DateTime newValueLocal, DateTime oldValueUTC, TimeModificationAssumption timeModificationAssumption = TimeModificationAssumption.None, SimpleTimeZone timezone = null)
        {
            // Check for invalid NULL parameters
            if (oldValueUTC == null)
                throw new ArgumentNullException(nameof(oldValueUTC));

            // Use the system's local SimpleTimeZone by default (as BCL's ToUniversal does)
            if (timezone == null)
                timezone = TimeZones.GetTimeZones().First(tz => tz.StandardName == TimeZone.CurrentTimeZone.StandardName);

            // Check whether the supplied DateTime objects are not of the wrong kind
            // Preferably one wouldn't use DateTimeKind.Unspecified either, but we mimic the BCL here
            if (newValueLocal.Kind == DateTimeKind.Utc)
                throw new ArgumentException("Method can not be run on a DateTime object of kind DateTimeKind.Utc");
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

                //  Convert the local DateTime to UCT according to what MichealBrumm.Globalization.SimpleTimeZone's naive implementation seems to do:
                //  ( => Always assume the datetime was meant to  be fall in the first hour , i.e. Daylight Savings Time in this case)
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
