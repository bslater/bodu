// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MsgPropertyDecoder" />, the storage property decoder.
/// </summary>
[TestClass]
public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Gets known-answer rows covering every fixed-length property type's inline decoding.
    /// </summary>
    /// <value>Rows of tag and inline value paired with the expected CLR value.</value>
    public static IEnumerable<object[]> FixedTypeKats =>
        new MsgDecodeKat[]
        {
            new("Int16MinusOne", 0x12340002, 0xFFFF, (short)-1),
            new("Int32MinusOne", 0x12340003, 0xFFFFFFFF, -1),
            new("FloatOnePointFive", 0x12340004, 0x3FC00000, 1.5f),
            new("DoubleMinusTwoPointTwoFive", 0x12340005, 0xC002000000000000, -2.25d),
            new("CurrencyScaledByTenThousand", 0x12340006, 12345, 1.2345m),
            new("AppTimeAsOleDate", 0x12340007, 0x4000000000000000, 2.0d),
            new("ErrorCodeAccessDenied", 0x1234000A, 0x80070005, unchecked((int)0x80070005)),
            new("BooleanTrue", 0x1234000B, 1, true),
            new("BooleanFalse", 0x1234000B, 0, false),
            new("BooleanIgnoresHighBytes", 0x1234000B, 0x100, false),
            new("Int64Sequence", 0x12340014, 0x0102030405060708, 0x0102030405060708L),
            new(
                "SystemTime2020",
                0x12340040,
                (ulong)new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToFileTime(),
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        }.Select(kat => new object[] { kat });

    /// <summary>
    /// Builds a fixture and decodes its root storage.
    /// </summary>
    /// <param name="builder">The fixture builder.</param>
    /// <param name="validationLevel">The validation level to decode under.</param>
    /// <returns>The decoded property collection.</returns>
    internal static MapiPropertyCollection Decode(
        MsgFixtureBuilder builder,
        CompoundValidationLevel validationLevel = CompoundValidationLevel.Compatible)
    {
        using MemoryStream stream = builder.Build();
        using var compound = CompoundFile.Open(stream);
        return MsgPropertyDecoder.Decode(
            compound.RootStorage, MsgPropertyStreamKind.Root, validationLevel, inheritedEncoding: null, out _);
    }
}
