using DstExperiment.Extensions;
using DstExperiment.TimezoneHandlers;
using DstExperiment.TimezoneHandlers.Extensions;
using MichaelBrumm.Win32;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace DstExperiment
{
    static class Program
    {
        static void Main()
        {
            // Set up culture for datetime format in output
            var culture = CultureInfo.GetCultureInfo("en-GB"); // CultureInfo.CurrentCulture
            Thread.CurrentThread.CurrentCulture = culture;

                //Dump a list of all available system timezones:
                //TimeZoneInfo.GetSystemTimeZones().ToList().ForEach(zone => Console.WriteLine("[{0,-30}] {1}", zone.Id, zone.ToString()));

            // Create a list of timezones to show debug output for
            var timezones = new TimeZoneInfo[]
            {
                //TimeZoneInfo.FindSystemTimeZoneById("Cen. Australia Standard Time"),
                //TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
                TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"),
                //TimeZoneInfo.Local
            };

            foreach (var timezone in timezones)
            {

                try
                {
                    // Write out all adjustment rules for the timezone
                    timezone.WriteAdjustmentRules();

                    // Create a list of TimeZoneHandlers to show debug output for
                    var timezoneHandlers = new ITimezoneHandler[]
                    {
                        // just the UTC datetime stamp:
                        new UtcHandler(),
                        // using the BCL's TimeZoneInfo class:
                        new TimeZoneInfoHandler(timezone),
                        // using Michael Brumm's SimpleTimeZone class:
                        new SimpleTimeZoneHandler(TimeZones.GetTimeZones().First(tz => tz.StandardName == timezone.StandardName))  //Get the matching (MichaelBrumm.Globalization.)SimpleTimeZone as well (as this is used by MSM)
                    };                                                                                                             // This breaks on UTC itself as TimeZoneInfo uses "UTC" and SimpleTimeZone "Coordinated Universal Time"

                    // Simulate a summertime to wintertime transition
                    var simulator = new DstTransitionSimulator(timezone, timezoneHandlers)
                    {
                        ModificationAssumption = TimeModificationAssumption.SmallestAdjustment,
                        ManualModificationInMinutes = 0
                    };
                    simulator.SimulateSummerToWinterTransition();
                }
                catch (ArgumentException e)
                {
                    var origColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("WARNING: {0}", e.Message);
                    Console.ForegroundColor = origColor;
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    var origColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: {0}", e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ForegroundColor = origColor;
                    Console.WriteLine();
                }
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit the application...");
                Console.ReadKey();
            }
        }
    }
}
