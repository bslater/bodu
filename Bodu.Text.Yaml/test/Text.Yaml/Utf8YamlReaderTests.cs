// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the forward-only token stream produced by <see cref="Utf8YamlReader" />.
/// </summary>
[TestClass]
public partial class Utf8YamlReaderTests
{
    /// <summary>Verifies that a scalar document root emits a single scalar token.</summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Read_WhenScalarRoot_ShouldEmitSingleToken()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("42"));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(YamlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(42L, reader.GetInt64());
        Assert.IsFalse(reader.Read());
    }
}
