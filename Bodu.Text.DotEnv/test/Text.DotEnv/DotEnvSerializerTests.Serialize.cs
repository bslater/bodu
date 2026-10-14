// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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

    /// <summary>
    /// Verifies that a member whose type is not a scalar, such as a <see cref="Dictionary{TKey, TValue}" />, throws
    /// <see cref="DotEnvSerializationException" /> naming the key and the type, rather than writing the value's
    /// <see cref="object.ToString" /> text, which does not read back.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAMemberIsNotAScalar_ShouldThrowDotEnvSerializationException()
    {
        var config = new DictionaryMemberConfig { Name = "a", Counts = new Dictionary<string, int> { ["x"] = 1 } };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains("'Counts'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(Dictionary<string, int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> member whose type is not a scalar, written because the options' default
    /// ignore condition is <see cref="IgnoreCondition.Never" />, throws <see cref="DotEnvSerializationException" />
    /// naming the key, rather than writing an empty value, which does not read back as that type either.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAMemberThatIsNotAScalarIsNull_ShouldThrowDotEnvSerializationException()
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var config = new DictionaryMemberConfig { Name = "a" };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'Counts'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a member declared as <see cref="object" /> whose value is not a scalar throws
    /// <see cref="DotEnvSerializationException" /> naming the key and the value's type, rather than writing the value's
    /// <see cref="object.ToString" /> text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAnObjectMemberHoldsAValueThatIsNotAScalar_ShouldThrowDotEnvSerializationException()
    {
        var config = new ObjectValueConfig { Value = new List<int> { 1, 2 } };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config);
        });

        Assert.IsTrue(ex.Message.Contains("'Value'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary root whose value type is not a scalar throws
    /// <see cref="DotEnvSerializationException" /> naming the key, rather than writing the value's
    /// <see cref="object.ToString" /> text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenADictionaryValueTypeIsNotAScalar_ShouldThrowDotEnvSerializationException()
    {
        var map = new Dictionary<string, List<int>> { ["A"] = [1] };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(map);
        });

        Assert.IsTrue(ex.Message.Contains("'A'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(List<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that an entry of a dictionary root with <see cref="object" /> values whose value is not a scalar throws
    /// <see cref="DotEnvSerializationException" /> naming the key and the value's type, rather than writing the value's
    /// <see cref="object.ToString" /> text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenADictionaryValueIsNotAScalar_ShouldThrowDotEnvSerializationException()
    {
        var map = new Dictionary<string, object> { ["A"] = "a", ["B"] = new Dictionary<string, string> { ["x"] = "y" } };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(map);
        });

        Assert.IsTrue(ex.Message.Contains("'B'", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(Dictionary<string, string>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a member whose value is not a scalar throws <see cref="DotEnvSerializationException" /> under every
    /// default ignore condition, none of which leaves out a value that is not <see langword="null" />.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never)]
    [DataRow(IgnoreCondition.WhenWritingNull)]
    [DataRow(IgnoreCondition.WhenWritingDefault)]
    public void Serialize_WhenAMemberIsNotAScalar_ForEveryIgnoreCondition_ShouldThrowDotEnvSerializationException(IgnoreCondition condition)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };
        var config = new DictionaryMemberConfig { Name = "a", Counts = new Dictionary<string, int> { ["x"] = 1 } };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(config, options);
        });

        Assert.IsTrue(ex.Message.Contains("'Counts'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a dictionary entry whose value is not a scalar throws <see cref="DotEnvSerializationException" />
    /// under every default ignore condition.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never)]
    [DataRow(IgnoreCondition.WhenWritingNull)]
    [DataRow(IgnoreCondition.WhenWritingDefault)]
    public void Serialize_WhenADictionaryValueIsNotAScalar_ForEveryIgnoreCondition_ShouldThrowDotEnvSerializationException(IgnoreCondition condition)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };
        var map = new Dictionary<string, object> { ["A"] = new List<int> { 1 } };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(map, options);
        });

        Assert.IsTrue(ex.Message.Contains("'A'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> member whose type is not a scalar throws
    /// <see cref="DotEnvSerializationException" /> by the member's type even when the ignore condition leaves it out,
    /// so that <c>Serialize</c> and <c>Deserialize</c> reject the same types.
    /// </summary>
    /// <param name="condition">The options' default ignore condition, which leaves out <see langword="null" /> values.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.WhenWritingNull)]
    [DataRow(IgnoreCondition.WhenWritingDefault)]
    public void Serialize_WhenAMemberThatIsNotAScalarIsNullAndLeftOut_ShouldThrowDotEnvSerializationException(IgnoreCondition condition)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };

        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(new DictionaryMemberConfig { Name = "a" }, options);
        });

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvValueNotScalar, "Counts", typeof(Dictionary<string, int>)), ex.Message);
    }

    /// <summary>
    /// Verifies that a set-only member whose type is not a scalar, which <c>Deserialize</c> would bind, throws
    /// <see cref="DotEnvSerializationException" /> when written too, although writing never reads the member.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAWriteOnlyMemberIsNotAScalar_ShouldThrowDotEnvSerializationException()
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(new WriteOnlyDictionaryMemberConfig { Name = "a" });
        });

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Op_Invalid_DotEnvValueNotScalar, "Counts", typeof(Dictionary<string, int>)), ex.Message);
    }

    /// <summary>
    /// Verifies that a root whose type is a nullable structure is written as the structure itself is.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTheRootIsANullableStruct_ShouldWriteTheStruct()
    {
        var value = new PortStruct { Port = 5, Host = "h" };

        Assert.AreEqual(DotEnvSerializer.Serialize(value), DotEnvSerializer.Serialize<PortStruct?>(value));
    }

    /// <summary>
    /// Verifies that under <see cref="IgnoreCondition.WhenWritingDefault" /> a nullable member that holds its underlying
    /// type's default, <c>0</c>, <see langword="false" /> or a default <see cref="DateTime" />, is written, since a
    /// nullable member's default is <see langword="null" />, as <c>System.Text.Json</c> decides it.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullableMembersHoldTheirUnderlyingDefaults_UnderWhenWritingDefault_ShouldWriteThem()
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault };
        var config = new NullableDefaultsConfig { Number = 0, Flag = false, At = default(DateTime) };

        string text = DotEnvSerializer.Serialize(config, options);

        Assert.AreEqual("Number=0\nFlag=false\nAt=0001-01-01T00:00:00.0000000\n", text);
    }

    /// <summary>
    /// Verifies that under <see cref="IgnoreCondition.WhenWritingDefault" /> a nullable member that is
    /// <see langword="null" />, its declared type's default, is still left out.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullableMembersAreNull_UnderWhenWritingDefault_ShouldLeaveThemOut()
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault };

        Assert.AreEqual(string.Empty, DotEnvSerializer.Serialize(new NullableDefaultsConfig(), options));
    }
}
