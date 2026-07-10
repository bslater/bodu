// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Samples.WorkingDays.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.WorkingDays;

/// <summary>
/// Entry point for the working-days sample: the <c>Bodu.Extensions</c> date arithmetic that turns a
/// notable-date service into business-day answers — payroll, settlement, SLA, and fiscal questions.
/// One service instance is shared by every scenario; the extensions take it as a parameter.
/// </summary>
public static class Program
{
    /// <summary>
    /// Creates the shared AU service and runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.WorkingDays");
        Console.WriteLine("===============================================");
        Console.WriteLine();

        // The extensions work over any INotableDateService - a data pack, a custom-authored
        // document, or a DI-registered service. Here: Australia from the embedded pack.
        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        WorkingDayChecks.Run(service);
        PaymentScheduling.Run(service);
        RangeCounting.Run(service);
        FiscalAndWeekPatterns.Run(service);

        Console.WriteLine("Done.");
    }
}
