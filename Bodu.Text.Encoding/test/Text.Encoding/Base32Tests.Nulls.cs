// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(char[], int, int, Base32Variant, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> when the character array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> when the string is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.Decode((string)null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when the byte array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when the byte array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArrayWithSlice_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base32.Encode((byte[])null!, 0, 0);
        });
    }

}
