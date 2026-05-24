// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(char[], int, int, Base85Variant, BaseFormatStyles)" /> throws for a
    /// null character array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base85.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> throws for a null string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base85.Decode((string)null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> throws for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base85.Encode((byte[])null!);
        });
    }

}
