// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatFactoryGeneratorTests.IniFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;
using Bodu.Text.Serialization;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Contains the end-to-end tests for the generated INI section factories, chiefly
/// <c>GeneratedServerSection.IniFactory</c>.
/// </summary>
public partial class FormatFactoryGeneratorTests
{
    /// <summary>
    /// Verifies that the generated factory resolves keys in declaration order, honouring <c>[PropertyName]</c>.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenGenerated_ShouldExposeResolvedKeys()
    {
        CollectionAssert.AreEqual(
            new[] { "Host", "Port", "use_tls", "Timeout" },
            GeneratedServerSection.IniFactory.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that a section serializes through the generated factory to canonical INI bytes, leaving out the
    /// <see langword="null" /> timeout as the reflection binder does under its default condition.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenSerialized_ShouldWriteCanonicalSection()
    {
        var section = new GeneratedServerSection { Host = "db.example.com", Port = 5432, UseTls = true, Timeout = null };

        string text = IniSerializer.SerializeSection("server", section, GeneratedServerSection.IniFactory);

        Assert.AreEqual("[server]\nHost=db.example.com\nPort=5432\nuse_tls=true\n", text);
    }

    /// <summary>
    /// Verifies that a section round-trips through the generated factory, preserving the nullable key.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenRoundTripped_ShouldPreserveValues()
    {
        var section = new GeneratedServerSection
        {
            Host = "db.example.com",
            Port = 5432,
            UseTls = true,
            Timeout = TimeSpan.FromSeconds(90),
        };

        string text = IniSerializer.SerializeSection("server", section, GeneratedServerSection.IniFactory);
        GeneratedServerSection restored = IniSerializer.DeserializeSection(text, "server", GeneratedServerSection.IniFactory);

        Assert.AreEqual("db.example.com", restored.Host);
        Assert.AreEqual(5432, restored.Port);
        Assert.IsTrue(restored.UseTls);
        Assert.AreEqual(TimeSpan.FromSeconds(90), restored.Timeout);
    }

    /// <summary>
    /// Verifies that an empty value binds a nullable key to <see langword="null" /> and that key matching falls back
    /// to case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenValueEmptyOrKeyCaseDiffers_ShouldBindLeniently()
    {
        GeneratedServerSection restored = IniSerializer.DeserializeSection(
            "[server]\nHOST=example.org\nport=80\nuse_tls=false\nTimeout=\n", "server", GeneratedServerSection.IniFactory);

        Assert.AreEqual("example.org", restored.Host);
        Assert.AreEqual(80, restored.Port);
        Assert.IsFalse(restored.UseTls);
        Assert.IsNull(restored.Timeout);
    }

    /// <summary>
    /// Verifies that a section of temporal values serializes through the generated INI factory to the same bytes as
    /// INI's runtime reflection binder writes, in INI's own forms, which the delimited forms do not change.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenTemporalValuesAreSerialized_ShouldMatchReflectionBinderOutput()
    {
        var section = new GeneratedScheduleSection
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc).AddTicks(4567891),
            AtOffset = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromHours(10)).AddTicks(4567891),
            Span = new TimeSpan(1, 2, 3, 4).Add(TimeSpan.FromTicks(5678901)),
            Count = 7,
        };

        string reflection = IniSerializer.Serialize(new { schedule = section });
        string generated = IniSerializer.SerializeSection("schedule", section, GeneratedScheduleSection.IniFactory);

