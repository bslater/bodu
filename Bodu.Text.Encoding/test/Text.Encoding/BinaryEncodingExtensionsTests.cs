// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="BinaryEncodingExtensions" />.
/// </summary>
[TestClass]
public sealed partial class BinaryEncodingExtensionsTests
{

    private static readonly byte[] CanonicalBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.Decode(string, IBinaryEncoding)" /> dispatches through an
    /// <see cref="IBinaryEncoding" /> instance.
    /// </summary>
    [TestMethod]
    public void Decode_OnStringWithIBinaryEncoding_ShouldDispatchThroughInterface()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base64;
        var encoded = encoding.Encode(CanonicalBytes);

        var actual = encoded.Decode(encoding);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.Encode(byte[], IBinaryEncoding)" /> dispatches through an
    /// <see cref="IBinaryEncoding" /> instance.
    /// </summary>
    [TestMethod]
    public void Encode_OnByteArrayWithIBinaryEncoding_ShouldDispatchThroughInterface()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base64;

        var actual = CanonicalBytes.Encode(encoding);

        Assert.AreEqual(encoding.Encode(CanonicalBytes), actual);
    }

    /// <summary>
    /// Verifies that the string overloads reject a <see langword="null" /> string.
    /// </summary>
    [TestMethod]
    public void FromString_Extensions_WhenNullString_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((string)null!).FromBase16String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((string)null!).FromBase32String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((string)null!).FromBase64String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((string)null!).FromBase58String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((string)null!).FromBase85String());
    }

    /// <summary>
    /// Verifies that the <c>From{N}String</c> reverse extensions round-trip the matching <c>To{N}String</c> output.
    /// </summary>
    [TestMethod]
    public void FromString_ExtensionsShouldRoundTripCanonicalForm()
    {
        Assert.IsTrue(CanonicalBytes.SequenceEqual(CanonicalBytes.ToBase16String().FromBase16String()));
        Assert.IsTrue(CanonicalBytes.SequenceEqual(CanonicalBytes.ToBase32String().FromBase32String()));
        Assert.IsTrue(CanonicalBytes.SequenceEqual(CanonicalBytes.ToBase64String().FromBase64String()));
        Assert.IsTrue(CanonicalBytes.SequenceEqual(CanonicalBytes.ToBase58String().FromBase58String()));
        Assert.IsTrue(CanonicalBytes.SequenceEqual(CanonicalBytes.ToBase85String().FromBase85String()));
    }

    /// <summary>
    /// Verifies that the <c>To{N}String</c> extension on <see cref="byte" />-array returns the same result as the
    /// matching canonical static method.
    /// </summary>
    [TestMethod]
    public void ToBase16String_OnByteArray_ShouldMatchStaticCanonical()
    {
        Assert.AreEqual(Base16.Encode(CanonicalBytes), CanonicalBytes.ToBase16String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase32String(byte[])" /> matches the static canonical
    /// Base32 form.
    /// </summary>
    [TestMethod]
    public void ToBase32String_OnByteArray_ShouldMatchStaticCanonical()
    {
        Assert.AreEqual(Base32.ToBase32String(CanonicalBytes), CanonicalBytes.ToBase32String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase58String(byte[])" /> matches the static canonical
    /// Base58 form.
    /// </summary>
    [TestMethod]
    public void ToBase58String_OnByteArray_ShouldMatchStaticCanonical()
    {
        Assert.AreEqual(Base58.ToBase58String(CanonicalBytes), CanonicalBytes.ToBase58String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase64String(byte[])" /> matches the static canonical
    /// Base64 form.
    /// </summary>
    [TestMethod]
    public void ToBase64String_OnByteArray_ShouldMatchStaticCanonical()
    {
        Assert.AreEqual(Base64.ToBase64String(CanonicalBytes), CanonicalBytes.ToBase64String());
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodingExtensions.ToBase85String(byte[])" /> matches the static canonical
    /// Ascii85 form.
    /// </summary>
    [TestMethod]
    public void ToBase85String_OnByteArray_ShouldMatchStaticCanonical()
    {
        Assert.AreEqual(Base85.ToBase85String(CanonicalBytes), CanonicalBytes.ToBase85String());
    }

    /// <summary>
    /// Verifies that the byte-array overloads reject a <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void ToString_Extensions_WhenNullByteArray_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((byte[])null!).ToBase16String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((byte[])null!).ToBase32String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((byte[])null!).ToBase64String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((byte[])null!).ToBase58String());
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = ((byte[])null!).ToBase85String());
    }

}
