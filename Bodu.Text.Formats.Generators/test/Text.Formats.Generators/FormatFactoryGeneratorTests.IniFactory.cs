// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.IniFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Contains the end-to-end tests for the generated <c>GeneratedServerSection.IniFactory</c>.
/// </summary>
public partial class FormatFactoryGeneratorTests
{
    /// <summary>
    /// Verifies that the generated factory resolves keys in declaration order, honouring <c>[PropertyName]</c>.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenGenerated_ShouldExposeResolvedKeys()
    {
        CollectionAssert.AreEqual(
            new[] { "Host", "Port", "use_tls", "Timeout" },
            GeneratedServerSection.IniFactory.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a section serializes through the generated factory to canonical INI bytes.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenSerialized_ShouldWriteCanonicalSection()
    {
        var section = new GeneratedServerSection { Host = "db.example.com", Port = 5432, UseTls = true, Timeout = null };

        string text = IniSerializer.SerializeSection("server", section, GeneratedServerSection.IniFactory);

        Assert.AreEqual("[server]\nHost=db.example.com\nPort=5432\nuse_tls=true\nTimeout=\n", text);
    }

    /// <summary>
    /// Verifies that a section round-trips through the generated factory, preserving the nullable key.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenRoundTripped_ShouldPreserveValues()
    {
        var section = new GeneratedServerSection
        {
            Host = "db.example.com",
            Port = 5432,
            UseTls = true,
            Timeout = TimeSpan.FromSeconds(90),
        };

        string text = IniSerializer.SerializeSection("server", section, GeneratedServerSection.IniFactory);
        GeneratedServerSection restored = IniSerializer.DeserializeSection(text, "server", GeneratedServerSection.IniFactory);

        Assert.AreEqual("db.example.com", restored.Host);
        Assert.AreEqual(5432, restored.Port);
        Assert.IsTrue(restored.UseTls);
        Assert.AreEqual(TimeSpan.FromSeconds(90), restored.Timeout);
    }

    /// <summary>
    /// Verifies that an empty value binds a nullable key to <see langword="null" /> and that key matching falls back
    /// to case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenValueEmptyOrKeyCaseDiffers_ShouldBindLeniently()
    {
        GeneratedServerSection restored = IniSerializer.DeserializeSection(
            "[server]\nHOST=example.org\nport=80\nuse_tls=false\nTimeout=\n", "server", GeneratedServerSection.IniFactory);

        Assert.AreEqual("example.org", restored.Host);
        Assert.AreEqual(80, restored.Port);
        Assert.IsFalse(restored.UseTls);
        Assert.IsNull(restored.Timeout);
    }
}