        Assert.AreEqual(reflection, generated);
    }

    /// <summary>
    /// Verifies that the generated INI factory leaves out a nullable key whose value is <see langword="null" />, as
    /// INI's reflection binder does under its default <see cref="IgnoreCondition.WhenWritingNull" /> condition, rather
    /// than writing the key with an empty value.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenANullableValueIsNull_ShouldLeaveTheKeyOutAsTheReflectionBinderDoes()
    {
        var section = new GeneratedScheduleSection
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc),
            AtOffset = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.Zero),
            Span = TimeSpan.FromMinutes(90),
            Count = null,
        };

        string reflection = IniSerializer.Serialize(new { schedule = section });
        string generated = IniSerializer.SerializeSection("schedule", section, GeneratedScheduleSection.IniFactory);

        Assert.AreEqual(reflection, generated);
        Assert.IsFalse(generated.Contains("Count", StringComparison.Ordinal), generated);
    }

    /// <summary>
    /// Verifies that the generated INI factory applies each member's own
    /// <see cref="Bodu.Text.Serialization.IgnoreAttribute" /> condition, and the default
    /// <see cref="IgnoreCondition.WhenWritingNull" /> to a member without one, so that it writes the same section as
    /// INI's reflection binder whether the members hold their default values or others.
    /// </summary>
    /// <param name="useDefaults">Whether the members hold default values, rather than set ones.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IniFactory_WhenMembersCarryIgnoreConditions_ShouldWriteTheSectionAsTheReflectionBinderDoes(bool useDefaults)
    {
        GeneratedConditionalSection section = useDefaults
            ? new() { MaybeZero = 0, Text = string.Empty }
            : new() { Plain = 1, Note = "n", Kept = 2, Zero = 3, MaybeZero = 4, Text = "t", Port = 5 };

        string reflection = IniSerializer.Serialize(new { conditional = section });
        string generated = IniSerializer.SerializeSection("conditional", section, GeneratedConditionalSection.IniFactory);

        Assert.AreEqual(reflection, generated);
    }

    /// <summary>
    /// Verifies that a value the generated INI factory cannot convert, an unparsable or overflowing integer, date or
    /// enum value, throws <see cref="IniSerializationException" /> carrying the conversion error, as INI's reflection
    /// binder does, rather than the raw conversion exception.
    /// </summary>
    /// <param name="key">The key that holds the value.</param>
    /// <param name="value">The value that cannot be converted.</param>
    [TestMethod]
    [DataRow("Count", "abc")]
    [DataRow("Count", "99999999999")]
    [DataRow("At", "not a date")]
    [DataRow("Kind", "Wizard")]
    public void IniFactory_WhenAValueCannotBeConverted_ShouldThrowIniSerializationException(string key, string value)
    {
        string text = $"[Schedule]\n{key}={value}\n";

        var reflection = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ScheduleDocument>(text);
        });
        var generated = Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.DeserializeSection(text, "Schedule", GeneratedScheduleSection.IniFactory);
        });

        Assert.IsNotNull(reflection.InnerException);
        Assert.IsNotNull(generated.InnerException);
        Assert.AreEqual(reflection.InnerException.GetType(), generated.InnerException.GetType());
    }

    /// <summary>
    /// Verifies that a section of temporal values round-trips through the generated INI factory with every tick, the
    /// kind of the <see cref="DateTime" /> and the offset of the <see cref="DateTimeOffset" /> kept.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenTemporalValuesAreRoundTripped_ShouldKeepEveryTickTheKindAndTheOffset()
    {
        var section = new GeneratedScheduleSection
        {
            At = new DateTime(2021, 2, 6, 1, 2, 3, DateTimeKind.Utc).AddTicks(4567891),
            AtOffset = new DateTimeOffset(2021, 2, 6, 1, 2, 3, TimeSpan.FromHours(10)).AddTicks(4567891),
            Span = new TimeSpan(1, 2, 3, 4).Add(TimeSpan.FromTicks(5678901)),
        };

        string text = IniSerializer.SerializeSection("schedule", section, GeneratedScheduleSection.IniFactory);
        GeneratedScheduleSection restored = IniSerializer.DeserializeSection(text, "schedule", GeneratedScheduleSection.IniFactory);

        Assert.AreEqual(section.At, restored.At, text);
        Assert.AreEqual(DateTimeKind.Utc, restored.At.Kind, text);
        Assert.AreEqual(section.AtOffset, restored.AtOffset, text);
        Assert.AreEqual(section.AtOffset.Offset, restored.AtOffset.Offset, text);
        Assert.AreEqual(section.Span, restored.Span, text);
    }

    /// <summary>
    /// Verifies that a section of the scalars beyond the common set serializes through the generated INI factory to
    /// the same text as through INI's runtime reflection binder, with the <see langword="null" /> members left out of
    /// both and with every member set.
    /// </summary>
    /// <param name="setEveryMember">Whether every member holds a value, rather than the nullable ones none.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IniFactory_WhenExtendedScalarsAreSerialized_ShouldMatchReflectionBinderOutput(bool setEveryMember)
    {
        GeneratedExtendedScalarSection section = CreateExtendedScalarSection(setEveryMember);

        string reflection = IniSerializer.Serialize(new { scalars = section });
        string generated = IniSerializer.SerializeSection("scalars", section, GeneratedExtendedScalarSection.IniFactory);

        Assert.AreEqual(reflection, generated);
    }

    /// <summary>
    /// Verifies that a section of the scalars beyond the common set reads back through the generated INI factory as
    /// the values written.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenExtendedScalarsAreRoundTripped_ShouldReadBackTheValues()
    {
        GeneratedExtendedScalarSection section = CreateExtendedScalarSection(setEveryMember: true);

        string text = IniSerializer.SerializeSection("scalars", section, GeneratedExtendedScalarSection.IniFactory);
        GeneratedExtendedScalarSection restored = IniSerializer.DeserializeSection(text, "scalars", GeneratedExtendedScalarSection.IniFactory);

        Assert.AreEqual(section.Day, restored.Day, text);
        Assert.AreEqual(section.Time, restored.Time, text);
        Assert.AreEqual(section.Link, restored.Link, text);
        Assert.AreEqual(section.Large, restored.Large, text);
        Assert.AreEqual(section.Unsigned, restored.Unsigned, text);
        Assert.AreEqual(section.Small, restored.Small, text);
        Assert.AreEqual(section.Big, restored.Big, text);
        Assert.AreEqual(section.Handle, restored.Handle, text);
        Assert.AreEqual(section.Size, restored.Size, text);
        Assert.AreEqual(section.MaybeDay, restored.MaybeDay, text);
        Assert.AreEqual(section.MaybeLarge, restored.MaybeLarge, text);
    }

    /// <summary>
    /// Creates a section of the scalars beyond the common set.
    /// </summary>
    /// <param name="setEveryMember">Whether every member holds a value, rather than the nullable ones none.</param>
    /// <returns>The section.</returns>
    private static GeneratedExtendedScalarSection CreateExtendedScalarSection(bool setEveryMember) =>
        new()
        {
            Day = new DateOnly(2026, 1, 2),
            Time = new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567)),
            Link = setEveryMember ? new Uri("https://example.com/a?b=c") : null,
            Large = Int128.MinValue,
            Unsigned = UInt128.MaxValue,
            Small = (Half)0.1,
            Big = -(System.Numerics.BigInteger.Pow(10, 40) + 1),
            Handle = nint.MinValue,
            Size = nuint.MaxValue,
            MaybeDay = setEveryMember ? new DateOnly(1999, 12, 31) : null,
            MaybeLarge = setEveryMember ? Int128.MaxValue : null,
        };

    /// <summary>
    /// Verifies that the generated INI factory writes a nullable member whose own condition is
    /// <see cref="IgnoreCondition.WhenWritingDefault" /> when it holds <c>0</c>, since a nullable member's default is
    /// <see langword="null" />, and leaves it out only when it is <see langword="null" />, as INI's reflection binder
    /// does.
    /// </summary>
    [TestMethod]
    public void IniFactory_WhenANullableMemberUnderWhenWritingDefaultHoldsZero_ShouldWriteIt()
    {
        string zero = IniSerializer.SerializeSection("conditional", new GeneratedConditionalSection { MaybeZero = 0 }, GeneratedConditionalSection.IniFactory);
        string none = IniSerializer.SerializeSection("conditional", new GeneratedConditionalSection(), GeneratedConditionalSection.IniFactory);

        Assert.IsTrue(zero.Contains("MaybeZero=0", StringComparison.Ordinal), zero);
        Assert.IsFalse(none.Contains("MaybeZero", StringComparison.Ordinal), none);
    }
}
