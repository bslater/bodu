// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> when the character array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> throws
    /// <see cref="ArgumentNullException" /> when the string is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.Decode((string)null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when the byte array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when the byte array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArrayWithSlice_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base16.Encode((byte[])null!, 0, 0);
        });
    }

}
