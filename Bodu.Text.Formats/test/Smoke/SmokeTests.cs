// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Text.Delimited;
using Bodu.Text.DotEnv;
using Bodu.Text.Ini;

namespace Bodu.Smoke;

/// <summary>
/// Smoke tests for the <c>Bodu.Text.*</c> format namespaces. Each test exercises one happy-path on a primary public
/// type so that the smoke-tier build catches catastrophic breakage in any of the format's load-bearing surfaces.
/// </summary>
[TestClass]
public sealed class SmokeTests
{
    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="Delimited.Format(DelimitedDocument)" /> round-trip a simple CSV document — headers and all field
    /// values survive unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Delimited_ParseFormat_ShouldRoundTripSimpleDocument()
    {
        const string source = "name,age,city\nAlice,30,Paris\nBob,25,London";

        DelimitedDocument original = Delimited.Parse(source);
        var formatted = Delimited.Format(original);
        DelimitedDocument roundTripped = Delimited.Parse(formatted);

        Assert.AreEqual(2, roundTripped.Rows.Count);
        Assert.AreEqual("Alice", roundTripped.Rows[0]["name"]);
        Assert.AreEqual("25", roundTripped.Rows[1]["age"]);
        Assert.AreEqual("London", roundTripped.Rows[1]["city"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="DotEnv.Format(DotEnvDocument)" /> round-trip a simple DotEnv document — all keys and values
    /// survive unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void DotEnv_ParseFormat_ShouldRoundTripSimpleDocument()
    {
        const string source = "HOST=localhost\nPORT=8080\nDEBUG=True";

        DotEnvDocument original = DotEnv.Parse(source);
        var formatted = DotEnv.Format(original);
        DotEnvDocument roundTripped = DotEnv.Parse(formatted);

        Assert.AreEqual("localhost", roundTripped["HOST"]);
        Assert.AreEqual("8080", roundTripped["PORT"]);
        Assert.AreEqual("True", roundTripped["DEBUG"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> and <see cref="Ini.Format(IniDocument)" />
    /// round-trip a simple INI document — all sections, keys, and values survive unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Ini_ParseFormat_ShouldRoundTripSimpleDocument()
    {
        const string source = "global=g\n[server]\nhost=localhost\nport=8080";

        IniDocument original = Ini.Parse(source);
        var formatted = Ini.Format(original);
        IniDocument roundTripped = Ini.Parse(formatted);

        Assert.AreEqual("g", roundTripped.GlobalSection["global"]);
        IniSection? server = roundTripped.GetSection("server");
        Assert.IsNotNull(server);
        Assert.AreEqual("localhost", server["host"]);
        Assert.AreEqual("8080", server["port"]);
    }
}
