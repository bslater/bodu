// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Behavioral tests for the read-only TOML configuration source - flattening into the colon-delimited key model,
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

    /// <summary>
    /// Gets rows pinning the invariant-culture configuration text of every scalar TOML value kind: a one-line document
    /// assigning the value to <c>v</c>, and the text the configuration exposes for <c>v</c>.
    /// </summary>
    /// <value>Rows of TOML document → expected configuration value.</value>
    public static IEnumerable<object[]> ScalarKats =>
        new ValidKat<string, string>[]
        {
            new("String", "v = \"text\"", "text"),
            new("Integer", "v = 42", "42"),
            new("NegativeInteger", "v = -7", "-7"),
            new("Float", "v = 1.5", "1.5"),
            new("FloatWithExponent", "v = -2.5e2", "-250"),
            new("BooleanTrue", "v = true", "true"),
            new("BooleanFalse", "v = false", "false"),
            new("OffsetDateTime", "v = 1979-05-27T00:32:00-07:00", "1979-05-27T00:32:00-07:00"),
            new("OffsetDateTimeWithFraction", "v = 1979-05-27T00:32:00.999999-07:00", "1979-05-27T00:32:00.999999-07:00"),
            new("OffsetDateTimeUtc", "v = 1979-05-27T07:32:00Z", "1979-05-27T07:32:00+00:00"),
            new("LocalDateTime", "v = 1979-05-27T07:32:00", "1979-05-27T07:32:00"),
            new("LocalDateTimeWithFraction", "v = 1979-05-27T07:32:00.5", "1979-05-27T07:32:00.5"),
            new("LocalDate", "v = 1979-05-27", "1979-05-27"),
            new("LocalTime", "v = 07:32:00", "07:32:00"),
            new("LocalTimeWithFraction", "v = 07:32:00.25", "07:32:00.25"),
        }.Select(kat => new object[] { kat });

    private static IConfigurationRoot BuildFromStream(string toml)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        return new ConfigurationBuilder().AddTomlStream(stream).Build();
    }
}
