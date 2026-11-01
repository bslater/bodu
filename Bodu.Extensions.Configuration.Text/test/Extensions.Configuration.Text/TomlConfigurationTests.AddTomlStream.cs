// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.AddTomlStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Test.Kat;
using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TomlConfigurationTests
{
    /// <summary>
    /// Verifies that every scalar TOML value kind - string, integer, float, boolean, offset and local date-times, local
    /// date and local time - flattens to its invariant-culture configuration text.
    /// </summary>
    /// <param name="kat">The row: a one-line TOML document and the configuration text expected for its value.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void AddTomlStream_WhenScalarKind_ShouldFlattenToInvariantText(ValidKat<string, string> kat)
    {
        IConfigurationRoot config = BuildFromStream(kat.Input + "\n");

        Assert.AreEqual(kat.Expected, config["v"]);
    }

    /// <summary>
    /// Verifies that two TOML keys differing only in case - distinct keys in TOML - collide in the case-insensitive
    /// configuration key model and throw <see cref="FormatException" /> naming the colliding key, at the root and
    /// within a table alike.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="toml">The document whose keys collide after flattening.</param>
    /// <param name="collidingKey">The flattened key the second definition collides on.</param>
    [TestMethod]
    [DataRow("Root", "a = 1\nA = 2\n", "A")]
    [DataRow("Nested", "[server]\nport = 1\n[SERVER]\nPort = 2\n", "SERVER:Port")]
    public void AddTomlStream_WhenKeysCollideCaseInsensitively_ShouldThrowFormatException(string testName, string toml, string collidingKey)
    {
        _ = testName;

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = BuildFromStream(toml);
        });

        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.Format_Invalid_TomlDuplicateConfigurationKey, collidingKey),
            ex.Message);
    }

    /// <summary>
    /// Verifies that loading a TOML stream flattens nested tables into colon-delimited keys with values
    /// stringified.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenLoaded_ShouldFlattenNestedKeys()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        Assert.AreEqual("app", config["title"]);
        Assert.AreEqual("localhost", config["server:host"]);
        Assert.AreEqual("8080", config["server:port"]);
        Assert.AreEqual("true", config["server:enabled"]);
    }

    /// <summary>
    /// Verifies that a TOML array is flattened into zero-based indexed colon-delimited keys.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenArray_ShouldFlattenToIndexedKeys()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        Assert.AreEqual("info", config["logging:levels:0"]);
        Assert.AreEqual("warn", config["logging:levels:1"]);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> over loaded TOML exposes the table's leaf
    /// keys as ordered children.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenGettingSection_ShouldExposeChildren()
    {
        IConfigurationRoot config = BuildFromStream(Sample);

        IConfigurationSection server = config.GetSection("server");
        Assert.AreEqual("localhost", server["host"]);

        string[] childKeys = server.GetChildren().Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "enabled", "host", "port" }, childKeys);
    }

    /// <summary>
    /// Verifies that loading malformed TOML from a stream throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenInvalidToml_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
            new ConfigurationBuilder().AddTomlStream(new MemoryStream(Encoding.UTF8.GetBytes("a = "))).Build());
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder or stream argument throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenNullArguments_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((IConfigurationBuilder)null!).AddTomlStream(new MemoryStream()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigurationBuilder().AddTomlStream(null!));
    }
}
