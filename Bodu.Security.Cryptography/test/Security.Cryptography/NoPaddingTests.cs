// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZeroPaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class NoPaddingTests
        : PaddingStrategyTests<NoPadding>
    {
        protected override int BlockSize => 16;

        protected override bool ValidatesPaddingOnUnpad => false;

        protected override byte[] CreatePlaintextWithResidual(int residualBytes)
        {
            byte[] buf = new byte[residualBytes];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte)(0x30 + i);
            return buf;
        }
    }
}
