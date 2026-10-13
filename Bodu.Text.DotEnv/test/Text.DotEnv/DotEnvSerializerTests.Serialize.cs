// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

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
}
