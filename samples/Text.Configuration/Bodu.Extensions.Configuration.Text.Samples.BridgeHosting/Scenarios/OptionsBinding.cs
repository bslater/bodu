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
        Console.WriteLine("--- AddConfigurationOptions<T>: TOML -> IOptions<T> via DI ---");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTomlFile(Path.Combine(AppContext.BaseDirectory, "Data", "settings.toml"))
            .Build();

        var services = new ServiceCollection();
        services.AddConfigurationOptions<ServerOptions>(configuration, "server");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ServerOptions>>().Value;

        Console.WriteLine($"ServerOptions: {options.Host}:{options.Port} (tls: {options.Tls})");

        Console.WriteLine();
    }
}
