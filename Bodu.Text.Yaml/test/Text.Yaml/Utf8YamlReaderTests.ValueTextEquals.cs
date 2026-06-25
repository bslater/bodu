// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlReaderTests.ValueTextEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies allocation-free property-name comparison through <see cref="Utf8YamlReader.ValueTextEquals" />.
/// </summary>
public partial class Utf8YamlReaderTests
{
    /// <summary>Verifies that a property name can be compared against UTF-8 text without allocation.</summary>
    [TestMethod]
    public void ValueTextEquals_WhenPropertyName_ShouldMatch()
    {
        var reader = new Utf8YamlReader(Encoding.UTF8.GetBytes("host: localhost\n"));

        Assert.IsTrue(reader.Read()); // StartMapping
        Assert.IsTrue(reader.Read()); // PropertyName
        Assert.AreEqual(YamlTokenType.PropertyName, reader.TokenType);
        Assert.IsTrue(reader.ValueTextEquals("host"u8));
        Assert.IsFalse(reader.ValueTextEquals("port"u8));
    }
}
