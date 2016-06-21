using System;
using System.Globalization;
using System.Threading;

namespace DstExperiment.Extension
{

    public enum WeekOfMonth
    {
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
        Last = 5,
    }

    public static class TimeZoneInfoExtensions
    {

        /* Taken from MSDN: https://msdn.microsoft.com/en-us/library/system.timezoneinfo.adjustmentrule(v=vs.110).aspx */
        public static void WriteAdjustmentRules(this TimeZoneInfo timeZone)
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            string[] monthNames = culture.DateTimeFormat.MonthNames;

            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(timeZone.StandardName);
            Console.ForegroundColor = origColor;
            TimeZoneInfo.AdjustmentRule[] adjustments = timeZone.GetAdjustmentRules();
            // Display message for time zones with no adjustments
            if (adjustments.Length == 0)
            {
                Console.WriteLine(" has no adjustment rules");
            }
            else
            {
                // Handle time zones with 1 or 2+ adjustments differently
                bool showCount = false;
                int ctr = 0;
                string spacer = "";

                Console.WriteLine(" Adjustment rules");
                if (adjustments.Length > 1)
                {
                    showCount = true;
                    spacer = "   ";
                }
                // Iterate adjustment rules
                foreach (TimeZoneInfo.AdjustmentRule adjustment in adjustments)
                {
                    if (showCount)
                    {
                        Console.WriteLine("   Adjustment rule #{0}", ctr + 1);
                        ctr++;
                    }
                    // Display general adjustment information
                    Console.WriteLine("{0}   Start Date:   {1:D}", spacer, adjustment.DateStart);
                    Console.WriteLine("{0}   End Date:     {1:D}", spacer, adjustment.DateEnd);
                    Console.Write(    "{0}   Time Change:  ", spacer);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(                     "{0}:{1:00} hours", adjustment.DaylightDelta.Hours, adjustment.DaylightDelta.Minutes);
                    Console.ForegroundColor = origColor;
                    // Get transition start information
                    TimeZoneInfo.TransitionTime transitionStart = adjustment.DaylightTransitionStart;
                    Console.Write(    "{0}   Annual Start: ", spacer);
                    Console.ForegroundColor = ConsoleColor.White;
                    if (transitionStart.IsFixedDateRule)
                    {
                        Console.WriteLine("On {0} {1} at {2:t}",
                                          monthNames[transitionStart.Month - 1],
                                          transitionStart.Day,
                                          transitionStart.TimeOfDay);
                    }
                    else
                    {
                        Console.WriteLine("The {0} {1} of {2} at {3:t}",
                                          ((WeekOfMonth)transitionStart.Week).ToString(),
                                          transitionStart.DayOfWeek.ToString(),
                                          monthNames[transitionStart.Month - 1],
                                          transitionStart.TimeOfDay);
                    }
                    Console.ForegroundColor = origColor;
                    // Get transition end information
                    TimeZoneInfo.TransitionTime transitionEnd = adjustment.DaylightTransitionEnd;
                    Console.Write(    "{0}   Annual End:   ", spacer);
                    Console.ForegroundColor = ConsoleColor.White;
                    if (transitionEnd.IsFixedDateRule)
                    {
                        Console.WriteLine("On {0} {1} at {2:t}",
                                          monthNames[transitionEnd.Month - 1],
                                          transitionEnd.Day,
                                          transitionEnd.TimeOfDay);
                    }
                    else
                    {
                        Console.WriteLine("The {0} {1} of {2} at {3:t}",
                                          ((WeekOfMonth)transitionEnd.Week).ToString(),
                                          transitionEnd.DayOfWeek.ToString(),
                                          monthNames[transitionEnd.Month - 1],
                                          transitionEnd.TimeOfDay);
                    }
                    Console.ForegroundColor = origColor;
                }
            }
            Console.WriteLine();
        }

        /* Taken from a private static function in original BCL source code http://referencesource.microsoft.com/#mscorlib/system/timezoneinfo.cs,8605eb2eb5309ae8 */
        public static DaylightTime GetDaylightTime(this TimeZoneInfo.AdjustmentRule rule, Int32 year)
        {
            TimeSpan delta = rule.DaylightDelta;
            DateTime startTime = rule.DaylightTransitionStart.TransitionTimeToDateTime(year);
            DateTime endTime = rule.DaylightTransitionEnd.TransitionTimeToDateTime(year);
            return new DaylightTime(startTime, endTime, delta);
        }

        /* Taken from a private static function in original BCL source code http://referencesource.microsoft.com/#mscorlib/system/timezoneinfo.cs,8605eb2eb5309ae8 */
        public static DateTime TransitionTimeToDateTime(this TimeZoneInfo.TransitionTime transitionTime, Int32 year)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                // create a DateTime from the passed in year and the properties on the transitionTime

                // if the day is out of range for the month then use the last day of the month
                Int32 day = DateTime.DaysInMonth(year, transitionTime.Month);

                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    //
                    // Get the (transitionTime.Week)th Sunday.
                    //
                    value = new DateTime(year, transitionTime.Month, 1,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }
                    delta += 7 * (transitionTime.Week - 1);

                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    //
                    // If TransitionWeek is greater than 4, we will get the last week.
                    //
                    Int32 daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    // This is the day of week for the last day of the month.
                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0)
                    {
                        value = value.AddDays(-delta);
                    }
                }
            }
            return value;
        }
    }
}
