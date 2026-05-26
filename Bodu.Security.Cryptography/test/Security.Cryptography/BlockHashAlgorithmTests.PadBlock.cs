// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.PadBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="BlockHashAlgorithm{T}" />'s <c>PadBlock</c> path is never
    /// dispatched via the happy path of <see cref="HashAlgorithm.ComputeHash(byte[])" />,
    /// and in particular never raises <see cref="NotImplementedException" />. Regression
    /// guard for <see cref="Poly1305" /> where <c>PadBlock</c> previously threw
    /// <c>NotImplementedException</c> despite being unreachable.
    /// </summary>
    [TestMethod]
    public void PadBlock_OnHappyPath_ShouldNeverRaiseNotImplementedException()
    {
        using TAlgorithm algo = CreateAlgorithm();
        try
        {
            var tag = algo.ComputeHash([1, 2, 3, 4, 5]);
            Assert.IsNotNull(tag);
        }
        catch (NotImplementedException)
        {
            Assert.Fail($"{typeof(TAlgorithm).Name}.PadBlock must not raise NotImplementedException on the happy path.");
        }
        catch (NotSupportedException)
        {
            // Acceptable: the contract method throws NotSupportedException if ever
            // dispatched to. The happy path should not reach it, however.
        }
    }

    /// <summary>
    /// Verifies that any exception message surfaced by the hash pipeline (including
    /// <c>PadBlock</c>'s argument-validation branch) is plain printable ASCII — i.e. no
    /// NUL bytes or high-bit characters from a file-encoding round-trip. Regression guard
    /// for a mojibake character that previously appeared in <see cref="SipHash" />'s
    /// <c>PadBlock</c> error message.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void HashPipeline_ExceptionMessages_ShouldContainOnlyPrintableAscii(TVariant variant)
    {
        HashAlgorithmSpecification specification = GetSpecification(variant);
        // Exercise every residual length from 0 up to 2x the configured input block size
        // so we hit every branch of any residual-handling PadBlock path.
        var limit = Math.Max(16, specification.InputBlockSize * 2);
        for (var len = 0; len < limit; len++)
        {
            using TAlgorithm algo = CreateAlgorithm();
            var data = new byte[len];
            try
            {
                var tag = algo.ComputeHash(data);
                Assert.IsNotNull(tag);
            }
            catch (Exception ex)
            {
                var message = ex.Message ?? string.Empty;
                Assert.IsFalse(
                    message.Contains('\0'),
                    $"Exception message for input length {len} must not contain NUL characters.");
                Assert.IsTrue(
                    Regex.IsMatch(message, "^[\\x20-\\x7E]*$"),
                    $"Exception message for input length {len} must be plain printable ASCII. Actual: '{message}'");
                throw;
            }
        }
    }
}
