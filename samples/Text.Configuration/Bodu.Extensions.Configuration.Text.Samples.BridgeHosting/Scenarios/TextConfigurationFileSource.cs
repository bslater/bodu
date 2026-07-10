// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationFileSource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Samples.Extensions.Configuration.Text.BridgeHosting.Scenarios;

/// <summary>
/// Demonstrates <c>AddTextConfigurationFile</c>: a <c>.boduconfig</c> file becomes an ordinary
/// <see cref="IConfiguration" /> source. The cascade is resolved for the supplied
/// <c>targetPath</c> at load time, and the resolved view's dotted keys surface as the standard
/// colon-separated configuration keys.
/// </summary>
public static class TextConfigurationFileSource
{
    /// <summary>
    /// Builds configuration from <c>Data/settings.boduconfig</c> under two target paths.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AddTextConfigurationFile: .boduconfig -> IConfiguration ---");

        // The file cascades: [*] defaults, [production/**] overrides logging.level.
        foreach (var target in new[] { "dev/web", "production/web" })
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddTextConfigurationFile("Data/settings.boduconfig", targetPath: target)
                .Build();

            Console.WriteLine($"targetPath '{target,-14}': app:name = {configuration["app:name"]}, logging:level = {configuration["logging:level"]}");
        }

        Console.WriteLine();
    }
}
