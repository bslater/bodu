// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519Tests.ScalarMultBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Curve25519Tests
{
    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMultBase" /> derives the public keys published in the RFC 7748 §6.1
    /// Diffie-Hellman example.
    /// </summary>
    [TestMethod]
    [DataRow(
        "RFC 7748 §6.1 Alice",
        "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a",
        "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a")]
    [DataRow(
        "RFC 7748 §6.1 Bob",
        "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb",
        "de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f")]
    public void ScalarMultBase_WhenGivenRfc7748PrivateKey_ShouldProducePublishedPublicKey(
        string testName, string privateHex, string expectedPublicHex)
    {
        _ = testName;

        byte[] destination = new byte[Curve25519.PointSizeInBytes];
        Curve25519.ScalarMultBase(Convert.FromHexString(privateHex), destination);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedPublicHex), destination);
    }

    /// <summary>
    /// Verifies that multiplying the base point through Ed25519's fixed-base table gives, for boundary and seeded
    /// scalars, the same u-coordinate as the Montgomery ladder on u = 9.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenScalarsAreSeededOrAtTheirBounds_ShouldMatchTheLadder()
    {
        var random = new Random(0x2551_9104);
        byte[] scalar = new byte[Curve25519.PointSizeInBytes];
        byte[] nine = new byte[Curve25519.PointSizeInBytes];
        nine[0] = 9;
        byte[] expected = new byte[Curve25519.PointSizeInBytes];
        byte[] actual = new byte[Curve25519.PointSizeInBytes];

        for (int iteration = 0; iteration < 256; iteration++)
        {
            switch (iteration)
            {
                case 0:
                    Array.Clear(scalar);
                    break;
                case 1:
                    Array.Fill(scalar, (byte)0xFF);
                    break;
                case 2:
                    Array.Clear(scalar);
                    scalar[0] = 9;
                    break;
                default:
                    random.NextBytes(scalar);
                    break;
            }

            _ = Curve25519.ScalarMult(scalar, nine, expected);
            Curve25519.ScalarMultBase(scalar, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMultBase" /> does not modify the caller's scalar buffer when applying
    /// RFC 7748 clamping.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenScalarRequiresClamping_ShouldNotModifyCallerScalar()
    {
        byte[] scalar = Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92cff");
        byte[] original = (byte[])scalar.Clone();

        Curve25519.ScalarMultBase(scalar, new byte[Curve25519.PointSizeInBytes]);

        CollectionAssert.AreEqual(original, scalar);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMultBase" /> throws <see cref="ArgumentException" /> naming the
    /// scalar when it is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenScalarLengthIsInvalid_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Curve25519.ScalarMultBase(new byte[31], new byte[Curve25519.PointSizeInBytes]);
        });

        Assert.AreEqual("scalar", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519.ScalarMultBase" /> throws <see cref="ArgumentException" /> naming the
    /// destination when it is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void ScalarMultBase_WhenDestinationLengthIsInvalid_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Curve25519.ScalarMultBase(new byte[Curve25519.PointSizeInBytes], new byte[33]);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }
}
