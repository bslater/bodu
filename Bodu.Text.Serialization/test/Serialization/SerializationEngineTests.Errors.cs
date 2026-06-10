// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationEngineTests.Errors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Infrastructure;

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies the engine's failure behavior: missing required members, duplicate properties, unknown properties, and
/// unsupported types.
/// </summary>
public partial class SerializationEngineTests
{
    /// <summary>
    /// Verifies that deserializing an object missing a required member throws
    /// <see cref="FormatSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredMemberMissing_ShouldThrowFormatSerializationException()
    {
        IReadOnlyList<SerializationToken> tokens =
        [
            new(SerializationValueKind.StartObject, null),
            new(SerializationValueKind.PropertyName, "Label"),
            new(SerializationValueKind.String, "no-id"),
            new(SerializationValueKind.EndObject, null),
        ];

        var ex = Assert.ThrowsExactly<FormatSerializationException>(() =>
        {
            _ = FakeFormat.Deserialize<RequiredModel>(tokens);
        });

        Assert.IsTrue(ex.Message.Contains("Id", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that an unknown property in the input is skipped and the rest of the object binds normally.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertyIsUnknown_ShouldSkipAndBindRemainder()
    {
        IReadOnlyList<SerializationToken> tokens =
        [
            new(SerializationValueKind.StartObject, null),
            new(SerializationValueKind.PropertyName, "Unknown"),
            new(SerializationValueKind.String, "ignored"),
            new(SerializationValueKind.PropertyName, "Name"),
            new(SerializationValueKind.String, "Grace"),
            new(SerializationValueKind.PropertyName, "Age"),
            new(SerializationValueKind.Int64, 40L),
            new(SerializationValueKind.EndObject, null),
        ];

        MutablePerson result = FakeFormat.Deserialize<MutablePerson>(tokens);

        Assert.AreEqual("Grace", result.Name);
        Assert.AreEqual(40, result.Age);
    }

    /// <summary>
    /// Verifies that a duplicate property in the input throws <see cref="FormatSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertyIsDuplicated_ShouldThrowFormatSerializationException()
    {
        IReadOnlyList<SerializationToken> tokens =
        [
            new(SerializationValueKind.StartObject, null),
            new(SerializationValueKind.PropertyName, "Name"),
            new(SerializationValueKind.String, "first"),
            new(SerializationValueKind.PropertyName, "Name"),
            new(SerializationValueKind.String, "second"),
            new(SerializationValueKind.EndObject, null),
        ];

        Assert.ThrowsExactly<FormatSerializationException>(() =>
        {
            _ = FakeFormat.Deserialize<MutablePerson>(tokens);
        });
    }

    /// <summary>
    /// Verifies that resolving a converter for an unsupported type throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenTypeIsUnsupported_ShouldThrowNotSupportedException()
    {
        var options = new FormatSerializerOptions();

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = options.GetConverter(typeof(decimal));
        });
    }

    /// <summary>
    /// Verifies that an integer value outside the target type's range throws
    /// <see cref="FormatSerializationException" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerOverflowsTarget_ShouldThrowFormatSerializationException()
    {
        IReadOnlyList<SerializationToken> tokens = [new(SerializationValueKind.Int64, 100000L)];

        Assert.ThrowsExactly<FormatSerializationException>(() =>
        {
            _ = FakeFormat.Deserialize<byte>(tokens);
        });
    }
}
