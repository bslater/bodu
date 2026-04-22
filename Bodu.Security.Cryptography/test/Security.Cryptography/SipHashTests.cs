// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    public enum SipHashVariant
    {
        SipHash_2_4,
        SipHash_4_8
    }

    public abstract partial class SipHashTests<TTest, TAlgorithm>
        : KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, SipHashVariant>
        where TTest : SipHashTests<TTest, TAlgorithm>, new()
        where TAlgorithm : SipHash<TAlgorithm>, new()
    {
        /// <inheritdocs/>
        protected static readonly byte[] SipHashTestKey =
        [
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
        ];

        /// <inheritdocs/>
        public override IEnumerable<SipHashVariant> GetHashAlgorithmVariants() => new[]
        {
            SipHashVariant.SipHash_2_4,
            SipHashVariant.SipHash_4_8
        };
    }
}