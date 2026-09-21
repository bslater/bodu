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
        SampleConsole.Scenario(
            "AddTextConfigurationFile - a resolved cascade as an IConfiguration source",
            what: "Builds configuration twice from the same .boduconfig file, once for a development target path "
                + "and once for a production one, and reads the same two keys from each.",
            why: "A .boduconfig file is not a flat key/value document - a value only means something once you "
                + "name the path it applies to. IConfiguration has no notion of that, so the bridge resolves the "
                + "cascade when the source loads and publishes the resulting view as ordinary keys. The "
                + "consequence is worth being explicit about: the target path is fixed at build time, so two "
                + "target paths need two builds rather than two lookups. In a host that is not a limitation, "
                + "since the environment is known before configuration is built - but it is why the target path "
                + "is a source parameter rather than an argument to the key lookup.",
            expect: "Two builds of one file give different values for logging:level and the same value for "
                + "app:name, because only the production section overrides the former. The dotted keys in the "
                + "file arrive as the colon-separated keys every other provider uses, so a consumer binding "
                + "these cannot tell a cascade was involved.");

        // The file cascades: [*] defaults, [production/**] overrides logging.level.
        foreach (var target in new[] { "dev/web", "production/web" })
        {
            // The cascade is fixed when the source loads, so each targetPath needs its own build.
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddTextConfigurationFile("Data/settings.boduconfig", targetPath: target)
                .Build();

            Console.WriteLine($"  targetPath '{target,-14}': app:name = {configuration["app:name"]}, logging:level = {configuration["logging:level"]}");
        }

        Console.WriteLine("  (same file, same keys, different values - the production section overrides logging:level, and the cascade is resolved at load)");

        Console.WriteLine();
    }
}
