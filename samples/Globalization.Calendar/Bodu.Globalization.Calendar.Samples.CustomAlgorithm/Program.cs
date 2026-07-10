// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm;

/// <summary>
/// Entry point for the custom-algorithm sample: extending the date-calculation vocabulary with your
/// own <c>INotableDateAlgorithm</c>, registered by key and referenced declaratively from rules — plus
/// the delegate shorthand for one-off calculations. The companion test project proves the sample's
/// calendar against the shared data-pack contract base.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.CustomAlgorithm");
        Console.WriteLine("===================================================");
        Console.WriteLine();

        RegistryRegistration.Run();
        DelegateAlgorithms.Run();

        Console.WriteLine("Done.");
    }
}
