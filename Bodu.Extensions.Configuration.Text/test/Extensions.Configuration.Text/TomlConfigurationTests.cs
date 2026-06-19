// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.IO;
using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Behavioural tests for the read-only TOML configuration source — flattening into the colon-delimited key model,
/// file and stream loading, and the read-only contract.
/// </summary>
[TestClass]
public sealed partial class TomlConfigurationTests
{
    private const string Sample =
        "title = \"app\"\n" +
        "\n" +
        "[server]\n" +
        "host = \"localhost\"\n" +
        "port = 8080\n" +
        "enabled = true\n" +
        "\n" +
        "[logging]\n" +
        "levels = [\"info\", \"warn\"]\n";

    private static IConfigurationRoot BuildFromStream(string toml)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        return new ConfigurationBuilder().AddTomlStream(stream).Build();
    }
}
