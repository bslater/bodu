// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the <see cref="DotEnvSerializer.Deserialize{T}(string, DotEnvSerializerOptions?)" /> backbone tests.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a document binds to a POCO, converting typed values with invariant parsing.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPoco_ShouldBindMappedKeysAndConvertValues()
    {
        ServerConfig config = DotEnvSerializer.Deserialize<ServerConfig>("HOST=example\nPORT=8080\nTLS=true\n");

        Assert.AreEqual("example", config.Host);
        Assert.AreEqual(8080, config.Port);
        Assert.IsTrue(config.UseTls);
    }

    /// <summary>
    /// Verifies that a document binds to a string-keyed dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringDictionary_ShouldBindEachEntry()
    {
        Dictionary<string, string> map = DotEnvSerializer.Deserialize<Dictionary<string, string>>("A=1\nB=two\n");

        Assert.AreEqual("1", map["A"]);
        Assert.AreEqual("two", map["B"]);
    }

    /// <summary>
    /// Verifies that an <see cref="int" />-valued dictionary converts each value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypedDictionary_ShouldConvertValues()
    {
        Dictionary<string, int> map = DotEnvSerializer.Deserialize<Dictionary<string, int>>("A=1\nB=2\n");

        Assert.AreEqual(1, map["A"]);
        Assert.AreEqual(2, map["B"]);
    }

    /// <summary>
    /// Verifies that a missing required key throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeyMissing_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<RequiredConfig>("OTHER=1\n");
        });
    }

    /// <summary>
    /// Verifies that case-insensitive matching binds keys whose case differs from the member name.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitive_ShouldBindDifferentlyCasedKeys()
    {
        var options = new DotEnvSerializerOptions { PropertyNameCaseInsensitive = true };

        RequiredConfig config = DotEnvSerializer.Deserialize<RequiredConfig>("token=abc\n", options);

        Assert.AreEqual("abc", config.Token);
    }

    /// <summary>
    /// Verifies that an unconvertible value throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueNotConvertible_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<ServerConfig>("HOST=x\nPORT=notanumber\n");
        });
    }

    /// <summary>
    /// Verifies that the deserialized callback fires after binding.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypeImplementsOnDeserialized_ShouldInvokeCallback()
    {
        CallbackConfig config = DotEnvSerializer.Deserialize<CallbackConfig>("Name=x\n");

        Assert.IsTrue(config.DeserializedCalled);
    }

    /// <summary>
    /// Verifies that a duplicated key resolves to the last occurrence, matching the near-universal .env last-wins rule.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDuplicateKey_ShouldTakeLastValue()
    {
        Dictionary<string, string> map = DotEnvSerializer.Deserialize<Dictionary<string, string>>("A=1\nA=2\nA=3\n");

        Assert.AreEqual("3", map["A"]);
    }

    /// <summary>
    /// Verifies that a key defined twice binds the value of its last definition, both into a dictionary and into a POCO
    /// member mapped under the <see cref="DotEnvSerializerDefaults.Web" /> defaults.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyRepeated_ShouldBindLastValue()
    {
        const string Text = "MULTI1=foo\nMULTI1=bar\n";
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web);

        Dictionary<string, string> map = DotEnvSerializer.Deserialize<Dictionary<string, string>>(Text, options);
        RepeatedKeyConfig config = DotEnvSerializer.Deserialize<RepeatedKeyConfig>(Text, options);

        Assert.AreEqual("bar", map["MULTI1"]);
        Assert.AreEqual("bar", config.Multi1);
    }

    /// <summary>
    /// Verifies that a scalar root type, or the nullable form of one, throws <see cref="DotEnvSerializationException" /> naming the
    /// type rather than returning its default value.
    /// </summary>
    /// <param name="kat">The scalar root type and the exception it raises.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarRootTypes),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Deserialize_WhenRootIsAScalar_ShouldThrowDotEnvSerializationException(InvalidKat<Type> kat)
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DeserializeAs(kat.Input, "A=1\n");
        });

        Assert.IsTrue(ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a POCO without a public parameterless constructor throws
    /// <see cref="DotEnvSerializationException" /> naming the type, rather than the runtime's
    /// <see cref="MissingMethodException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheTypeHasNoParameterlessConstructor_ShouldThrowDotEnvSerializationException()
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<ConstructedConfig>("HOST=localhost\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(ConstructedConfig), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that an abstract POCO throws <see cref="DotEnvSerializationException" /> naming the type, rather than
    /// the runtime's <see cref="MissingMethodException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTheTypeIsAbstract_ShouldThrowDotEnvSerializationException()
    {
        var ex = Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<AbstractConfig>("HOST=localhost\n");
        });

        Assert.IsTrue(ex.Message.Contains(nameof(AbstractConfig), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a key binds only to the most derived member of its name, so that a base <see cref="int" /> member
    /// hidden with <see langword="new" /> by a text member is not bound, and text it cannot convert does not throw.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAMemberHidesABaseMemberWithNew_ShouldBindOnlyTheMostDerivedMember()
    {
        PortTextHidingConfig config = DotEnvSerializer.Deserialize<PortTextHidingConfig>("Port=abc\n");

        Assert.AreEqual("abc", config.Port);
        Assert.AreEqual(0, ((PortNumberBase)config).Port);
    }
}
