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
    /// Verifies that a dictionary root type whose keys are not strings, such as
    /// <see cref="System.Collections.Hashtable" /> or a <see cref="Dictionary{TKey, TValue}" /> keyed by
    /// <see cref="int" />, throws <see cref="IniSerializationException" /> naming the type, as a root that is not an
    /// object does, rather than returning an empty instance.
    /// </summary>
    /// <param name="kat">The dictionary root type and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(NonStringKeyedDictionaryRootTypes),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Deserialize_WhenRootIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException(InvalidKat<Type> kat)
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = DeserializeAs(kat.Input, "A=a\n");
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section member whose type is a dictionary keyed by <see cref="int" /> throws
    /// <see cref="IniSerializationException" /> naming the member, rather than binding an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenASectionMemberIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<IntKeyedSectionConfig>("[Codes]\n1=a\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Codes'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the reserved global-section member, when its type is a dictionary keyed by <see cref="int" />,
    /// throws <see cref="IniSerializationException" /> naming the member, rather than binding an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheGlobalSectionMemberIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "Codes" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<IntKeyedSectionConfig>("1=a\n", options);
        });

        Assert.IsTrue(ex.Message.Contains("'Codes'", StringComparison.Ordinal), ex.Message);
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

    /// <summary>
    /// Verifies that a value that cannot be converted throws <see cref="IniSerializationException" /> naming the key
    /// and the member's declared type, a nullable one included, with the conversion error as the inner exception.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAValueCannotBeConverted_ShouldNameTheKeyAndTheDeclaredType()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<NullableCountConfig>("Count=abc\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Count'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(int?).ToString(), StringComparison.Ordinal), ex.Message);
        Assert.IsInstanceOfType<FormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a value-type root model is created and its global keys bound.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootIsAStruct_ShouldBindItsMembers()
    {
        PortStruct config = IniSerializer.Deserialize<PortStruct>("Port=5\nHost=h\n");

        Assert.AreEqual(5, config.Port);
        Assert.AreEqual("h", config.Host);
    }

    /// <summary>
    /// Verifies that a dictionary root of sections of a concrete type is created as that type, and one declared as the
    /// dictionary interface, sections included, as <see cref="Dictionary{TKey, TValue}" /> instances.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootIsAStringKeyedDictionary_ShouldCreateTheDeclaredTypeOrADictionary()
    {
        SortedDictionary<string, Dictionary<string, int>> sorted = IniSerializer.Deserialize<SortedDictionary<string, Dictionary<string, int>>>("[s]\nB=2\n");
        IDictionary<string, IDictionary<string, int>> declared = IniSerializer.Deserialize<IDictionary<string, IDictionary<string, int>>>("[s]\nB=2\n");

        Assert.AreEqual(2, sorted["s"]["B"]);
        Assert.IsInstanceOfType<Dictionary<string, IDictionary<string, int>>>(declared);
        Assert.IsInstanceOfType<Dictionary<string, int>>(declared["s"]);
        Assert.AreEqual(2, declared["s"]["B"]);
    }

    /// <summary>
    /// Verifies that a root declared as a read-only dictionary interface, which is not a dictionary the serializer
    /// creates, throws <see cref="IniSerializationException" /> naming the type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootIsAReadOnlyDictionaryInterface_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<IReadOnlyDictionary<string, int>>("B=2\n");
        });

        Assert.IsTrue(ex.Message.Contains("IReadOnlyDictionary", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root that implements <see cref="IDictionary{TKey, TValue}" /> keyed by <see cref="string" />,
    /// but not the non-generic <see cref="System.Collections.IDictionary" />, is created and bound through the generic
    /// interface, each global key converted to the value type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheRootImplementsOnlyTheGenericDictionaryInterface_ShouldMapTheGlobalKeys()
    {
        GenericOnlyDictionary<int> root = IniSerializer.Deserialize<GenericOnlyDictionary<int>>("b=2\na=1\n");

        CollectionAssert.AreEqual(new[] { "b", "a" }, root.Keys.ToArray());
        Assert.AreEqual(2, root["b"]);
        Assert.AreEqual(1, root["a"]);
    }

    /// <summary>
    /// Verifies that a root whose sections, and the root itself, implement only the generic dictionary interface maps
    /// the reserved global entries and each section to a dictionary of that type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheSectionsImplementOnlyTheGenericDictionaryInterface_ShouldMapEachSection()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        GenericOnlyDictionary<GenericOnlyDictionary<string>> root =
            IniSerializer.Deserialize<GenericOnlyDictionary<GenericOnlyDictionary<string>>>("a=1\n[db]\nhost=x\n", options);

        CollectionAssert.AreEqual(new[] { "global", "db" }, root.Keys.ToArray());
        Assert.AreEqual("1", root["global"]["a"]);
        Assert.AreEqual("x", root["db"]["host"]);
    }

    /// <summary>
    /// Verifies that a section member whose type implements only the generic dictionary interface binds the section's
    /// entries.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenASectionMemberImplementsOnlyTheGenericDictionaryInterface_ShouldBindItsEntries()
    {
        GenericOnlySectionConfig config = IniSerializer.Deserialize<GenericOnlySectionConfig>("Name=app\n[Logging]\nlevel=info\nsink=console\n");

        Assert.AreEqual("app", config.Name);
        Assert.IsNotNull(config.Logging);
        CollectionAssert.AreEqual(new[] { "level", "sink" }, config.Logging.Keys.ToArray());
        Assert.AreEqual("info", config.Logging["level"]);
        Assert.AreEqual("console", config.Logging["sink"]);
    }

    /// <summary>
    /// Verifies that a root member declared as <see cref="object" /> throws <see cref="IniSerializationException" />
    /// naming the member, rather than binding a bare <see cref="object" /> and dropping its section's keys.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARootMemberIsDeclaredAsObject_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ObjectMemberConfig>("Name=app\n[O]\nk=v\n");
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root member declared as <see cref="object" /> is rejected by its declared type, so that a
    /// document without a section for it still throws <see cref="IniSerializationException" /> naming the member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenARootMemberIsDeclaredAsObjectAndHasNoSection_ShouldThrowIniSerializationException()
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ObjectMemberConfig>("Name=app\n");
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the reserved global-section member, when it is declared as <see cref="object" />, throws
    /// <see cref="IniSerializationException" /> naming the member, rather than binding a bare <see cref="object" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheGlobalSectionMemberIsDeclaredAsObject_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "O" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ObjectMemberConfig>("k=v\n", options);
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section type with a get-only member that is not a scalar, which writing rejects, throws
    /// <see cref="IniSerializationException" /> naming the member when a section, or the reserved global section, is
    /// read into it, rather than binding the settable members and leaving the get-only one out.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="text">The INI text to read.</param>
    /// <param name="globalSectionName">The reserved global-section name, or <see langword="null" />.</param>
    [TestMethod]
    [DataRow("A section", "[S]\nName=b\n", null)]
    [DataRow("The global section", "Name=b\n", "S")]
    public void Deserialize_WhenASectionTypeHasAGetOnlyMemberThatIsNotAScalar_ShouldThrowIniSerializationException(string testName, string text, string? globalSectionName)
    {
        var options = new IniSerializerOptions { GlobalSectionName = globalSectionName };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<GetOnlyListSectionConfig>(text, options);
        }, testName);

        Assert.IsTrue(ex.Message.Contains("'Values'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section type with a get-only scalar member, and a get-only list left out with
    /// <see cref="Bodu.Text.Serialization.IgnoreAttribute" />, still reads, binding its settable members and leaving the get-only ones as they
    /// are.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenASectionTypeHasAGetOnlyScalarAndAnIgnoredGetOnlyList_ShouldBindTheSettableMembers()
    {
        GetOnlyMembersSectionConfig result = IniSerializer.Deserialize<GetOnlyMembersSectionConfig>("[S]\nName=b\nComputed=x\n");

        Assert.AreEqual("b", result.S.Name);
        Assert.AreEqual("computed", result.S.Computed);
        Assert.AreEqual(0, result.S.Values.Count);
    }
}
