// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

using Bodu.Test.Kat;
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

    /// <summary>
    /// Verifies that a scalar root, such as a <see cref="decimal" />, a <see cref="DateTime" /> or a
    /// <see cref="DateOnly" />, throws <see cref="IniSerializationException" /> naming its type rather than being written through
    /// its public properties.
    /// </summary>
    /// <param name="kat">The scalar root and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarRootValues),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenRootIsAScalar_ShouldThrowIniSerializationException(InvalidKat<object> kat)
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(kat.Input);
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary root whose keys are not strings, such as a <see cref="System.Collections.Hashtable" />
    /// or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />, throws
    /// <see cref="IniSerializationException" /> naming its type, as a root that is not an object does, rather than
    /// writing entries that do not read back into it.
    /// </summary>
    /// <param name="kat">The dictionary root and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(NonStringKeyedDictionaryRootValues),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenRootIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException(InvalidKat<object> kat)
    {
        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(kat.Input);
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section member whose type is a dictionary keyed by <see cref="int" /> throws
    /// <see cref="IniSerializationException" /> naming the member, rather than writing a section that does not read
    /// back into it.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenASectionMemberIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var config = new IntKeyedSectionConfig { Codes = new Dictionary<int, string> { [1] = "a" } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains("'Codes'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the reserved global-section member, when its type is a dictionary keyed by <see cref="int" />,
    /// throws <see cref="IniSerializationException" /> naming the member, rather than writing global keys that do not
    /// read back into it.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheGlobalSectionMemberIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var config = new IntKeyedSectionConfig { Codes = new Dictionary<int, string> { [1] = "a" } };
        var options = new IniSerializerOptions { GlobalSectionName = "Codes" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'Codes'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root dictionary entry whose value is a dictionary keyed by <see cref="int" /> throws
    /// <see cref="IniSerializationException" /> naming the entry, rather than writing it as a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARootEntryIsADictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, object> { ["codes"] = new Dictionary<int, string> { [1] = "a" } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("'codes'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a global key hiding a base member of the same name with <see langword="new" /> is written once,
    /// from the most derived declaration, and that the hidden member is not written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAMemberHidesABaseMemberWithNew_ShouldWriteOnlyTheMostDerivedMember()
    {
        var config = new PortNumberHidingConfig { Port = 5 };
        ((PortTextBase)config).Port = "x";

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Port=5\n", text);
    }

    /// <summary>
    /// Verifies that a type mapping two global keys to one name throws <see cref="IniSerializationException" /> naming
    /// the type and the name, rather than writing the key twice.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTwoMembersMapToOneName_ShouldThrowIniSerializationException()
    {
        var config = new DuplicateKeyConfig { First = "a", Second = "b" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains(nameof(DuplicateKeyConfig), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains("'X'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the options' default ignore condition leaves out the null members, or the null and default ones,
    /// that it ignores, and that a member's own condition overrides it.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    /// <param name="expected">The text written.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never, "Text=\nZero=0\nOne=1\nFlag=false\nMaybe=\nNullByMember=\n")]
    [DataRow(IgnoreCondition.WhenWritingNull, "Zero=0\nOne=1\nFlag=false\nNullByMember=\n")]
    [DataRow(IgnoreCondition.WhenWritingDefault, "One=1\nNullByMember=\n")]
    public void Serialize_WhenTheDefaultIgnoreConditionIsSet_ShouldLeaveOutTheMembersItIgnores(IgnoreCondition condition, string expected)
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = condition };

        string text = IniSerializer.Serialize(new ConditionalConfig(), options);

        Assert.AreEqual(expected, text);
    }

    /// <summary>
    /// Verifies that a dictionary entry whose value is <see langword="null" /> is left out unless the options' default
    /// ignore condition is <see cref="IgnoreCondition.Never" />, which writes it with an empty value.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    /// <param name="expected">The text written.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never, "A=\nB=b\n")]
    [DataRow(IgnoreCondition.WhenWritingNull, "B=b\n")]
    [DataRow(IgnoreCondition.WhenWritingDefault, "B=b\n")]
    public void Serialize_WhenADictionaryValueIsNull_ShouldLeaveItOutUnlessTheConditionIsNever(IgnoreCondition condition, string expected)
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = condition };
        var map = new Dictionary<string, string?> { ["A"] = null, ["B"] = "b" };

        string text = IniSerializer.Serialize(map, options);

        Assert.AreEqual(expected, text);
    }

    /// <summary>
    /// Verifies that a root that implements <see cref="IDictionary{TKey, TValue}" /> keyed by <see cref="string" />,
    /// but not the non-generic <see cref="System.Collections.IDictionary" />, serializes each entry as a global key, in
    /// the order the dictionary enumerates them.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheRootImplementsOnlyTheGenericDictionaryInterface_ShouldWriteTheGlobalKeys()
    {
        var root = new GenericOnlyDictionary<int> { ["b"] = 2, ["a"] = 1 };

        string text = IniSerializer.Serialize(root);

        Assert.AreEqual("b=2\na=1\n", text);
    }

    /// <summary>
    /// Verifies that a root whose sections, and the root itself, implement only the generic dictionary interface
    /// writes the reserved global entry as global keys and every other entry as a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheSectionsImplementOnlyTheGenericDictionaryInterface_ShouldWriteEachSection()
    {
        var root = new GenericOnlyDictionary<GenericOnlyDictionary<string>>
        {
            ["db"] = new() { ["host"] = "x" },
            ["global"] = new() { ["a"] = "1" },
        };
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        string text = IniSerializer.Serialize(root, options);

        Assert.AreEqual("a=1\n[db]\nhost=x\n", text);
    }

    /// <summary>
    /// Verifies that a section member whose type implements only the generic dictionary interface is written as a
    /// section of its entries.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenASectionMemberImplementsOnlyTheGenericDictionaryInterface_ShouldWriteItsEntries()
    {
        var config = new GenericOnlySectionConfig
        {
            Name = "app",
            Logging = new GenericOnlyDictionary<string> { ["level"] = "info", ["sink"] = "console" },
        };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=app\n[Logging]\nlevel=info\nsink=console\n", text);
    }

    /// <summary>
    /// Verifies that a section entry whose value implements only the generic dictionary interface nests beyond INI's
    /// two levels, as a <see cref="Dictionary{TKey, TValue}" /> value does, and throws
    /// <see cref="IniSerializationException" /> naming the entry.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenASectionEntryImplementsOnlyTheGenericDictionaryInterface_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, Dictionary<string, object>>
        {
            ["db"] = new() { ["inner"] = new GenericOnlyDictionary<string> { ["host"] = "x" } },
        };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("'inner'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root dictionary entry whose value is not a scalar, such as a <see cref="List{T}" />, throws
    /// <see cref="IniSerializationException" /> naming the key and the value's type, rather than writing the value's
    /// <see cref="object.ToString" /> text as a global key, which does not read back.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARootEntryIsNotAScalar_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, object> { ["name"] = "app", ["a"] = new List<int> { 1 } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a section dictionary entry whose value is not a scalar, such as a <see cref="List{T}" />, throws
    /// <see cref="IniSerializationException" /> naming the section, the key and the value's type, rather than writing
    /// the value's <see cref="object.ToString" /> text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenASectionEntryIsNotAScalar_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, Dictionary<string, object>>
        {
            ["db"] = new() { ["host"] = "x", ["ports"] = new List<int> { 1, 2 } },
        };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("'db'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains("'ports'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root dictionary entry whose value implements only a generic dictionary interface keyed by
    /// <see cref="int" /> throws <see cref="IniSerializationException" /> naming the key, rather than writing the
    /// value's <see cref="object.ToString" /> text as a global key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARootEntryIsAGenericOnlyDictionaryWhoseKeysAreNotStrings_ShouldThrowIniSerializationException()
    {
        var root = new Dictionary<string, object> { ["codes"] = new GenericOnlyIntKeyedDictionary { [1] = "a" } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("'codes'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary entry whose value is <see langword="null" /> or a scalar, an enumeration and a
    /// <see cref="Uri" /> included, is still written, at the root and in a section, as each value's own text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenEntriesAreScalarsOrNull_ShouldWriteThemByTheirOwnType()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, object?>
        {
            ["day"] = DayOfWeek.Monday,
            ["none"] = null,
            ["db"] = new Dictionary<string, object?> { ["uri"] = new Uri("https://example.com/"), ["count"] = 2 },
        };

        string text = IniSerializer.Serialize(root, options);

        Assert.AreEqual("day=Monday\nnone=\n[db]\nuri=https://example.com/\ncount=2\n", text);
    }

    /// <summary>
    /// Verifies that a root member declared as <see cref="object" /> throws <see cref="IniSerializationException" />
    /// naming the member, rather than writing its value as a section that reads back as a bare <see cref="object" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARootMemberIsDeclaredAsObject_ShouldThrowIniSerializationException()
    {
        var config = new ObjectMemberConfig { Name = "app", O = new Dictionary<string, string> { ["k"] = "v" } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a root member declared as <see cref="object" /> is rejected by its declared type, so that a
    /// <see langword="null" /> value, which the default ignore condition would leave out, still throws
    /// <see cref="IniSerializationException" /> naming the member.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenARootMemberDeclaredAsObjectIsNull_ShouldThrowIniSerializationException()
    {
        var config = new ObjectMemberConfig { Name = "app" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that the reserved global-section member, when it is declared as <see cref="object" />, throws
    /// <see cref="IniSerializationException" /> naming the member, rather than writing its value's entries as global
    /// keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheGlobalSectionMemberIsDeclaredAsObject_ShouldThrowIniSerializationException()
    {
        var config = new ObjectMemberConfig { Name = "app", O = new Dictionary<string, string> { ["k"] = "v" } };
        var options = new IniSerializerOptions { GlobalSectionName = "O" };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'O'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a dictionary root whose value type is a section type, written
    /// because the options' default ignore condition is <see cref="IgnoreCondition.Never" />, is written as an empty
    /// section, as a POCO root's <see langword="null" /> section member is, rather than as an empty global key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullRootEntryIsASectionUnderNever_ShouldWriteAnEmptySection()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, Dictionary<string, string>?> { ["db"] = null, ["x"] = new() { ["k"] = "v" } };

        string text = IniSerializer.Serialize(root, options);

        Assert.AreEqual("[db]\n[x]\nk=v\n", text);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a dictionary root whose value type is a section type is left
    /// out under the default ignore condition, which leaves out <see langword="null" /> values.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullRootEntryIsASectionAndNullsAreIgnored_ShouldLeaveItOut()
    {
        var root = new Dictionary<string, Dictionary<string, string>?> { ["db"] = null, ["x"] = new() { ["k"] = "v" } };

        string text = IniSerializer.Serialize(root);

        Assert.AreEqual("[x]\nk=v\n", text);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a dictionary root whose value type is neither a scalar,
    /// <see cref="object" /> nor a section type, written because the options' default ignore condition is
    /// <see cref="IgnoreCondition.Never" />, throws <see cref="IniSerializationException" /> naming the key and the
    /// value type, rather than writing an empty key that does not read back as that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullRootEntryIsNeitherAScalarNorASectionUnderNever_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, List<int>?> { ["a"] = null };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root, options);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a root section whose dictionary value type is not a scalar,
    /// written because the options' default ignore condition is <see cref="IgnoreCondition.Never" />, throws
    /// <see cref="IniSerializationException" /> naming the key and the value type, rather than writing an empty key
    /// that does not read back as that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullSectionEntryIsNotAScalarUnderNever_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, Dictionary<string, List<int>?>> { ["s"] = new() { ["a"] = null } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root, options);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a root section whose dictionary value type is itself a section
    /// type, a third level, throws <see cref="IniSerializationException" /> naming the key under
    /// <see cref="IgnoreCondition.Never" />, as a non-null entry of that type does.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullSectionEntryIsASectionTypeUnderNever_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, Dictionary<string, Dictionary<string, string>?>> { ["s"] = new() { ["a"] = null } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root, options);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of the reserved global section, when the section's dictionary value
    /// type is not a scalar, throws <see cref="IniSerializationException" /> naming the key under
    /// <see cref="IgnoreCondition.Never" />, rather than writing an empty global key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullGlobalSectionEntryIsNotAScalarUnderNever_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never, GlobalSectionName = "global" };
        var root = new Dictionary<string, Dictionary<string, List<int>?>> { ["global"] = new() { ["a"] = null } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(root, options);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry of a section member's dictionary, when its value type is not a
    /// scalar, throws <see cref="IniSerializationException" /> naming the key under <see cref="IgnoreCondition.Never" />,
    /// rather than writing an empty key in the section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenANullEntryOfASectionMemberIsNotAScalarUnderNever_ShouldThrowIniSerializationException()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var config = new ListValuedSectionConfig { Codes = new Dictionary<string, List<int>?> { ["a"] = null } };

        var ex = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'a'", StringComparison.Ordinal), ex.Message);
    }
}
