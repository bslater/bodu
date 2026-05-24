// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(char[], int, int, Base64Variant, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> for a null character array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> for a null string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.Decode((string)null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], int, int, Base64Variant, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArrayWithSlice_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base64.Encode((byte[])null!, 0, 0);
        });
    }

}
