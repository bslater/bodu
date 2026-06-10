// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeBindingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Infrastructure;

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies that the serialization attributes govern member naming, exclusion, ordering, and converter selection.
/// </summary>
[TestClass]
public class AttributeBindingTests
{
    /// <summary>
    /// Verifies that a member annotated with <see cref="FormatPropertyNameAttribute" /> is written under its explicit name.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberRenamed_ShouldUseExplicitName()
    {
        var model = new AnnotatedModel { Name = "Tom" };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model);

        Assert.IsTrue(HasProperty(tokens, "display_name"));
        Assert.IsFalse(HasProperty(tokens, "Name"));
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="FormatIgnoreAttribute" /> is excluded from the output.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIgnored_ShouldOmitMember()
    {
        var model = new AnnotatedModel { Secret = "hidden" };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model);

        Assert.IsFalse(HasProperty(tokens, "Secret"));
    }

    /// <summary>
    /// Verifies that members are written in ascending <see cref="FormatPropertyOrderAttribute" /> order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMembersOrdered_ShouldWriteInOrder()
    {
        var model = new AnnotatedModel { First = 1, Second = 2 };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model);

        Assert.IsTrue(IndexOfProperty(tokens, "First") < IndexOfProperty(tokens, "Second"));
    }

    /// <summary>
    /// Verifies that a property-level <see cref="FormatConverterAttribute" /> applies the named converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberHasConverterAttribute_ShouldApplyConverter()
    {
        var model = new ConverterModel { Code = "abc" };

        IReadOnlyList<SerializationToken> tokens = FakeFormat.Serialize(model);

        var written = (string?)tokens.Single(static t => t.Kind == SerializationValueKind.String).Value;
        Assert.AreEqual("cba", written);
    }

    /// <summary>
    /// Verifies that a property-level converter is applied symmetrically so the value round-trips.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenMemberHasConverterAttribute_ShouldPreserveValue()
    {
        var model = new ConverterModel { Code = "abc" };

        ConverterModel result = FakeFormat.RoundTrip(model);

        Assert.AreEqual("abc", result.Code);
    }

    /// <summary>
    /// Determines whether the token stream contains a property with the specified name.
    /// </summary>
    /// <param name="tokens">The tokens to inspect.</param>
    /// <param name="name">The property name.</param>
    /// <returns><see langword="true" /> when present; otherwise <see langword="false" />.</returns>
    private static bool HasProperty(IReadOnlyList<SerializationToken> tokens, string name) =>
        IndexOfProperty(tokens, name) >= 0;

    /// <summary>
    /// Returns the index of the first property token with the specified name, or -1.
    /// </summary>
    /// <param name="tokens">The tokens to inspect.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The token index, or -1 when not found.</returns>
    private static int IndexOfProperty(IReadOnlyList<SerializationToken> tokens, string name)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind == SerializationValueKind.PropertyName && (string?)tokens[i].Value == name)
                return i;
        }

        return -1;
    }
}
