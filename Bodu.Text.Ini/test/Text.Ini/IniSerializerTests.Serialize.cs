// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for the <see cref="IniSerializer.Serialize{T}(T, IniSerializerOptions?)" /> overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a POCO with global scalar members, a section POCO, and a section dictionary serializes to the
    /// expected INI text with globals before sections.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenPocoWithGlobalsAndSections_ShouldEmitExpectedText()
    {
        var config = new ServerConfig
        {
            Name = "app",
            Retries = 3,
            Database = new DatabaseSection { Host = "localhost", Port = 5432 },
            Logging = new Dictionary<string, string> { ["level"] = "info" },
        };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=app\nRetries=3\n[Database]\nHost=localhost\nPort=5432\n[Logging]\nlevel=info\n", text);
    }

    /// <summary>
    /// Verifies that the configured naming policy applies to global keys, section names, and section keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicySnakeCaseLower_ShouldApplyToKeysAndSections()
    {
        var config = new ServerConfig
        {
            Name = "app",
            Database = new DatabaseSection { Host = "x", Port = 1 },
        };
        var options = new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        string text = IniSerializer.Serialize(config, options);

        Assert.AreEqual("name=app\nretries=0\n[database]\nhost=x\nport=1\n", text);
    }

    /// <summary>
    /// Verifies that a nested string-keyed dictionary root serializes each entry as a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedDictionaryRoot_ShouldEmitSections()
    {
        var root = new Dictionary<string, Dictionary<string, string>>
        {
            ["db"] = new() { ["host"] = "x" },
            ["log"] = new() { ["level"] = "warn" },
        };

        string text = IniSerializer.Serialize(root);

        Assert.AreEqual("[db]\nhost=x\n[log]\nlevel=warn\n", text);
    }

    /// <summary>
    /// Verifies that the entry matching <see cref="IniSerializerOptions.GlobalSectionName" /> is emitted as global
    /// keys rather than a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGlobalSectionNameSet_ShouldEmitReservedEntryAsGlobals()
    {
        var root = new Dictionary<string, Dictionary<string, string>>
        {
            ["global"] = new() { ["a"] = "1" },
            ["db"] = new() { ["host"] = "x" },
        };
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        string text = IniSerializer.Serialize(root, options);

        Assert.AreEqual("a=1\n[db]\nhost=x\n", text);
    }

    /// <summary>
    /// Verifies that a null section member is omitted under the default ignore condition.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenSectionMemberNull_ShouldOmitSection()
    {
        var config = new ServerConfig { Name = "app" };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=app\nRetries=0\n", text);
    }

    /// <summary>
    /// Verifies that a scalar root throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarRoot_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize("just a string");
        });
    }

    /// <summary>
    /// Verifies that a member nested beyond INI's two levels throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNestsBeyondTwoLevels_ShouldThrowIniSerializationException()
    {
        var config = new DeepConfig { Outer = new OuterSection { Inner = new DatabaseSection { Host = "x" } } };

        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });
    }

    /// <summary>
    /// Verifies that only a POCO's public members are written, so that a private property is left out.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeHasNonPublicMembers_ShouldWriteOnlyPublicOnes()
    {
        var config = new PrivateMemberConfig { Name = "Unknwon" };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=Unknwon\n", text);
    }

    /// <summary>
    /// Verifies that an integer member holding zero is written under the default options rather than left out.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIntMemberIsZero_ShouldWriteTheKey()
    {
        var config = new PortConfig { Port = 0 };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Port=0\n", text);
    }

    /// <summary>
    /// Verifies that a global member whose value INI cannot hold, a string with a line break, throws
    /// <see cref="IniSerializationException" /> naming the key, with the writer's <see cref="ArgumentException" /> as
    /// the inner exception.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGlobalValueCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var config = new ServerConfig { Name = "first\nsecond" };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            IniSerializer.Serialize(new ArrayBufferWriter<byte>(), config);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'Name'", ex.Message);
    }

    /// <summary>
    /// Verifies that a section member whose value INI cannot hold throws <see cref="IniSerializationException" />
    /// naming the section and the key, with the writer's <see cref="ArgumentException" /> as the inner exception.
    /// </summary>
    /// <param name="host">The value of the section's <c>Host</c> member.</param>
    [TestMethod]
    [DataRow("db\nexample", DisplayName = "line break")]
    [DataRow(" db.example ", DisplayName = "surrounding whitespace")]
    public void Serialize_WhenSectionValueCannotBeWritten_ShouldThrowIniSerializationException(string host)
    {
        var config = new ServerConfig { Name = "app", Database = new DatabaseSection { Host = host, Port = 1 } };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'Database'", ex.Message);
        Assert.Contains("'Host'", ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary key INI would read back as something else, <c>a=b</c>, throws
    /// <see cref="IniSerializationException" /> naming the section and the key, with the writer's
    /// <see cref="ArgumentException" /> as the inner exception.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, Dictionary<string, string>> { ["db"] = new() { ["a=b"] = "c" } };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'db'", ex.Message);
        Assert.Contains("'a=b'", ex.Message);
    }

    /// <summary>
    /// Verifies that a section name INI would read back as something else throws
    /// <see cref="IniSerializationException" /> naming the section, with the writer's <see cref="ArgumentException" />
    /// as the inner exception.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenSectionNameCannotBeWritten_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, Dictionary<string, string>> { ["db];primary"] = new() { ["host"] = "x" } };

        IniSerializationException ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.AreEqual(typeof(ArgumentException), ex.InnerException.GetType());
        Assert.Contains("'db];primary'", ex.Message);
    }
}
