// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationEngineTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Infrastructure;

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies how the engine treats <see langword="null" /> member values when writing.
/// </summary>
public partial class SerializationEngineTests
{
    /// <summary>
    /// Verifies that a null member is omitted from the output under the default null handling.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ForDefaultHandling_ShouldOmitMember()
    {
        var model = new RequiredModel { Id = 1, Label = null };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model);

        Assert.IsFalse(ContainsProperty(tokens, "Label"));
        Assert.IsTrue(ContainsProperty(tokens, "Id"));
    }

    /// <summary>
    /// Verifies that a null member is written as a null token when null handling is set to include.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ForIncludeHandling_ShouldWriteNull()
    {
        var options = new FormatSerializerOptions { NullHandling = FormatNullHandling.Include };
        var model = new RequiredModel { Id = 1, Label = null };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model, options);

        Assert.IsTrue(ContainsProperty(tokens, "Label"));
    }

    /// <summary>
    /// Determines whether the token stream contains a property with the specified name.
    /// </summary>
    /// <param name="tokens">The tokens to inspect.</param>
    /// <param name="name">The property name to look for.</param>
    /// <returns><see langword="true" /> when the property name is present; otherwise <see langword="false" />.</returns>
    private static bool ContainsProperty(IReadOnlyList<SerializationToken> tokens, string name)
    {
        foreach (SerializationToken token in tokens)
        {
            if (token.Kind == SerializationValueKind.PropertyName && (string?)token.Value == name)
                return true;
        }

        return false;
    }
}
