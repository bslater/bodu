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
}
