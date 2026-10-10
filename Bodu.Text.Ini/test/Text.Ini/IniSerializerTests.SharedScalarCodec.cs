// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.SharedScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>Regression coverage for the common INI scalar and flat-member implementation.</summary>
public partial class IniSerializerTests
{
    [TestMethod]
    public void ScalarCodec_WhenNullableHasWhitespace_ShouldPreserveIniSemantics()
    {
        Assert.IsNull(InvariantScalarCodec.Parse("", typeof(double?), InvariantScalarCodec.Policy.Configuration));
        Assert.ThrowsExactly<FormatException>(() =>
            InvariantScalarCodec.Parse("  ", typeof(double?), InvariantScalarCodec.Policy.Configuration));
    }

    [TestMethod]
    public void ScalarCodec_WhenDateTimeOffsetHasOffset_ShouldParseWithInvariantRoundtripKind()
    {
        const string Text = "2026-01-02T03:04:05.1234567+10:00";
        DateTimeOffset expected = DateTimeOffset.Parse(Text, System.Globalization.CultureInfo.InvariantCulture);
        object? actual = InvariantScalarCodec.Parse(Text, typeof(DateTimeOffset), InvariantScalarCodec.Policy.Configuration);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void MemberDiscovery_WhenFieldsExcluded_ShouldSkipIgnoredAndIndexers()
    {
        var members = FlatMemberDiscovery.Enumerate(typeof(IniMemberProbe), includeFields: false).ToArray();
        Assert.AreEqual(1, members.Length);
        Assert.AreEqual(nameof(IniMemberProbe.Name), members[0].Name);
    }

    private sealed class IniMemberProbe
    {
        public string Name { get; set; } = string.Empty;
        [Bodu.Text.Serialization.Ignore]
        public int Hidden { get; set; }
        public int PublicField;
        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
