// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(char[], int, int, Base58Variant, BaseFormatStyles)" /> throws for a
    /// null character array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base58.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> throws for a null string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base58.Decode((string)null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], Base58Variant)" /> throws for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base58.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], int, int, Base58Variant)" /> throws for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArrayWithSlice_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base58.Encode((byte[])null!, 0, 0);
        });
    }

}
