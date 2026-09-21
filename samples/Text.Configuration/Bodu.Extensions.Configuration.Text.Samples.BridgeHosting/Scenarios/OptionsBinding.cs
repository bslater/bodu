// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionsBinding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Samples.Extensions.Configuration.Text.BridgeHosting.Scenarios;

/// <summary>
/// Demonstrates the last hop of the bridge: <c>AddConfigurationOptions&lt;TOptions&gt;</c> binds
/// a configuration section to a POCO and registers it with dependency injection, so a service
/// consumes <see cref="IOptions{TOptions}" /> with no knowledge that the values started life in
/// a TOML file.
/// </summary>
public static class OptionsBinding
{
    /// <summary>
    /// The strongly typed server settings bound from the <c>[server]</c> table.
    /// </summary>
    public sealed class ServerOptions
    {
        /// <summary>Gets or sets the bind host.</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>Gets or sets the listener port.</summary>
        public int Port { get; set; }

        /// <summary>Gets or sets whether TLS is enabled.</summary>
        public bool Tls { get; set; }
    }

    /// <summary>
    /// Binds <c>[server]</c> from <c>Data/settings.toml</c> into <c>IOptions&lt;ServerOptions&gt;</c>.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddConfigurationOptions<T> - from a TOML table to an injected options type",
            what: "Builds configuration from the TOML file, registers the [server] table as a strongly typed "
                + "options class, resolves it from the service provider, and prints the bound values.",
            why: "This is where the bridge stops being visible, which is the goal. A service that takes "
                + "IOptions<ServerOptions> depends on a shape it owns, not on a configuration key, a file path "
                + "or a format - so the file can move to JSON, to environment variables, or to a secret store "
                + "without touching it. Binding also does the type conversion in one place: the wire values are "
                + "all text, and Port arriving as an int rather than a string is the difference between parsing "
                + "once at startup and parsing at every call site, differently.",
            expect: "The options instance carries values from the TOML table with their types already resolved - "
                + "an int port and a bool flag, not strings. Nothing in ServerOptions or in the code consuming it "
                + "refers to TOML, which is the entire point of registering it this way.");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTomlFile(Path.Combine(AppContext.BaseDirectory, "Data", "settings.toml"))
            .Build();

        var services = new ServiceCollection();
        services.AddConfigurationOptions<ServerOptions>(configuration, "server");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ServerOptions>>().Value;

        Console.WriteLine($"  ServerOptions: {options.Host}:{options.Port} (tls: {options.Tls})"
            + "  (bound from the [server] table with types resolved once at startup - the consuming code never mentions TOML)");

        Console.WriteLine();
    }
}
