// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTagTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="MapiPropertyTag" />, the 32-bit MAPI property tag.
/// </summary>
[TestClass]
public partial class MapiPropertyTagTests
{
    /// <summary>
    /// Gets rows pinning the canonical hexadecimal form for every base property type, the multi-valued flag, and the
    /// named-property boundary.
    /// </summary>
    /// <value>Rows of raw tag value → expected <see cref="MapiPropertyTag.ToString" /> text.</value>
    public static IEnumerable<object[]> ToStringKats =>
        new ValidKat<uint, string>[]
        {
            new("Unspecified", 0x00370000, "0x00370000"),
            new("Null", 0x00370001, "0x00370001"),
            new("Int16", 0x00370002, "0x00370002"),
            new("Int32", 0x00370003, "0x00370003"),
            new("Float", 0x00370004, "0x00370004"),
            new("Double", 0x00370005, "0x00370005"),
            new("Currency", 0x00370006, "0x00370006"),
            new("AppTime", 0x00370007, "0x00370007"),
            new("ErrorCode", 0x0037000A, "0x0037000A"),
            new("Boolean", 0x0037000B, "0x0037000B"),
            new("Object", 0x0037000D, "0x0037000D"),
            new("Int64", 0x00370014, "0x00370014"),
            new("String8", 0x0037001E, "0x0037001E"),
            new("Unicode", 0x0037001F, "0x0037001F"),
            new("SystemTime", 0x00370040, "0x00370040"),
            new("Guid", 0x00370048, "0x00370048"),
            new("Binary", 0x00370102, "0x00370102"),
            new("MultiValuedUnicode", 0x0037101F, "0x0037101F"),
            new("MultiValuedBinary", 0x00371102, "0x00371102"),
            new("NamedFloor", 0x8000001F, "0x8000001F"),
            new("MaxIdBinary", 0xFFFF0102, "0xFFFF0102"),
            new("ZeroTag", 0x00000000, "0x00000000"),
        }.Select(kat => new object[] { kat });
}
