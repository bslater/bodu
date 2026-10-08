// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.DelimitedFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Delimited;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Contains the end-to-end tests for the generated delimited record factories, chiefly
/// <c>GeneratedPerson.DelimitedFactory</c>.
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

    /// <summary>
    /// Verifies that serializing temporal values through the generated factory produces byte-identical output to the
    /// runtime reflection binder, which writes <see cref="DateTime" /> and <see cref="DateTimeOffset" /> in the
    /// round-trip <c>O</c> form and <see cref="TimeSpan" /> in the constant <c>c</c> form.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenTemporalValuesAreSerialized_ShouldMatchReflectionBinderOutput()
    {
        List<GeneratedTemporalRecord> records = CreateTemporalRecords();

        string reflection = DelimitedSerializer.Serialize(records);
        string generated = DelimitedSerializer.Serialize(records, GeneratedTemporalRecord.DelimitedFactory);

        Assert.AreEqual(reflection, generated);
    }

    /// <summary>
    /// Verifies that temporal values written and read back through the generated factory keep every tick, the
    /// <see cref="DateTimeKind" /> and the offset, as they do through the runtime reflection binder.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenTemporalValuesAreRoundTripped_ShouldKeepEveryTickTheKindAndTheOffset()
    {
        List<GeneratedTemporalRecord> records = CreateTemporalRecords();

        string text = DelimitedSerializer.Serialize(records, GeneratedTemporalRecord.DelimitedFactory);
        List<GeneratedTemporalRecord> restored = DelimitedSerializer.Deserialize(text, GeneratedTemporalRecord.DelimitedFactory);

        CollectionAssert.AreEqual(
            records.Select(Describe).ToArray(),
            restored.Select(Describe).ToArray(),
            $"The factory wrote {text.ReplaceLineEndings(@"\r\n")}.");
    }

    /// <summary>
    /// Verifies that the generated factory binds <see langword="null" /> to every nullable value-type member whose
    /// field is empty, as the runtime reflection binder does.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenNullableColumnsAreEmpty_ShouldBindNullAsTheReflectionBinderDoes()
    {
        const string Text = "Id,Flag,Letter,Count,Amount,Key,At,AtOffset,Span,Kind\n1,,,,,,,,,\n";

        List<GeneratedNullableRecord> generated = DelimitedSerializer.Deserialize(Text, GeneratedNullableRecord.DelimitedFactory);

        Assert.AreEqual("1|||||||||", Describe(generated.Single()));
        Assert.AreEqual(Describe(DelimitedSerializer.Deserialize<GeneratedNullableRecord>(Text).Single()), Describe(generated.Single()));
    }

    /// <summary>
    /// Verifies that the generated factory binds <see langword="null" /> to every nullable value-type member other than
    /// a <see cref="char" /> whose field holds only white space, as the runtime reflection binder does, rather than
    /// failing to parse the white space.
    /// </summary>
    /// <param name="blank">The white space each field holds.</param>
    [TestMethod]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow(" \t ")]
    public void DelimitedFactory_WhenNullableColumnsAreWhiteSpace_ShouldBindNullAsTheReflectionBinderDoes(string blank)
    {
        string text = "Id,Flag,Count,Amount,Key,At,AtOffset,Span,Kind\n1" + string.Concat(Enumerable.Repeat("," + blank, 8)) + "\n";

        List<GeneratedNullableRecord> generated = DelimitedSerializer.Deserialize(text, GeneratedNullableRecord.DelimitedFactory);

        Assert.AreEqual("1|||||||||", Describe(generated.Single()));
        Assert.AreEqual(Describe(DelimitedSerializer.Deserialize<GeneratedNullableRecord>(text).Single()), Describe(generated.Single()));
    }

    /// <summary>
    /// Verifies that the generated factory binds a nullable <see cref="char" /> field holding one space to the space,
    /// as the runtime reflection binder does, since only an empty field holds no character.
    /// </summary>
    [TestMethod]
    public void DelimitedFactory_WhenANullableCharColumnIsASpace_ShouldBindTheSpaceAsTheReflectionBinderDoes()
    {
        const string Text = "Id,Letter\n1,\" \"\n";

        List<GeneratedNullableRecord> generated = DelimitedSerializer.Deserialize(Text, GeneratedNullableRecord.DelimitedFactory);

        Assert.AreEqual((char?)' ', generated.Single().Letter);
        Assert.AreEqual(Describe(DelimitedSerializer.Deserialize<GeneratedNullableRecord>(Text).Single()), Describe(generated.Single()));
    }

    /// <summary>
    /// Creates records whose temporal values have every component a lossy form would drop: seven digits of fractional
    /// seconds, a UTC or unspecified kind, a whole or fractional offset, and a negative or multi-day duration.
    /// </summary>
    /// <returns>The records.</returns>
    private static List<GeneratedTemporalRecord> CreateTemporalRecords() =>
    [
        new()
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc).AddTicks(4567891),
            AtOffset = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromHours(10)).AddTicks(4567891),
            Span = new TimeSpan(1, 2, 3, 4).Add(TimeSpan.FromTicks(5678901)),
            MaybeAt = new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Unspecified).AddTicks(1),
            MaybeAtOffset = new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(-330)).AddTicks(9999999),
            MaybeSpan = TimeSpan.FromTicks(-1),
        },
        new()
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Unspecified),
            AtOffset = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.Zero),
            Span = TimeSpan.Zero,
        },
    ];

    /// <summary>
    /// Describes a temporal record in round-trip forms, so that two records describe alike only when every tick, kind
    /// and offset agree.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>The description, with an empty part for a <see langword="null" /> member.</returns>
    private static string Describe(GeneratedTemporalRecord record) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{record.At:O}|{record.AtOffset:O}|{record.Span:c}|{record.MaybeAt:O}|{record.MaybeAtOffset:O}|{record.MaybeSpan:c}");

    /// <summary>
    /// Describes a nullable record's members in invariant forms, the letter as its code point.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>The description, with an empty part for a <see langword="null" /> member.</returns>
    private static string Describe(GeneratedNullableRecord record) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{record.Id}|{record.Flag}|{(int?)record.Letter}|{record.Count}|{record.Amount}|{record.Key}|{record.At:O}|{record.AtOffset:O}|{record.Span:c}|{record.Kind}");
}
