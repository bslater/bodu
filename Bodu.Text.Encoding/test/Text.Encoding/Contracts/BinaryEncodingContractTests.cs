// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a binary encoding (Base16, Base32, Base58, Base64,
/// Base64Url, Base85, hex, etc.). Concrete subclasses override the four <c>Encode</c>/<c>Decode</c>/
/// <c>TryEncode</c>/<c>TryDecode</c> adapter methods plus <see cref="KnownAnswers" /> and
/// <see cref="InvalidInputs" /> to plug their encoding into the inherited tests.
/// </summary>
/// <typeparam name="TEncoding">The encoding type under test.</typeparam>
/// <remarks>
/// <para>
/// Carried for documentation only; the contract runs through the adapter delegates.
/// </para>
/// </remarks>
public abstract class BinaryEncodingContractTests<TEncoding>
{
    /// <summary>
    /// Encodes a byte payload to the canonical text form.
    /// </summary>
    /// <param name="input">The raw byte payload.</param>
    /// <returns>The encoded text.</returns>
    protected abstract string Encode(byte[] input);

    /// <summary>
    /// Decodes the canonical text form back to bytes. Implementations must throw a
    /// <see cref="FormatException" />-derived exception when <paramref name="text" /> is malformed.
    /// </summary>
    /// <param name="text">The encoded text.</param>
    /// <returns>The decoded byte payload.</returns>
    protected abstract byte[] Decode(string text);

    /// <summary>
    /// Attempts to encode a byte payload into <paramref name="destination" />. Returns
    /// <see langword="false" /> when the destination is too small without throwing.
    /// </summary>
    /// <param name="input">The raw byte payload.</param>
    /// <param name="destination">The destination character buffer.</param>
    /// <param name="charsWritten">Receives the number of characters written when the call returns <see langword="true" />.</param>
    /// <returns><see langword="true" /> when the entire payload was encoded; otherwise <see langword="false" />.</returns>
    protected abstract bool TryEncode(byte[] input, Span<char> destination, out int charsWritten);

    /// <summary>
    /// Attempts to decode <paramref name="text" /> into <paramref name="destination" />. Returns
    /// <see langword="false" /> when the destination is too small or when the text is malformed, without
    /// throwing.
    /// </summary>
    /// <param name="text">The encoded text.</param>
    /// <param name="destination">The destination byte buffer.</param>
    /// <param name="bytesWritten">Receives the number of bytes written when the call returns <see langword="true" />.</param>
    /// <returns><see langword="true" /> when decoding succeeded; otherwise <see langword="false" />.</returns>
    protected abstract bool TryDecode(string text, Span<byte> destination, out int bytesWritten);

    /// <summary>
    /// Returns the positive known-answer vectors for the encoding under test.
    /// </summary>
    protected abstract IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; }

    /// <summary>
    /// Returns the negative-vector rows the decoder must reject.
    /// </summary>
    protected abstract IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; }

    /// <summary>
    /// Verifies that every <see cref="BinaryEncodingKat" /> row produces its expected text under
    /// <see cref="Encode" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenKnownBytes_ShouldReturnExpectedText()
    {
        foreach (BinaryEncodingKat kat in KnownAnswers)
        {
            string actual = Encode(kat.Bytes);

            Assert.AreEqual(kat.Encoded, actual, $"KAT '{kat.Name}': encode produced unexpected text.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="BinaryEncodingKat" /> row round-trips back to its bytes under
    /// <see cref="Decode" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenKnownText_ShouldReturnExpectedBytes()
    {
        foreach (BinaryEncodingKat kat in KnownAnswers)
        {
            byte[] actual = Decode(kat.Encoded);

            CollectionAssert.AreEqual(kat.Bytes, actual, $"KAT '{kat.Name}': decode produced unexpected bytes.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="BinaryEncodingKat" /> round-trips bytes → text → bytes without
    /// loss. Documents that the encoding is bijective on its accepted domain.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenKnownBytes_ShouldReturnOriginalBytes()
    {
        foreach (BinaryEncodingKat kat in KnownAnswers)
        {
            byte[] roundTripped = Decode(Encode(kat.Bytes));

            CollectionAssert.AreEqual(kat.Bytes, roundTripped, $"KAT '{kat.Name}': round-trip lost bytes.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="TryEncode" /> returns <see langword="false" /> for a destination buffer
    /// that is exactly one character too small to hold the encoded output.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        foreach (BinaryEncodingKat kat in KnownAnswers)
        {
            if (kat.Encoded.Length == 0)
                continue;

            Span<char> destination = new char[kat.Encoded.Length - 1];

            bool success = TryEncode(kat.Bytes, destination, out _);

            Assert.IsFalse(success, $"KAT '{kat.Name}': TryEncode must return false when destination is too small.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="TryDecode" /> returns <see langword="false" /> for every
    /// <see cref="InvalidEncodedTextKat" /> negative vector.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenTextIsInvalid_ShouldReturnFalse()
    {
        foreach (InvalidEncodedTextKat kat in InvalidInputs)
        {
            // Use a generous destination so the failure is from text-validation, not buffer sizing.
            Span<byte> destination = new byte[kat.Encoded.Length + 32];

            bool success = TryDecode(kat.Encoded, destination, out _);

            Assert.IsFalse(success, $"Invalid KAT '{kat.Name}': TryDecode must return false for malformed text.");
        }
    }
}
