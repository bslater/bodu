// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.ServiceHosting.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.ServiceHosting;

/// <summary>
/// Entry point for the service-hosting sample: registering the notable-date service in a dependency
/// injection container — the simple singleton form and the reloadable form whose data can be swapped
/// at run time without restarting the host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.ServiceHosting");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        BasicRegistration.Run();
        KeyedRegistration.Run();
        ReloadableResource.Run();

        Console.WriteLine("Done.");
    }
}
