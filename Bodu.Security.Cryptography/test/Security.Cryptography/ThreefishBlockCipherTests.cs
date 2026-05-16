// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the <see cref="ThreefishBlockCipher" /> abstract base — specifically for members defined
/// at the base class level that are not exercised by the variant-specific
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> harness.
/// </summary>
/// <remarks>
/// <para>
/// Tests that apply across all three Threefish variants (256, 512, 1024) are implemented here against
/// <see cref="Threefish256Cipher" />, which uses the smallest key and tweak sizes and exercises every
/// code path in <see cref="ThreefishBlockCipher" /> with the least memory overhead.
/// </para>
/// </remarks>
[TestClass]
internal sealed partial class ThreefishBlockCipherTests
{
    private static readonly byte[] s_validKey256 = new byte[32];
    private static readonly byte[] s_validTweak = new byte[16];
}
