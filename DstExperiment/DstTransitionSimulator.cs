using DstExperiment.Extensions;
using DstExperiment.TimezoneHandlers.Extensions;
using DstExperiment.TimezoneHandlers;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DstExperiment
{
    public class DstTransitionSimulator
    {
        public DstTransitionSimulator(TimeZoneInfo timezone, ITimezoneHandler[] timezoneHandlers)
        {
            this.Timezone = timezone;
            this.TimezoneHandlers = timezoneHandlers;
        }

        public TimeZoneInfo Timezone { get; private set; }
        public ITimezoneHandler[] TimezoneHandlers { get; private set; }
        public TimeModificationAssumption ModificationAssumption { get; set; }
        public int ManualModificationInMinutes { get; set; }

        public void SimulateSummerToWinterTransition()
        {
            // If no Timezone is passed in, that is an error
            if (this.Timezone == null)
                throw new ArgumentNullException(nameof(this.Timezone));
            if (!this.Timezone.SupportsDaylightSavingTime)
                throw new ArgumentException("Timezone doesn't have any DST rules", nameof(this.Timezone));

            /*
             * Find the summer (DST) to winter (standard) time transition for this year
             */

            // Get the local DateTime for the timezone specified
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, this.Timezone);

            // Find the DST rule that is/was active around the time of the local DateTime
            var activeDstRule = this.Timezone.GetAdjustmentRules().Last(rule => rule.DateStart <= localNow && rule.DateEnd >= localNow);

            // Get the (local) DST end datetime for this (local) year
            var dstEndLocal = activeDstRule.DaylightTransitionEnd.TransitionTimeToDateTime(localNow.Year);

            // Convert this local DST end datetime to UTC
            var dstEndUtc = TimeZoneInfo.ConvertTimeToUtc(dstEndLocal, this.Timezone);
            // Per API we get the DST period in Local Time, after which ToUniversalTime already stuffs up (as it prefers Standard Time over DST when ambigious),
            // hence the next line to get to the real transition moment:
            dstEndUtc -= activeDstRule.DaylightDelta;

            /*
             * Iterate over de times around the DST changeover (summer -> standard time) in 10 minute blocks
             * and write debug output
             */

            WriteTransitionOutputHeaders();
            for (var dateTimeUTC = dstEndUtc.AddMinutes(-135); dateTimeUTC <= dstEndUtc.AddMinutes(140); dateTimeUTC = dateTimeUTC.AddMinutes(10))
            {
                WriteTransitionOutputData(dateTimeUTC, dateTimeUTC < dstEndUtc);
            }

            Console.WriteLine();
        }

        private void WriteTransitionOutputData(DateTime dateTimeUTC, bool isActuallyDST = false)
        {
            var oldColor = Console.ForegroundColor;
            var mightBeAmbiguous = this.TimezoneHandlers.Any(handler => handler.IsAmbiguous(handler.ConvertFromUtcToLocal(dateTimeUTC)));
            if (mightBeAmbiguous)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            int i = 0;
            foreach (var handler in this.TimezoneHandlers)
            {
                if (handler.HandlerType == TimeZoneHandlerType.UtcHandler)
                {
                    var normalColor = Console.ForegroundColor;
                    if (isActuallyDST)
                        Console.ForegroundColor = mightBeAmbiguous ? ConsoleColor.Yellow : ConsoleColor.DarkYellow;
                    else
                        Console.ForegroundColor = mightBeAmbiguous ? ConsoleColor.Green : ConsoleColor.DarkGreen;
                    Console.Write("{0}", dateTimeUTC);
                    Console.ForegroundColor = normalColor;
                }
                else
                {
                    // Get the local datetime for the selected timezone
                    var local = handler.ConvertFromUtcToLocal(dateTimeUTC);

                    // Convert back to UTC (directly)
                    /* !! HERE BE DRAGONS:
                     * For one reason or another TimeZoneInfo can handle this correctly,
                     * BUT: IF AND ONLY IF Timezone.Local is passed in as the timezone.
                     * In no other case this seem to work! Not even when using the same equivalent Timezone, retrieved via its Identifier...
                     */
                    var reUTC = handler.ConvertFromLocalToUtc(local);

                    // Create a manual datetime object, so we 'forget' any interesting info that could be left in the DateTime object
                    // This mimics what happens when we get a local DateTime back from the UI (e.g. with Occurred Date on MSM's request screen).
                    // Optionally we can change this value slightly to see how the algorithms handle changed datetimes
                    var correctManualUTC = dateTimeUTC.AddMinutes(ManualModificationInMinutes);
                    var localChanged = handler.ConvertFromUtcToLocal(correctManualUTC);
                    var manualLocal = new DateTime(localChanged.Year, localChanged.Month, localChanged.Day, localChanged.Hour, localChanged.Minute, localChanged.Second, DateTimeKind.Unspecified);
                    var manualMightBeAmbiguous = this.TimezoneHandlers.Any(h => h.IsAmbiguous(manualLocal));

                    // Now convert this 'manual' local datetime back to UTC
                    var manualUTC = handler.ConvertFromLocalToUtc(manualLocal);

                    // Now convert this 'manual' local datetime back to UTC, this time using a custom algorithm that tries be smarter with unchanged datetimes during the transitional hours
                    var smartManualUTC = handler.ConvertFromLocalToMostProbableUtc(manualLocal, dateTimeUTC, this.ModificationAssumption);

                    // Now output the data to the console
                    ConsoleColor origColor = Console.ForegroundColor;
                    Console.Write("{0:HH:mm} ", local);
                    Console.ForegroundColor = (handler.IsDaylightSavingTime(local) == isActuallyDST) ? (mightBeAmbiguous ? ConsoleColor.Green : ConsoleColor.DarkGreen) : (mightBeAmbiguous ? ConsoleColor.Red : ConsoleColor.DarkRed);
                    Console.Write(" {0}", handler.IsDaylightSavingTime(local) ? "+" : "-");
                    Console.ForegroundColor = origColor;
                    Console.Write(' ');
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(handler.IsAmbiguous(local) ? "!" : " ");
                    Console.ForegroundColor = origColor;
                    Console.Write(' ');
                    Console.Write(" | ");
                    Console.ForegroundColor = (reUTC == dateTimeUTC) ? ConsoleColor.DarkCyan : ConsoleColor.DarkYellow;
                    Console.Write("     {0:HH:mm}", reUTC);
                    Console.ForegroundColor = origColor;
                    Console.Write(" | ");
                    Console.ForegroundColor = manualMightBeAmbiguous ? ConsoleColor.White : origColor;
                    Console.Write(" {0:HH:mm} ", manualLocal);
                    Console.ForegroundColor = (handler.IsDaylightSavingTime(manualLocal) == isActuallyDST) ? (manualMightBeAmbiguous ? ConsoleColor.Green : ConsoleColor.DarkGreen) : (manualMightBeAmbiguous ? ConsoleColor.Red : ConsoleColor.DarkRed);
                    Console.Write(" {0}", handler.IsDaylightSavingTime(manualLocal) ? "+" : "-");
                    Console.ForegroundColor = origColor;
                    Console.Write(' ');
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(handler.IsAmbiguous(manualLocal) ? "!" : " ");
                    Console.ForegroundColor = origColor;
                    Console.Write(' ');
                    Console.Write(" | ");
                    Console.ForegroundColor = (manualUTC == correctManualUTC) ? (manualMightBeAmbiguous ? ConsoleColor.Green : ConsoleColor.DarkGreen) : (manualMightBeAmbiguous ? ConsoleColor.Red : ConsoleColor.DarkRed);
                    Console.Write("  {0:HH:mm}", manualUTC);
                    Console.ForegroundColor = origColor;
                    Console.Write(' ');
                    Console.Write(" | ");
                    Console.ForegroundColor = (smartManualUTC == correctManualUTC) ? (manualMightBeAmbiguous ? ConsoleColor.Green : ConsoleColor.DarkGreen) : (manualMightBeAmbiguous ? ConsoleColor.Red : ConsoleColor.DarkRed);
                    Console.Write(" {0:HH:mm} ", smartManualUTC);
                    Console.ForegroundColor = origColor;
                }
                if (++i < this.TimezoneHandlers.Length)
                    Console.Write(" || ");
            }
            Console.ForegroundColor = oldColor;
            Console.WriteLine();
        }

        private void WriteTransitionOutputHeaders()
        {
            string singleDataHeader = "Local [D/A] | Direct UTC | Manual [D/A] | UTC def. | SmartUT";
            var header = new StringBuilder();
            var dataHeader = new StringBuilder();
            var headerLine = new StringBuilder();
            int i = 0;
            foreach (var handler in this.TimezoneHandlers)
            {
                int headerLength = singleDataHeader.Length;
                if (handler.HandlerType == TimeZoneHandlerType.UtcHandler)
                {
                    headerLength = handler.Description.Length;
                }
                string headerFormatter = String.Format("{{0,-{0}}}", headerLength);
                if (handler.HandlerType == TimeZoneHandlerType.UtcHandler)
                {
                    dataHeader.AppendFormat(headerFormatter, "");
                    headerLine.AppendFormat(headerFormatter, new String('-', headerLength));
                }
                else
                {
                    dataHeader.AppendFormat(headerFormatter, singleDataHeader);
                    headerLine.AppendFormat(headerFormatter, Regex.Replace(singleDataHeader, "[^|]", "-").Replace('|', '+'));
                }
                header.AppendFormat(headerFormatter, handler.Description);
                if (++i < this.TimezoneHandlers.Length)
                {
                    header.Append(" || ");
                    dataHeader.Append(" || ");
                    headerLine.Append("-++-");
                }
            }
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(header);
            Console.ForegroundColor = origColor;
            Console.WriteLine(dataHeader);
            Console.WriteLine(headerLine);
        }

    }
}
