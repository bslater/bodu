// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.SharedScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;

namespace Bodu.Text.Delimited;

/// <summary>Regression cases for the shared codec's delimited-only temporal and blank-cell policy.</summary>
public partial class DelimitedSerializerTests
{
    [TestMethod]
    public void ScalarCodec_WhenNullableIsWhitespace_ShouldTreatAsNullExceptForCharacter()
    {
        Assert.IsNull(InvariantScalarCodec.Parse(" 	 ", typeof(decimal?), InvariantScalarCodec.Policy.Delimited));
        Assert.AreEqual(' ', InvariantScalarCodec.Parse(" ", typeof(char?), InvariantScalarCodec.Policy.Delimited));
        Assert.ThrowsExactly<FormatException>(() =>
            InvariantScalarCodec.Parse("  ", typeof(char?), InvariantScalarCodec.Policy.Delimited));
    }

    [TestMethod]
    public void ScalarCodec_WhenTemporalHasTicks_ShouldUseRoundtripRepresentation()
    {
        DateTime date = new DateTime(2026, 8, 20, 11, 22, 33, DateTimeKind.Utc).AddTicks(1234567);
        string written = InvariantScalarCodec.Format(date, InvariantScalarCodec.Policy.Delimited);
        Assert.AreEqual(date.ToString("O", CultureInfo.InvariantCulture), written);
        Assert.AreEqual(date, InvariantScalarCodec.Parse(written, typeof(DateTime), InvariantScalarCodec.Policy.Delimited));
    }

    private sealed class DelimitedMemberProbe : FlatMemberDescriptor
    {
        public DelimitedMemberProbe(System.Reflection.MemberInfo member) : base(member, static value => value) { }
    }

    private sealed class DelimitedMemberSample
    {
        [PropertyOrder(2)]
        public int Number { get; set; }
        public readonly int ImmutableField = 12;
    }

    [TestMethod]
    public void MemberDescriptor_WhenFieldIsReadOnly_ShouldReportAccessAndOrdering()
    {
        var field = typeof(DelimitedMemberSample).GetField(nameof(DelimitedMemberSample.ImmutableField))!;
        var descriptor = new DelimitedMemberProbe(field);
        Assert.IsTrue(descriptor.CanRead);
        Assert.IsFalse(descriptor.CanWrite);
        Assert.AreEqual(12, descriptor.GetValue(new DelimitedMemberSample()));

        var property = typeof(DelimitedMemberSample).GetProperty(nameof(DelimitedMemberSample.Number))!;
        Assert.AreEqual(2, new DelimitedMemberProbe(property).Order);
    }
}
