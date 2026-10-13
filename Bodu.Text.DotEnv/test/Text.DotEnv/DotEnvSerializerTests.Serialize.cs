// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the <see cref="DotEnvSerializer.Serialize{T}(T, DotEnvSerializerOptions?)" /> backbone tests.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a POCO serializes to key/value lines using the mapped names and invariant value formats.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPoco_ShouldWriteMappedKeysAndValues()
    {
        var config = new ServerConfig { Host = "localhost", Port = 5432, UseTls = true, Secret = "hidden" };

        string text = DotEnvSerializer.Serialize(config);

        Assert.AreEqual("HOST=localhost\nPORT=5432\nTLS=true\n", text);
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary serializes each entry as a key/value line.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringDictionary_ShouldWriteEachEntry()
    {
        var map = new Dictionary<string, string> { ["A"] = "1", ["B"] = "two" };

        string text = DotEnvSerializer.Serialize(map);

        Assert.AreEqual("A=1\nB=two\n", text);
    }

    /// <summary>
    /// Verifies that a scalar root throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarRoot_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(42);
        });
    }

    /// <summary>
    /// Verifies that the serializing callback fires during serialization.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeImplementsOnSerializing_ShouldInvokeCallback()
    {
        var config = new CallbackConfig { Name = "x" };

        _ = DotEnvSerializer.Serialize(config);

        Assert.IsTrue(config.SerializingCalled);
    }

    /// <summary>
    /// Verifies that a scalar root, such as a <see cref="decimal" />, a <see cref="DateTime" /> or a
    /// <see cref="DateOnly" />, throws <see cref="DotEnvSerializationException" /> naming its type rather than being written through
    /// its public properties.
    /// </summary>
    /// <param name="kat">The scalar root and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarRootValues),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenRootIsAScalar_ShouldThrowDotEnvSerializationException(InvalidKat<object> kat)
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(kat.Input);
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary root whose keys are not strings, such as a <see cref="System.Collections.Hashtable" />
    /// or a <see cref="Dictionary{TKey, TValue}" /> keyed by <see cref="int" />, throws
    /// <see cref="DotEnvSerializationException" /> naming its type, as a root that is not an object does, rather than
    /// writing entries that do not read back into it.
    /// </summary>
    /// <param name="kat">The dictionary root and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(NonStringKeyedDictionaryRootValues),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenRootIsADictionaryWhoseKeysAreNotStrings_ShouldThrowDotEnvSerializationException(InvalidKat<object> kat)
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(kat.Input);
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary key the reader would not read back throws <see cref="DotEnvSerializationException" />
    /// naming the key, with the writer's <see cref="ArgumentException" /> as the inner exception, rather than writing
    /// text that does not read back.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenADictionaryKeyCannotBeReadBack_ShouldThrowDotEnvSerializationException()
    {
        var map = new Dictionary<string, string> { ["db.host"] = "localhost" };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(map);
        });

        Assert.IsTrue(ex.Message.Contains("db.host", StringComparison.Ordinal), ex.Message);
        Assert.IsInstanceOfType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a member hiding a base member of the same name with <see langword="new" /> is written once, from
    /// the most derived declaration, and that the hidden member is not written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAMemberHidesABaseMemberWithNew_ShouldWriteOnlyTheMostDerivedMember()
    {
        var config = new PortNumberHidingConfig { Port = 5 };
        ((PortTextBase)config).Port = "x";

        string text = DotEnvSerializer.Serialize(config);

        Assert.AreEqual("Port=5\n", text);
    }

    /// <summary>
    /// Verifies that a type mapping two members to one key throws <see cref="DotEnvSerializationException" /> naming the
    /// type and the key, rather than writing the key twice.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTwoMembersMapToOneKey_ShouldThrowDotEnvSerializationException()
    {
        var config = new DuplicateKeyConfig { First = "a", Second = "b" };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains(nameof(DuplicateKeyConfig), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains("'X'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a type whose naming policy maps a member to the key another member names explicitly throws
    /// <see cref="DotEnvSerializationException" /> naming the key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheNamingPolicyMapsTwoMembersToOneKey_ShouldThrowDotEnvSerializationException()
    {
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web);
        var config = new PolicyDuplicateKeyConfig { HostName = "a", Alias = "b" };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'HOST_NAME'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a member declared as <see cref="object" /> is written in the form of its value's own type: a
    /// <see cref="DateTime" /> in its round-trip form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAMemberIsDeclaredAsObject_ShouldFormatTheValueByItsOwnType()
    {
        var config = new ObjectValueConfig { Value = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567) };

        string text = DotEnvSerializer.Serialize(config);

        Assert.AreEqual("Value=2026-01-02T03:04:05.1234567Z\n", text);
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
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };

        string text = DotEnvSerializer.Serialize(new ConditionalConfig(), options);

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
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };
        var map = new Dictionary<string, string?> { ["A"] = null, ["B"] = "b" };

        string text = DotEnvSerializer.Serialize(map, options);

        Assert.AreEqual(expected, text);
    }

    /// <summary>
    /// Verifies that a root that implements <see cref="IDictionary{TKey, TValue}" /> keyed by <see cref="string" />,
    /// but not the non-generic <see cref="System.Collections.IDictionary" />, serializes each entry as a key/value line,
    /// in the order the dictionary enumerates them.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheRootImplementsOnlyTheGenericDictionaryInterface_ShouldWriteEachEntry()
    {
        var map = new GenericOnlyDictionary<int> { ["B"] = 2, ["A"] = 1 };

        string text = DotEnvSerializer.Serialize(map);

        Assert.AreEqual("B=2\nA=1\n", text);
    }

    /// <summary>
    /// Verifies that an entry whose value is <see langword="null" />, in a root that implements only the generic
    /// dictionary interface, is left out unless the options' default ignore condition is
    /// <see cref="IgnoreCondition.Never" />, as it is in a <see cref="Dictionary{TKey, TValue}" />.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    /// <param name="expected">The text written.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never, "A=\nB=b\n")]
    [DataRow(IgnoreCondition.WhenWritingNull, "B=b\n")]
    [DataRow(IgnoreCondition.WhenWritingDefault, "B=b\n")]
    public void Serialize_WhenAGenericOnlyDictionaryValueIsNull_ShouldLeaveItOutUnlessTheConditionIsNever(IgnoreCondition condition, string expected)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };
        var map = new GenericOnlyDictionary<string?> { ["A"] = null, ["B"] = "b" };

        string text = DotEnvSerializer.Serialize(map, options);

        Assert.AreEqual(expected, text);
    }
}
