// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.DelimitedFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Contains the end-to-end tests for the generated <c>GeneratedPerson.DelimitedFactory</c>.
/// </summary>
public partial class FormatFactoryGeneratorTests
{
    /// <summary>
    /// Verifies that the generated factory resolves headers in declaration order, honouring
    /// <c>[PropertyName]</c> and excluding the <c>[Ignore]</c> member.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DelimitedFactory_WhenGenerated_ShouldExposeResolvedHeaders()
    {
        CollectionAssert.AreEqual(
            new[] { "Name", "Age", "email_address", "Score", "Kind" },
            GeneratedPerson.DelimitedFactory.Headers.ToArray());
    }

    /// <summary>
    /// Verifies that serializing through the generated factory produces byte-identical output to the runtime
    /// reflection binder.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenSerialized_ShouldMatchReflectionBinderOutput()
    {
        var people = new List<GeneratedPerson>
        {
            new() { Name = "Ada, the \"pioneer\"", Age = 36, Email = "ada@example.com", Score = 1.5, Kind = PersonKind.Mathematician },
            new() { Name = "Grace", Age = 45, Email = null, Score = null, Kind = PersonKind.Engineer },
        };

        string reflection = DelimitedSerializer.Serialize(people);
        string generated = DelimitedSerializer.Serialize(people, GeneratedPerson.DelimitedFactory);

        Assert.AreEqual(reflection, generated);
    }

    /// <summary>
    /// Verifies that a record round-trips through the generated factory, preserving nullable
    /// <see langword="null" /> values and the enum column.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenRoundTripped_ShouldPreserveValues()
    {
        var people = new List<GeneratedPerson>
        {
            new() { Name = "Ada", Age = 36, Email = "ada@example.com", Score = 2.75, Kind = PersonKind.Mathematician },
            new() { Name = "Grace", Age = 45, Email = null, Score = null, Kind = PersonKind.Unknown },
        };

        string text = DelimitedSerializer.Serialize(people, GeneratedPerson.DelimitedFactory);
        List<GeneratedPerson> restored = DelimitedSerializer.Deserialize(text, GeneratedPerson.DelimitedFactory);

        Assert.AreEqual(2, restored.Count);
        Assert.AreEqual("Ada", restored[0].Name);
        Assert.AreEqual(36, restored[0].Age);
        Assert.AreEqual("ada@example.com", restored[0].Email);
        Assert.AreEqual(2.75, restored[0].Score);
        Assert.AreEqual(PersonKind.Mathematician, restored[0].Kind);
        Assert.IsNull(restored[1].Score);
        Assert.AreEqual(PersonKind.Unknown, restored[1].Kind);
    }

    /// <summary>
    /// Verifies that the generated factory binds a headerless document positionally in declaration order.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenHeaderless_ShouldBindPositionally()
    {
        var options = new DelimitedSerializerOptions { NoHeader = true };

        List<GeneratedPerson> restored = DelimitedSerializer.Deserialize(
            "Ada,36,ada@example.com,1.5,Engineer\n", GeneratedPerson.DelimitedFactory, options);

        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual("Ada", restored[0].Name);
        Assert.AreEqual(36, restored[0].Age);
        Assert.AreEqual("ada@example.com", restored[0].Email);
        Assert.AreEqual(1.5, restored[0].Score);
        Assert.AreEqual(PersonKind.Engineer, restored[0].Kind);
    }

    /// <summary>
    /// Verifies that the generated factory falls back to case-insensitive header matching.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenHeaderCaseDiffers_ShouldBindCaseInsensitively()
    {
        List<GeneratedPerson> restored = DelimitedSerializer.Deserialize(
            "name,AGE\nAda,36\n", GeneratedPerson.DelimitedFactory);

        Assert.AreEqual("Ada", restored[0].Name);
        Assert.AreEqual(36, restored[0].Age);
    }

    /// <summary>
    /// Verifies that the generated factory never reads or writes the <c>[Ignore]</c> member.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenMemberIgnored_ShouldNotSerializeIt()
    {
        var person = new GeneratedPerson { Name = "Ada", Age = 36, Secret = "do-not-write" };

        string text = DelimitedSerializer.Serialize(new[] { person }, GeneratedPerson.DelimitedFactory);

        Assert.IsFalse(text.Contains("do-not-write", StringComparison.Ordinal));
    }
}
