// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar;

/// <summary>
/// Entry point for the custom-calendar sample: authoring a company-holiday calendar with the fluent
/// <c>NotableDateDocumentBuilder</c>, layering adjustment policies, importing the shared catalogues,
/// and round-tripping the document through XML — the calendar analog of "bring your own data".
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.CustomCalendar");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        AuthoringCompanyHolidays.Run();
        FrequencyBasedSchedules.Run();
        AdjustmentsAndPolicies.Run();
        ImportingCatalogues.Run();
        XmlRoundTrip.Run();

        Console.WriteLine("Done.");
    }
}
