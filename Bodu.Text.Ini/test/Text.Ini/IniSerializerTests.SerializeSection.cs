// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.SerializeSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains the <see cref="IniSerializer.SerializeSection{TSection}(string, TSection, IIniSectionFactory{TSection}, IniSerializerOptions?)" />
/// backbone tests for the reflection-free section-factory overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a section value serializes through the factory to a section header followed by its entries.
    /// </summary>
    [TestMethod]
    public void SerializeSection_WhenNamedSection_ShouldWriteHeaderAndEntries()
    {
        var section = new DatabaseSection { Host = "db.example.com", Port = 5432 };

        string text = IniSerializer.SerializeSection("database", section, new DatabaseSectionFactory());

        Assert.AreEqual("[database]\nHost=db.example.com\nPort=5432\n", text);
    }

    /// <summary>
    /// Verifies that an empty section name writes the entries as global keys with no section header.
    /// </summary>
    [TestMethod]
    public void SerializeSection_WhenEmptySectionName_ShouldWriteGlobalEntries()
    {
        var section = new DatabaseSection { Host = "db.example.com", Port = 5432 };

        string text = IniSerializer.SerializeSection(string.Empty, section, new DatabaseSectionFactory());

        Assert.AreEqual("Host=db.example.com\nPort=5432\n", text);
    }

    /// <summary>
    /// Verifies that the factory overload throws <see cref="ArgumentNullException" /> for a <see langword="null" />
    /// section name, value, or factory.
    /// </summary>
    [TestMethod]
    public void SerializeSection_WhenArgumentsNull_ShouldThrowArgumentNullException()
    {
        var section = new DatabaseSection { Host = "h", Port = 1 };

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.SerializeSection(null!, section, new DatabaseSectionFactory());
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.SerializeSection("database", (DatabaseSection)null!, new DatabaseSectionFactory());
        });

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = IniSerializer.SerializeSection("database", section, (IIniSectionFactory<DatabaseSection>)null!);
        });
    }

    /// <summary>
    /// Verifies that a value the factory supplies that INI cannot hold throws <see cref="IniSerializationException" />
    /// naming the section and the key, with the writer's <see cref="ArgumentException" /> as the inner exception.
    /// </summary>
    [TestMethod]
    public void SerializeSection_WhenFactoryValueCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var section = new DatabaseSection { Host = "db\nexample", Port = 1 };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.SerializeSection("database", section, new DatabaseSectionFactory());
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'database'", ex.Message);
        Assert.Contains("'Host'", ex.Message);
    }

    /// <summary>
    /// Verifies that a section name INI would read back as something else throws
    /// <see cref="IniSerializationException" /> naming the section, with the writer's <see cref="ArgumentException" />
    /// as the inner exception, from the buffer-writer overload as well.
    /// </summary>
    [TestMethod]
    public void SerializeSection_WhenSectionNameCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var section = new DatabaseSection { Host = "db.example.com", Port = 1 };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            IniSerializer.SerializeSection(new ArrayBufferWriter<byte>(), "database ", section, new DatabaseSectionFactory());
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'database '", ex.Message);
    }
}
