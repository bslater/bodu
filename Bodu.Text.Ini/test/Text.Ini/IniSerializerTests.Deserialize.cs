// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Kat;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for the <see cref="IniSerializer.Deserialize{T}(string, IniSerializerOptions?)" /> overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a document with global keys and sections populates the matching POCO members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalsAndSections_ShouldPopulatePoco()
    {
        ServerConfig config = IniSerializer.Deserialize<ServerConfig>(
            "Name=app\nRetries=3\n[Database]\nHost=localhost\nPort=5432\n[Logging]\nlevel=info\n");

        Assert.AreEqual("app", config.Name);
        Assert.AreEqual(3, config.Retries);
        Assert.IsNotNull(config.Database);
        Assert.AreEqual("localhost", config.Database.Host);
        Assert.AreEqual(5432, config.Database.Port);
        Assert.IsNotNull(config.Logging);
        Assert.AreEqual("info", config.Logging["level"]);
    }

    /// <summary>
    /// Verifies that a nested string-keyed dictionary root maps each section to a sub-dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedDictionaryRoot_ShouldMapSections()
    {
        var root = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            "[db]\nhost=x\n[log]\nlevel=warn\n");

        Assert.AreEqual(2, root.Count);
        Assert.AreEqual("x", root["db"]["host"]);
        Assert.AreEqual("warn", root["log"]["level"]);
    }

    /// <summary>
    /// Verifies that a scalar-valued dictionary root maps the global keys directly.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarDictionaryRoot_ShouldMapGlobals()
    {
        var root = IniSerializer.Deserialize<Dictionary<string, string>>("a=1\nb=2\n");

        Assert.AreEqual(2, root.Count);
        Assert.AreEqual("1", root["a"]);
        Assert.AreEqual("2", root["b"]);
    }

    /// <summary>
    /// Verifies that sections cannot map into a scalar-valued dictionary root.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarDictionaryRootWithSections_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, string>>("[db]\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that global keys cannot map into a nested-dictionary root unless
    /// <see cref="IniSerializerOptions.GlobalSectionName" /> is set.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalsWithoutGlobalSectionName_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>("a=1\n[db]\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that global keys bind to the reserved key when <see cref="IniSerializerOptions.GlobalSectionName" />
    /// is set.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalSectionNameSet_ShouldBindGlobalsToReservedKey()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        var root = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            "a=1\n[db]\nhost=x\n", options);

        Assert.AreEqual("1", root["global"]["a"]);
        Assert.AreEqual("x", root["db"]["host"]);
    }

    /// <summary>
    /// Verifies that a missing required key throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeyMissing_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<RequiredConfig>("Other=1\n");
        });
    }

    /// <summary>
    /// Verifies that a value that cannot convert to the member type throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueNotConvertible_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ServerConfig>("[Database]\nPort=not-a-number\n");
        });
    }

    /// <summary>
    /// Verifies that key and section matching honours <see cref="IniSerializerOptions.PropertyNameCaseInsensitive" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertyNameCaseInsensitive_ShouldMatchDifferentCase()
    {
        var options = new IniSerializerOptions { PropertyNameCaseInsensitive = true };

        ServerConfig config = IniSerializer.Deserialize<ServerConfig>("NAME=app\n[DATABASE]\nHOST=x\n", options);

        Assert.AreEqual("app", config.Name);
        Assert.IsNotNull(config.Database);
        Assert.AreEqual("x", config.Database.Host);
    }

    /// <summary>
    /// Verifies that a scalar target type throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarTarget_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<int>("a=1\n");
        });
    }

    /// <summary>
    /// Verifies that the strict defaults reject a duplicate section with <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDuplicateSectionUnderStrictDefaults_ShouldThrowIniFormatException()
    {
        var options = new IniSerializerOptions(IniSerializerDefaults.Strict);

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                "[db]\nhost=x\n[db]\nport=5\n", options);
        });
    }

    /// <summary>
    /// Verifies that the strict defaults report a duplicate section or key at the offending header or key: its line,
    /// and the offset of its first byte, the <c>[</c> of a header or the first byte of a key after any leading
    /// whitespace.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The INI source, its lines separated by LF.</param>
    /// <param name="lineEnding">The line ending written in place of each LF.</param>
    /// <param name="line">The expected 1-based line of the offending header or key.</param>
    /// <param name="offset">The expected zero-based byte offset of its first byte.</param>
    [TestMethod]
    [DataRow("duplicate section, CR LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r\n", 5, 30)]
    [DataRow("duplicate key in a section, LF", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\n", 4, 17)]
    [DataRow("duplicate global key, lone CR", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r", 3, 10)]
    public void Deserialize_WhenStrictDefaultsRejectASectionOrKey_ShouldReportTheOffendingHeaderOrKey(string testName, string source, string lineEnding, int line, int offset)
    {
        _ = testName;
        var options = new IniSerializerOptions(IniSerializerDefaults.Strict);

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                source.Replace("\n", lineEnding, StringComparison.Ordinal), options);
        });

        Assert.AreEqual(line, ex.LineNumber, ex.Message);
        Assert.AreEqual(offset, ex.Offset, ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="float" /> member is parsed from its value with the invariant culture, so that a current
    /// culture whose decimal separator is a comma, and whose group separator is a period, reads the same value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberIsSingle_ShouldParseTheValue()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            RatioConfig config = IniSerializer.Deserialize<RatioConfig>("Ratio=2.5");

            Assert.AreEqual(2.5f, config.Ratio);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that a scalar root type, or the nullable form of one, throws <see cref="IniSerializationException" /> naming the
    /// type rather than returning its default value.
    /// </summary>
    /// <param name="kat">The scalar root type and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarRootTypes),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Deserialize_WhenRootIsAScalar_ShouldThrowIniSerializationException(InvalidKat<Type> kat)
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = DeserializeAs(kat.Input, "A=1\n");
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root type without a public parameterless constructor throws
    /// <see cref="IniSerializationException" /> naming the type, rather than the runtime's
    /// <see cref="MissingMethodException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootTypeHasNoParameterlessConstructor_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ConstructedConfig>("Name=web\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(ConstructedConfig), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that an abstract root type throws <see cref="IniSerializationException" /> naming the type, rather than
    /// the runtime's <see cref="MissingMethodException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootTypeIsAbstract_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<AbstractConfig>("Name=web\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(AbstractConfig), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section type without a public parameterless constructor throws
    /// <see cref="IniSerializationException" /> naming the type, rather than the runtime's
    /// <see cref="MissingMethodException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenASectionTypeHasNoParameterlessConstructor_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ConstructedSectionConfig>("[Database]\nHost=db\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(ConstructedSection), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a global key binds only to the most derived member of its name, so that a base
    /// <see cref="int" /> member hidden with <see langword="new" /> by a text member is not bound, and text it cannot
    /// convert does not throw.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAMemberHidesABaseMemberWithNew_ShouldBindOnlyTheMostDerivedMember()
    {
        PortTextHidingConfig config = IniSerializer.Deserialize<PortTextHidingConfig>("Port=abc\n");

        Assert.AreEqual("abc", config.Port);
        Assert.AreEqual(0, ((PortNumberBase)config).Port);
    }

    /// <summary>
    /// Verifies that a missing global key for a member declared with the C# <see langword="required" /> keyword throws
    /// <see cref="IniSerializationException" /> naming the key, as a member marked <see cref="RequiredAttribute" />
    /// does.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARequiredKeywordKeyIsMissing_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<RequiredKeywordConfig>("Other=1\n[Database]\nHost=x\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Name'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a missing section for a member declared with the C# <see langword="required" /> keyword throws
    /// <see cref="IniSerializationException" /> naming the section.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARequiredKeywordSectionIsMissing_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<RequiredKeywordConfig>("Name=app\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Database'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a missing key in a section, for a section member declared with the C# <see langword="required" />
    /// keyword, throws <see cref="IniSerializationException" /> naming the key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARequiredKeywordSectionKeyIsMissing_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<RequiredKeywordConfig>("Name=app\n[Database]\nPort=1\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Host'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a type mapping two global keys to one name throws <see cref="IniSerializationException" /> naming
    /// the type and the name, rather than binding the key to both.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTwoMembersMapToOneName_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<DuplicateKeyConfig>("X=a\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(DuplicateKeyConfig), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains("'X'", StringComparison.Ordinal), ex.Message);
    }
}
