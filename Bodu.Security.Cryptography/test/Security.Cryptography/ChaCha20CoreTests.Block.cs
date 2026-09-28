// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.Block.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>The resource name of the embedded RFC 8439 text.</summary>
    private const string Rfc8439ResourceName = "Bodu.Security.Cryptography.Rfc8439.txt";

    /// <summary>
    /// Verifies that the block function reproduces the serialized block of RFC 8439 Section 2.3.2.
    /// </summary>
    [TestMethod]
    public void Block_WhenGivenRfc8439Section232Vector_ShouldProduceSerializedBlock()
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        ChaCha20Core.Initialize(
            state,
            Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F"),
            Convert.FromHexString("000000090000004A00000000"));
        byte[] block = new byte[ChaCha20Core.BlockBytes];

        ChaCha20Core.Block(state, 1, block);

        CollectionAssert.AreEqual(
            Convert.FromHexString(
                "10F1E7E4D13B5915500FDD1FA32071C4C7D1F4C733C068030422AA9AC3D46C4E" +
                "D2826446079FAA0914C2D705D98B02A2B5129CD1DE164EB9CBD083E8A2503C4E"),
            block);
    }

    /// <summary>
    /// Verifies that the block function reproduces the keystream of every RFC 8439 Appendix A.1 vector.
    /// </summary>
    /// <param name="vector">The block-function vector under test; <c>Ciphertext</c> carries the keystream.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc8439BlockFunctionData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Block_WhenGivenRfc8439AppendixA1Vector_ShouldProduceKeystream(StreamCipherKnownAnswer vector)
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        ChaCha20Core.Initialize(state, vector.Key, vector.Nonce);
        byte[] block = new byte[ChaCha20Core.BlockBytes];

        ChaCha20Core.Block(state, vector.Counter, block);

        CollectionAssert.AreEqual(vector.Ciphertext, block, vector.Name);
    }

    /// <summary>
    /// Verifies that the block function takes its counter from the argument, whatever the state's counter word holds.
    /// </summary>
    [TestMethod]
    public void Block_WhenStateCounterWordDiffers_ShouldUseTheCounterArgument()
    {
        var random = new Random(0x0C4A_0001);
        uint[] state = NextState(random);
        uint[] other = (uint[])state.Clone();
        other[ChaCha20Core.CounterWord] ^= 0x8000_0001;
        byte[] expected = new byte[ChaCha20Core.BlockBytes];
        byte[] actual = new byte[ChaCha20Core.BlockBytes];

        ChaCha20Core.Block(state, 7, expected);
        ChaCha20Core.Block(other, 7, actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a destination shorter than a block is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void Block_WhenDestinationIsShorterThanABlock_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        byte[] destination = new byte[ChaCha20Core.BlockBytes - 1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Block(state, 0, destination);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a state shorter than sixteen words is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void Block_WhenStateHoldsFewerThanSixteenWords_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[ChaCha20Core.StateWords - 1];
        byte[] destination = new byte[ChaCha20Core.BlockBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Block(state, 0, destination);
        });

        Assert.AreEqual("state", ex.ParamName);
    }

    /// <summary>
    /// Loads the RFC 8439 Appendix A.1 block-function vectors from the embedded RFC text as
    /// <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded resource cannot be located.</exception>
    private static IEnumerable<object[]> Rfc8439BlockFunctionData()
    {
        using Stream stream = typeof(ChaCha20CoreTests).Assembly.GetManifestResourceStream(Rfc8439ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{Rfc8439ResourceName}' is missing.");

        foreach (Rfc8439TestVector vector in Rfc8439VectorReader.Read(stream, "A.1.  The ChaCha20 Block Functions"))
        {
            yield return new object[]
            {
                new StreamCipherKnownAnswer
                {
                    Name = $"RFC 8439 Appendix A.1 block function #{vector.Number}",
                    Provenance = KatProvenance.Rfc("RFC 8439 Appendix A.1"),
                    Key = vector.Field("Key"),
                    Nonce = vector.Field("Nonce"),
                    Counter = vector.InitialBlockCounter ?? 0,
                    IsKeystream = true,
                    Plaintext = [],
                    Ciphertext = vector.Field("Keystream"),
                },
            };
        }
    }
}
