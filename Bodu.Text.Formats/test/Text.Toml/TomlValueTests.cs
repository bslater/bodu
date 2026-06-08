// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Behavioural tests for the scalar <see cref="TomlValue" /> types — kind reporting, value round-trip, and value
/// equality.
/// </summary>
[TestClass]
public sealed class TomlValueTests
{
    /// <summary>
    /// Verifies that each scalar value reports the expected <see cref="TomlValueKind" />.
    /// </summary>
    [TestMethod]
    public void Kind_WhenScalar_ShouldMatchType()
    {
        Assert.AreEqual(TomlValueKind.String, new TomlString("x").Kind);
        Assert.AreEqual(TomlValueKind.Integer, new TomlInteger(1).Kind);
        Assert.AreEqual(TomlValueKind.Float, new TomlFloat(1.0).Kind);
        Assert.AreEqual(TomlValueKind.Boolean, new TomlBoolean(true).Kind);
        Assert.AreEqual(TomlValueKind.OffsetDateTime, new TomlOffsetDateTime(DateTimeOffset.UnixEpoch).Kind);
        Assert.AreEqual(TomlValueKind.LocalDateTime, new TomlLocalDateTime(new DateTime(2020, 1, 1, 0, 0, 0)).Kind);
        Assert.AreEqual(TomlValueKind.LocalDate, new TomlLocalDate(new DateOnly(2020, 1, 1)).Kind);
        Assert.AreEqual(TomlValueKind.LocalTime, new TomlLocalTime(new TimeOnly(7, 32)).Kind);
    }

    /// <summary>
    /// Verifies that scalar values expose the value they were constructed with.
    /// </summary>
    [TestMethod]
    public void Value_WhenConstructed_ShouldRoundTrip()
    {
        Assert.AreEqual("hello", new TomlString("hello").Value);
        Assert.AreEqual(42L, new TomlInteger(42).Value);
        Assert.AreEqual(3.5, new TomlFloat(3.5).Value);
        Assert.IsTrue(new TomlBoolean(true).Value);
        Assert.AreEqual(new DateOnly(1979, 5, 27), new TomlLocalDate(new DateOnly(1979, 5, 27)).Value);
        Assert.AreEqual(new TimeOnly(7, 32, 0), new TomlLocalTime(new TimeOnly(7, 32, 0)).Value);
    }

    /// <summary>
    /// Verifies that value equality holds for equal scalar values and fails across differing values and kinds.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameValue_ShouldBeEqual()
    {
        Assert.AreEqual(new TomlString("a"), new TomlString("a"));
        Assert.AreNotEqual(new TomlString("a"), new TomlString("b"));
        Assert.AreEqual(new TomlInteger(7), new TomlInteger(7));
        Assert.AreNotEqual<TomlValue>(new TomlInteger(7), new TomlString("7"));
    }

    /// <summary>
    /// Verifies that a local date-time normalizes its <see cref="DateTimeKind" /> to <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void LocalDateTime_WhenConstructedFromUtc_ShouldBeUnspecifiedKind()
    {
        var value = new TomlLocalDateTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual(DateTimeKind.Unspecified, value.Value.Kind);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="TomlString" /> from <see langword="null" /> throws.
    /// </summary>
    [TestMethod]
    public void TomlString_WhenNullValue_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TomlString(null!);
        });
    }
}
