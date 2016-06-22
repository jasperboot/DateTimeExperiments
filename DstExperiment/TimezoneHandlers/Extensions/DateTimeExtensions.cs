using System;

namespace DstExperiment.TimezoneHandlers.Extensions
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
            public static DateTime? ReturnMostLikelyDateTime(DateTime firstHour, DateTime secondHour, DateTime oldValueUTC, TimeModificationAssumption timeModificationAssumption)
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

        }
    }
