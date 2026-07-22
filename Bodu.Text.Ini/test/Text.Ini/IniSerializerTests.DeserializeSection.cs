// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.DeserializeSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Contains the <see cref="IniSerializer.DeserializeSection{TSection}(string, string, IIniSectionFactory{TSection}, IniSerializerOptions?)" />
/// backbone tests for the reflection-free section-factory overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a named section binds through the factory to a section instance.
    /// </summary>
    [TestMethod]
    public void DeserializeSection_WhenNamedSection_ShouldBindEntries()
    {
        DatabaseSection section = IniSerializer.DeserializeSection(
            "[database]\nHost = db.example.com\nPort = 5432\n", "database", new DatabaseSectionFactory());

        Assert.AreEqual("db.example.com", section.Host);
        Assert.AreEqual(5432, section.Port);
    }

    /// <summary>
    /// Verifies that an empty section name binds the document's global keys.
    /// </summary>
    [TestMethod]
    public void DeserializeSection_WhenEmptySectionName_ShouldBindGlobalEntries()
    {
        DatabaseSection section = IniSerializer.DeserializeSection(
            "Host = global.example.com\nPort = 80\n\n[database]\nHost = other\n", string.Empty, new DatabaseSectionFactory());

        Assert.AreEqual("global.example.com", section.Host);
        Assert.AreEqual(80, section.Port);
    }

    /// <summary>
    /// Verifies that duplicate sections merge before binding, per the default duplicate-section policy.
    /// </summary>
    [TestMethod]
    public void DeserializeSection_WhenDuplicateSectionsMerge_ShouldBindMergedEntries()
    {
        DatabaseSection section = IniSerializer.DeserializeSection(
            "[database]\nHost = db.example.com\n\n[other]\nx = 1\n\n[database]\nPort = 5432\n", "database", new DatabaseSectionFactory());

        Assert.AreEqual("db.example.com", section.Host);
        Assert.AreEqual(5432, section.Port);
    }

    /// <summary>
    /// Verifies that a missing section throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void DeserializeSection_WhenSectionMissing_ShouldThrowIniSerializationException()
    {
        var exception = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.DeserializeSection("[other]\nx = 1\n", "database", new DatabaseSectionFactory());
        });

        Assert.IsTrue(exception.Message.Contains("database", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the factory overload throws <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// text, section name, or factory.
    /// </summary>
    [TestMethod]
    public void DeserializeSection_WhenArgumentsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.DeserializeSection((string)null!, "database", new DatabaseSectionFactory());
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.DeserializeSection("[database]\n", null!, new DatabaseSectionFactory());
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.DeserializeSection("[database]\n", "database", (IIniSectionFactory<DatabaseSection>)null!);
        });
    }
}
